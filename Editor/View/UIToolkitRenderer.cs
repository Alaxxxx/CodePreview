using System;
using System.IO;
using System.Text;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Helpers;
using OpalStudio.CodePreview.Editor.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using FileInfo = OpalStudio.CodePreview.Editor.Data.FileInfo;
using Object = UnityEngine.Object;

namespace OpalStudio.CodePreview.Editor.View
{
      internal sealed class UIToolkitRenderer
      {
            private readonly PreviewSettings _settings;
            private readonly SearchManager _searchManager;
            private readonly MonoScript _monoScriptTarget;
            private readonly TextAsset _textAssetTarget;

            private VisualElement _linesContainer;
            private ScrollView _scrollView;
            private Label _searchStatusLabel;
            private Button _prevButton;
            private Button _nextButton;
            private SliderInt _fontSizeSlider;
            private SliderInt _previewHeightSlider;
            private SliderInt _maxLinesSlider;
            private Toggle _lineNumbersToggle;
            private Toggle _syntaxHighlightingToggle;
            private TextField _searchField;

            private string[] _currentLines;

            private readonly Color _borderColor = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.7f, 0.7f, 0.7f);
            private readonly Color _codeBackgroundColor = new(0.12f, 0.12f, 0.12f);
            private readonly Color _headerBackgroundColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);

            private readonly Color _sectionBackgroundColor =
                  EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.88f, 0.88f, 0.88f);

            private readonly Font _editorFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font
                                                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            internal UIToolkitRenderer(PreviewSettings settings, SearchManager searchManager, Object target)
            {
                  _settings = settings;
                  _searchManager = searchManager;
                  _monoScriptTarget = target as MonoScript;
                  _textAssetTarget = target as TextAsset;
            }

            public VisualElement CreateRootElement(string[] processedLines, FileInfo fileInfo)
            {
                  var root = new VisualElement
                  {
                        style =
                        {
                              paddingTop = 8, paddingBottom = 8,
                              paddingLeft = 8, paddingRight = 8,
                              flexGrow = 1
                        }
                  };

                  root.Add(CreateMinimalHeader());
                  root.Add(CreateSpacer(6));
                  root.Add(CreateDetailsFoldout(fileInfo));
                  root.Add(CreateSpacer(6));
                  root.Add(CreateCodePreviewSection(processedLines));

                  _settings.OnSettingsChanged += UpdateStyles;
                  _searchManager.OnSearchResultsChanged += UpdateSearchStatus;

                  return root;
            }

            private VisualElement CreateMinimalHeader()
            {
                  var headerBox = new VisualElement
                  {
                        style =
                        {
                              flexDirection = FlexDirection.Row,
                              alignItems = Align.Center,
                              backgroundColor = _headerBackgroundColor,
                              borderBottomColor = _borderColor,
                              borderTopColor = _borderColor,
                              borderLeftColor = _borderColor,
                              borderRightColor = _borderColor,
                              borderBottomWidth = 1, borderTopWidth = 1,
                              borderLeftWidth = 1, borderRightWidth = 1,
                              borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                              borderTopLeftRadius = 4, borderTopRightRadius = 4,
                              paddingTop = 6, paddingBottom = 6,
                              paddingLeft = 10, paddingRight = 10
                        }
                  };

                  var icon = new Image { style = { width = 16, height = 16, marginRight = 6 } };
                  var title = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13, flexGrow = 1 } };

                  if (_monoScriptTarget != null)
                  {
                        icon.image = EditorGUIUtility.IconContent("cs Script Icon").image;
                        title.text = $"{_monoScriptTarget.name}.cs";
                  }
                  else if (_textAssetTarget != null)
                  {
                        ScriptType scriptType = ScriptTypeDetector.DetectType(AssetDatabase.GetAssetPath(_textAssetTarget));
                        icon.image = GetIconForType(scriptType).image;
                        title.text = _textAssetTarget.name + GetExtensionForType(scriptType);
                  }

                  var typeLabel = new Label
                  {
                        style =
                        {
                              paddingLeft = 8, paddingRight = 8,
                              paddingTop = 2, paddingBottom = 2,
                              backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.8f, 0.8f, 0.8f),
                              borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                              borderTopLeftRadius = 3, borderTopRightRadius = 3,
                              fontSize = 10,
                              unityFontStyleAndWeight = FontStyle.Bold
                        }
                  };

                  if (_monoScriptTarget != null)
                  {
                        (string label, Color color) = GetScriptTypeInfo(_monoScriptTarget);
                        typeLabel.text = label;
                        typeLabel.style.color = color;
                  }
                  else if (_textAssetTarget != null)
                  {
                        (string label, Color color) =
                              GetTextAssetTypeInfo(ScriptTypeDetector.DetectType(AssetDatabase.GetAssetPath(_textAssetTarget)));
                        typeLabel.text = label;
                        typeLabel.style.color = color;
                  }

                  headerBox.Add(icon);
                  headerBox.Add(title);
                  headerBox.Add(typeLabel);

                  return headerBox;
            }

            private VisualElement CreateDetailsFoldout(FileInfo fileInfo)
            {
                  var detailsFoldout = new Foldout
                  {
                        text = "⚙️ Details & Options",
                        value = _settings.OptionsFoldout,
                        style =
                        {
                              backgroundColor = _sectionBackgroundColor,
                              paddingLeft = 10, paddingRight = 10,
                              paddingTop = 6, paddingBottom = 6,
                              borderBottomLeftRadius = 4, borderBottomRightRadius = 4,
                              borderTopLeftRadius = 4, borderTopRightRadius = 4,
                              borderBottomColor = _borderColor, borderTopColor = _borderColor,
                              borderLeftColor = _borderColor, borderRightColor = _borderColor,
                              borderBottomWidth = 1, borderTopWidth = 1,
                              borderLeftWidth = 1, borderRightWidth = 1
                        }
                  };
                  detailsFoldout.RegisterValueChangedCallback(evt => _settings.OptionsFoldout = evt.newValue);

                  var content = new VisualElement { style = { paddingTop = 8 } };

                  if (fileInfo != null)
                  {
                        content.Add(CreateFileStats(fileInfo));
                        content.Add(CreateSpacer(10));
                  }

                  content.Add(CreateSearchContent());
                  content.Add(CreateSpacer(10));
                  content.Add(CreateOptionsContent());

                  detailsFoldout.contentContainer.Add(content);

                  return detailsFoldout;
            }

            private VisualElement CreateCodePreviewSection(string[] processedLines)
            {
                  var container = new VisualElement();

                  _linesContainer = new VisualElement
                  {
                        style =
                        {
                              paddingLeft = 12,
                              paddingRight = 12,
                              paddingTop = 10,
                              paddingBottom = 10,
                        }
                  };

                  RebuildLines(processedLines);

                  _scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
                  {
                        horizontalScrollerVisibility = ScrollerVisibility.Auto,
                        verticalScrollerVisibility = ScrollerVisibility.Auto,
                        style =
                        {
                              height = _settings.PreviewHeight,
                              backgroundColor = _codeBackgroundColor,
                              borderBottomColor = _borderColor,
                              borderTopColor = _borderColor,
                              borderLeftColor = _borderColor,
                              borderRightColor = _borderColor,
                              borderBottomWidth = 1,
                              borderTopWidth = 1,
                              borderLeftWidth = 1,
                              borderRightWidth = 1,
                              borderBottomLeftRadius = 4,
                              borderBottomRightRadius = 4,
                              borderTopLeftRadius = 4,
                              borderTopRightRadius = 4,
                        }
                  };
                  _scrollView.Add(_linesContainer);

                  var quickActions = new VisualElement
                  {
                        style =
                        {
                              flexDirection = FlexDirection.Row,
                              justifyContent = Justify.SpaceBetween,
                              marginTop = 8
                        }
                  };

                  var editBtn = new Button(static () => AssetDatabase.OpenAsset(Selection.activeObject))
                        { text = "📝 Edit", style = { flexGrow = 1, marginRight = 4 } };
                  var showBtn = new Button(static () => EditorGUIUtility.PingObject(Selection.activeObject))
                        { text = "📁 Show", style = { flexGrow = 1, marginRight = 4 } };

                  var copyPathBtn = new Button(static () => EditorGUIUtility.systemCopyBuffer = AssetDatabase.GetAssetPath(Selection.activeObject))
                        { text = "📋 Copy Path", style = { flexGrow = 1, marginRight = 4 } };

                  var copyCodeBtn = new Button(static () =>
                  {
                        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

                        if (!string.IsNullOrEmpty(path))
                        {
                              EditorGUIUtility.systemCopyBuffer = File.ReadAllText(path);
                        }
                  }) { text = "📄 Copy Code", style = { flexGrow = 1 } };

                  quickActions.Add(editBtn);
                  quickActions.Add(showBtn);
                  quickActions.Add(copyPathBtn);
                  quickActions.Add(copyCodeBtn);

                  container.Add(_scrollView);
                  container.Add(quickActions);

                  UpdateStyles();

                  return container;
            }

            private void RebuildLines(string[] processedLines)
            {
                  _linesContainer.Clear();

                  if (processedLines == null)
                  {
                        return;
                  }

                  foreach (string line in processedLines)
                  {
                        string displayLine = ReplaceSpacesOutsideTags((line ?? "").Replace("\t", "    "));

                        var label = new Label(displayLine)
                        {
                              enableRichText = true,
                              style =
                              {
                                    unityFont = _editorFont,
                                    fontSize = _settings.FontSize,
                                    color = new Color(0.85f, 0.85f, 0.85f),
                                    whiteSpace = WhiteSpace.NoWrap,
                              }
                        };
                        _linesContainer.Add(label);
                  }
            }

            private static string ReplaceSpacesOutsideTags(string line)
            {
                  var sb = new StringBuilder();
                  bool insideTag = false;

                  foreach (char c in line)
                  {
                        if (c == '<')
                        {
                              insideTag = true;
                        }

                        sb.Append(!insideTag && c == ' ' ? '\u00A0' : c);

                        if (c == '>')
                        {
                              insideTag = false;
                        }
                  }

                  return sb.ToString();
            }

            internal void UpdateCodeContent(string[] processedLines)
            {
                  if (_linesContainer == null)
                  {
                        return;
                  }

                  RebuildLines(processedLines);
            }

            internal void UpdateSearchableContent(string[] lines)
            {
                  _currentLines = lines;

                  if (_searchManager.HasSearchQuery)
                  {
                        _searchManager.PerformSearch(_currentLines);
                  }
            }

            private static VisualElement CreateFileStats(FileInfo fileInfo)
            {
                  var statsContainer = new VisualElement
                  {
                        style =
                        {
                              backgroundColor = EditorGUIUtility.isProSkin
                                    ? new Color(0.15f, 0.15f, 0.15f, 0.5f)
                                    : new Color(0.85f, 0.85f, 0.85f, 0.5f),
                              paddingTop = 8, paddingBottom = 8,
                              paddingLeft = 10, paddingRight = 10,
                              borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                              borderTopLeftRadius = 3, borderTopRightRadius = 3
                        }
                  };

                  var baseLabelStyle = new Action<Label>(static label =>
                  {
                        label.style.minWidth = 80;
                        label.style.flexGrow = 1;
                        label.style.fontSize = 11;
                  });

                  var row1 = new VisualElement
                        { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
                  var l1 = new Label($"📄 {fileInfo.TotalLines} lines");
                  var w1 = new Label($"📝 {fileInfo.TotalWords} words");
                  var c1 = new Label($"🔤 {fileInfo.TotalChars} chars");
                  baseLabelStyle(l1);
                  baseLabelStyle(w1);
                  baseLabelStyle(c1);
                  row1.Add(l1);
                  row1.Add(w1);
                  row1.Add(c1);

                  var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                  var s2 = new Label($"💾 {fileInfo.FormattedSize}");
                  var com2 = new Label($"💬 {fileInfo.CommentLines} comments");
                  var mod2 = new Label($"🕐 {fileInfo.LastModifiedTime:MM/dd HH:mm}");
                  baseLabelStyle(s2);
                  baseLabelStyle(com2);
                  baseLabelStyle(mod2);
                  row2.Add(s2);
                  row2.Add(com2);
                  row2.Add(mod2);

                  statsContainer.Add(row1);
                  statsContainer.Add(row2);

                  return statsContainer;
            }

            private VisualElement CreateSearchContent()
            {
                  var content = new VisualElement();

                  var sectionTitle = new Label("🔍 Search & Navigation")
                  {
                        style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11, marginBottom = 6 }
                  };
                  content.Add(sectionTitle);

                  var searchFieldRow = new VisualElement
                        { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                  var findLabel = new Label("Find:") { style = { width = 50, marginRight = 6 } };

                  _searchField = new TextField { value = _searchManager.SearchQuery, style = { flexGrow = 1, marginRight = 8 } };

                  _searchField.RegisterValueChangedCallback(evt =>
                  {
                        _searchManager.SearchQuery = evt.newValue;

                        if (_currentLines != null)
                        {
                              _searchManager.PerformSearch(_currentLines);
                        }
                  });

                  var caseToggle = new Toggle("Case Sensitive") { value = _searchManager.CaseSensitiveSearch };

                  caseToggle.RegisterValueChangedCallback(evt =>
                  {
                        _searchManager.CaseSensitiveSearch = evt.newValue;

                        if (_currentLines != null)
                        {
                              _searchManager.PerformSearch(_currentLines);
                        }
                  });

                  searchFieldRow.Add(findLabel);
                  searchFieldRow.Add(_searchField);
                  searchFieldRow.Add(caseToggle);

                  var navRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                  var navButtons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginRight = 8 } };

                  _prevButton = new Button(() =>
                  {
                        if (_searchManager.GoToPreviousResult())
                        {
                              ScrollToLine(_searchManager.GetCurrentResultLine());
                        }
                  }) { text = "◀ Previous", style = { marginRight = 4 } };
                  _prevButton.SetEnabled(_searchManager.HasSearchResults);

                  _nextButton = new Button(() =>
                  {
                        if (_searchManager.GoToNextResult())
                        {
                              ScrollToLine(_searchManager.GetCurrentResultLine());
                        }
                  }) { text = "Next ▶" };
                  _nextButton.SetEnabled(_searchManager.HasSearchResults);

                  navButtons.Add(_prevButton);
                  navButtons.Add(_nextButton);

                  _searchStatusLabel = new Label(_searchManager.GetSearchStatusText())
                        { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } };

                  var clearButton = new Button(() =>
                  {
                        _searchManager.ClearSearch();
                        _searchField.value = "";
                  }) { text = "✖ Clear" };

                  navRow.Add(navButtons);
                  navRow.Add(_searchStatusLabel);
                  navRow.Add(clearButton);

                  var goToRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                  var goToLabel = new Label("Go to Line:") { style = { width = 80, marginRight = 6 } };
                  var goToField = new IntegerField { value = _searchManager.GoToLine, style = { width = 80, marginRight = 6 } };
                  goToField.RegisterValueChangedCallback(evt => _searchManager.GoToLine = evt.newValue);
                  var goButton = new Button(() => ScrollToLine(_searchManager.GetGoToLineZeroBased())) { text = "Go", style = { width = 50 } };

                  goToRow.Add(goToLabel);
                  goToRow.Add(goToField);
                  goToRow.Add(goButton);

                  content.Add(searchFieldRow);
                  content.Add(navRow);
                  content.Add(goToRow);

                  return content;
            }

            private VisualElement CreateOptionsContent()
            {
                  var content = new VisualElement();

                  var sectionTitle = new Label("🎛️ Display Options")
                  {
                        style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11, marginBottom = 6 }
                  };
                  content.Add(sectionTitle);

                  var togglesContainer = new VisualElement { style = { marginBottom = 8 } };

                  _lineNumbersToggle = new Toggle("Show Line Numbers") { value = _settings.ShowLineNumbers, style = { marginBottom = 4 } };
                  _lineNumbersToggle.RegisterValueChangedCallback(evt => _settings.ShowLineNumbers = evt.newValue);

                  _syntaxHighlightingToggle = new Toggle("Enable Syntax Highlighting") { value = _settings.EnableSyntaxHighlighting };
                  _syntaxHighlightingToggle.RegisterValueChangedCallback(evt => _settings.EnableSyntaxHighlighting = evt.newValue);

                  togglesContainer.Add(_lineNumbersToggle);
                  togglesContainer.Add(_syntaxHighlightingToggle);

                  var fontSizeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                  var fsLabel = new Label("Font Size") { style = { width = 180, marginRight = 8 } };
                  _fontSizeSlider = new SliderInt(8, 20) { value = _settings.FontSize, showInputField = true, style = { flexGrow = 1 } };
                  _fontSizeSlider.RegisterValueChangedCallback(evt => _settings.FontSize = evt.newValue);
                  fontSizeRow.Add(fsLabel);
                  fontSizeRow.Add(_fontSizeSlider);

                  var heightRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
                  var hLabel = new Label("Preview Height") { style = { width = 180, marginRight = 8 } };
                  _previewHeightSlider = new SliderInt(200, 800) { value = _settings.PreviewHeight, showInputField = true, style = { flexGrow = 1 } };
                  _previewHeightSlider.RegisterValueChangedCallback(evt => _settings.PreviewHeight = evt.newValue);
                  heightRow.Add(hLabel);
                  heightRow.Add(_previewHeightSlider);

                  var maxLinesRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
                  var mlLabel = new Label("Max Lines for Highlighting") { style = { width = 180, marginRight = 8 } };
                  _maxLinesSlider = new SliderInt(100, 10000)
                        { value = _settings.MaxLinesToDisplay, showInputField = true, style = { flexGrow = 1 } };
                  _maxLinesSlider.RegisterValueChangedCallback(evt => _settings.MaxLinesToDisplay = evt.newValue);
                  maxLinesRow.Add(mlLabel);
                  maxLinesRow.Add(_maxLinesSlider);

                  var resetButton = new Button(() =>
                  {
                        _settings.ResetToDefaults();
                        _lineNumbersToggle.value = _settings.ShowLineNumbers;
                        _syntaxHighlightingToggle.value = _settings.EnableSyntaxHighlighting;
                        _fontSizeSlider.value = _settings.FontSize;
                        _previewHeightSlider.value = _settings.PreviewHeight;
                        _maxLinesSlider.value = _settings.MaxLinesToDisplay;
                  }) { text = "Reset to Defaults", style = { alignSelf = Align.FlexEnd } };

                  content.Add(togglesContainer);
                  content.Add(fontSizeRow);
                  content.Add(heightRow);
                  content.Add(maxLinesRow);
                  content.Add(resetButton);

                  return content;
            }

            private void UpdateStyles()
            {
                  if (_scrollView != null)
                  {
                        _scrollView.style.height = _settings.PreviewHeight;
                  }

                  if (_linesContainer == null)
                  {
                        return;
                  }

                  foreach (VisualElement child in _linesContainer.Children())
                  {
                        if (child is Label lbl)
                        {
                              lbl.style.fontSize = _settings.FontSize;
                        }
                  }
            }

            private void UpdateSearchStatus()
            {
                  if (_searchStatusLabel == null)
                  {
                        return;
                  }

                  _searchStatusLabel.text = _searchManager.GetSearchStatusText();
                  _prevButton.SetEnabled(_searchManager.HasSearchResults);
                  _nextButton.SetEnabled(_searchManager.HasSearchResults);
            }

            private void ScrollToLine(int lineIndex)
            {
                  _scrollView.schedule.Execute(() =>
                  {
                        if (lineIndex < 0 || _linesContainer.childCount == 0)
                        {
                              return;
                        }

                        int clamped = Mathf.Clamp(lineIndex, 0, _linesContainer.childCount - 1);
                        VisualElement lineEl = _linesContainer[clamped];

                        lineEl.RegisterCallbackOnce<GeometryChangedEvent>(_ => CenterOnElement(lineEl));
                        CenterOnElement(lineEl);
                  });
            }

            private void CenterOnElement(VisualElement lineEl)
            {
                  float elementY = lineEl.layout.y;
                  float elementHeight = lineEl.layout.height;
                  float viewportHeight = _scrollView.contentViewport.layout.height;

                  float targetY = elementY - (viewportHeight / 2f) + (elementHeight / 2f);
                  targetY = Mathf.Max(0, targetY);

                  _scrollView.scrollOffset = new Vector2(0, targetY);
            }

            private static VisualElement CreateSpacer(int height = 10) => new() { style = { height = height } };

            private static (string, Color) GetScriptTypeInfo(MonoScript script)
            {
                  Type classType = script.GetClass();

                  if (classType == null)
                  {
                        return ("Unknown", Color.gray);
                  }

                  if (classType.IsSubclassOf(typeof(UnityEditor.Editor)))
                  {
                        return ("Editor Script", Color.magenta);
                  }

                  if (classType.IsSubclassOf(typeof(MonoBehaviour)))
                  {
                        return ("MonoBehaviour", Color.green);
                  }

                  if (classType.IsSubclassOf(typeof(ScriptableObject)))
                  {
                        return ("ScriptableObject", Color.cyan);
                  }

                  if (classType.IsSubclassOf(typeof(EditorWindow)))
                  {
                        return ("Editor Window", Color.yellow);
                  }

                  return ("Class", Color.white);
            }

            private static (string, Color) GetTextAssetTypeInfo(ScriptType scriptType) => scriptType switch
            {
                  ScriptType.Json => ("JSON Data", Color.yellow),
                  ScriptType.XML => ("XML Document", Color.cyan),
                  ScriptType.Readme => ("Readme File", Color.green),
                  ScriptType.Yaml => ("YAML Data", Color.magenta),
                  _ => ("Text File", Color.gray)
            };

            private static GUIContent GetIconForType(ScriptType scriptType) =>
                  scriptType is ScriptType.Json or ScriptType.XML or ScriptType.Readme or ScriptType.Yaml
                        ? EditorGUIUtility.IconContent("TextAsset Icon")
                        : EditorGUIUtility.IconContent("DefaultAsset Icon");

            private static string GetExtensionForType(ScriptType scriptType) => scriptType switch
            {
                  ScriptType.Json => ".json",
                  ScriptType.XML => ".xml",
                  ScriptType.Readme => ".md",
                  ScriptType.Yaml => ".yml",
                  _ => ""
            };
      }
}