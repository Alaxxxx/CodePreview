using System;
using System.IO;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Helpers;
using OpalStudio.CodePreview.Editor.Settings;
using UnityEditor;
using UnityEngine;
using FileInfo = OpalStudio.CodePreview.Editor.Data.FileInfo;
using Object = UnityEngine.Object;

namespace OpalStudio.CodePreview.Editor.View
{
      public sealed class UIRenderer : IDisposable
      {
            private readonly PreviewSettings _settings;

            // UI State
            private Vector2 _scrollPosition;
            private GUIStyle _codeStyle;
            private GUIStyle _scrollViewStyle;

            // Scroll to line functionality
            private bool _scrollToLineRequested;
            private int _targetLineIndex;

            public UIRenderer(PreviewSettings settings)
            {
                  _settings = settings;
            }

            public void RefreshStyles()
            {
                  _codeStyle = null;
                  _scrollViewStyle = null;
            }

            private void InitializeStyles()
            {
                  if (_codeStyle == null || _scrollViewStyle == null || _codeStyle.fontSize != _settings.FontSize)
                  {
                        bool isDark = EditorGUIUtility.isProSkin;
                        Color backgroundColor = isDark ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.94f, 0.94f, 0.94f, 1f);

                        var backgroundTexture = new Texture2D(1, 1);
                        backgroundTexture.SetPixel(0, 0, backgroundColor);
                        backgroundTexture.Apply();

                        _scrollViewStyle = new GUIStyle
                        {
                                    normal = { background = backgroundTexture }
                        };

                        _codeStyle = new GUIStyle(GUI.skin.label)
                        {
                                    fontSize = _settings.FontSize,
                                    richText = true,
                                    wordWrap = false,
                                    padding = new RectOffset(10, 10, 5, 5),
                                    font = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene).font,
                                    normal = { textColor = isDark ? Color.white : Color.black }
                        };

                        _codeStyle.hover = _codeStyle.normal;
                        _codeStyle.active = _codeStyle.normal;
                        _codeStyle.focused = _codeStyle.normal;
                  }
            }

#region Headers

            public static void DrawHeader(MonoScript script, FileInfo fileInfo)
            {
                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  EditorGUILayout.BeginHorizontal();
                  GUIContent icon = EditorGUIUtility.IconContent("cs Script Icon");
                  GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                  EditorGUILayout.LabelField($"{script.name}.cs", EditorStyles.boldLabel);
                  GUILayout.FlexibleSpace();
                  DrawScriptTypeInfo(script);
                  EditorGUILayout.EndHorizontal();

                  if (fileInfo != null)
                  {
                        EditorGUILayout.Space(3);
                        DrawFileStats(fileInfo);
                  }

                  EditorGUILayout.EndVertical();
            }

            public static void DrawHeaderForTextAsset(TextAsset textAsset, FileInfo fileInfo, ScriptType scriptType)
            {
                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  EditorGUILayout.BeginHorizontal();

                  GUIContent icon = scriptType switch
                  {
                              ScriptType.Json or ScriptType.XML or ScriptType.Readme or ScriptType.Yaml => EditorGUIUtility.IconContent("TextAsset Icon"),
                              _ => EditorGUIUtility.IconContent("DefaultAsset Icon")
                  };

                  GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));

                  string fileName = textAsset.name + GetExtensionForType(scriptType);
                  EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel);

                  GUILayout.FlexibleSpace();
                  DrawTextAssetTypeInfo(scriptType);
                  EditorGUILayout.EndHorizontal();

                  if (fileInfo != null)
                  {
                        EditorGUILayout.Space(3);
                        DrawFileStats(fileInfo);
                  }

                  EditorGUILayout.EndVertical();
            }

#endregion

#region Info Sections

            private static void DrawTextAssetTypeInfo(ScriptType scriptType)
            {
                  (string label, Color color, string tooltip) = scriptType switch
                  {
                              ScriptType.Json => ("JSON Data", Color.yellow, "JavaScript Object Notation configuration file"),
                              ScriptType.XML => ("XML Document", Color.cyan, "Extensible Markup Language document"),
                              ScriptType.Readme => ("Readme File", Color.green, "Markdown or text readme file"),
                              ScriptType.Yaml => ("YAML Data", Color.magenta, "YAML Ain't Markup Language configuration file"),
                              _ => ("Text File", Color.gray, "Plain text file")
                  };

                  Color oldColor = GUI.color;
                  GUI.color = color;
                  var typeContent = new GUIContent(label, tooltip);
                  EditorGUILayout.LabelField(typeContent, EditorStyles.miniLabel, GUILayout.Width(100));
                  GUI.color = oldColor;
            }

            private static void DrawScriptTypeInfo(MonoScript script)
            {
                  Type classType = script.GetClass();

                  if (classType == null)
                  {
                        var unknownContent = new GUIContent("Unknown", "Script class could not be loaded");
                        EditorGUILayout.LabelField(unknownContent, EditorStyles.miniLabel, GUILayout.Width(80));

                        return;
                  }

                  string typeLabel = "Class";
                  Color typeColor = Color.white;
                  string tooltip = "C# class";

                  if (classType.IsSubclassOf(typeof(UnityEditor.Editor)))
                  {
                        typeLabel = "Editor Script";
                        typeColor = Color.magenta;
                        tooltip = "Custom Unity Editor script";
                  }
                  else if (classType.IsSubclassOf(typeof(MonoBehaviour)))
                  {
                        typeLabel = "MonoBehaviour";
                        typeColor = Color.green;
                        tooltip = "Unity component script that can be attached to GameObjects";
                  }
                  else if (classType.IsSubclassOf(typeof(ScriptableObject)))
                  {
                        typeLabel = "ScriptableObject";
                        typeColor = Color.cyan;
                        tooltip = "Unity data container script";
                  }
                  else if (classType.IsSubclassOf(typeof(EditorWindow)))
                  {
                        typeLabel = "Editor Window";
                        typeColor = Color.yellow;
                        tooltip = "Custom Unity Editor window script";
                  }
                  else if (classType.IsSubclassOf(typeof(PropertyDrawer)))
                  {
                        typeLabel = "Property Drawer";
                        typeColor = Color.blue;
                        tooltip = "Custom property drawer for Inspector";
                  }
                  else if (classType.IsEnum)
                  {
                        typeLabel = "Enum";
                        typeColor = Color.gray;
                        tooltip = "Enumeration type";
                  }
                  else if (classType.IsInterface)
                  {
                        typeLabel = "Interface";
                        typeColor = Color.white;
                        tooltip = "C# interface";
                  }
                  else if (classType.IsAbstract)
                  {
                        typeLabel = "Abstract Class";
                        typeColor = Color.white;
                        tooltip = "Abstract C# class";
                  }

                  Color oldColor = GUI.color;
                  GUI.color = typeColor;
                  var typeContent = new GUIContent(typeLabel, tooltip);
                  EditorGUILayout.LabelField(typeContent, EditorStyles.miniLabel, GUILayout.Width(100));
                  GUI.color = oldColor;
            }

#endregion

#region File Stats

            private static void DrawFileStats(FileInfo fileInfo)
            {
                  EditorGUILayout.BeginHorizontal();
                  var linesContent = new GUIContent($"📄 {fileInfo.totalLines} lines", "Total number of lines in the script");
                  EditorGUILayout.LabelField(linesContent, EditorStyles.miniLabel, GUILayout.Width(80));
                  var wordsContent = new GUIContent($"📝 {fileInfo.totalWords} words", "Total number of words in the script");
                  EditorGUILayout.LabelField(wordsContent, EditorStyles.miniLabel, GUILayout.Width(100));
                  var charsContent = new GUIContent($"🔤 {fileInfo.totalChars} chars", "Total number of characters in the script");
                  EditorGUILayout.LabelField(charsContent, EditorStyles.miniLabel, GUILayout.Width(100));
                  GUILayout.FlexibleSpace();
                  EditorGUILayout.EndHorizontal();

                  EditorGUILayout.BeginHorizontal();
                  var sizeContent = new GUIContent($"💾 {fileInfo.FormattedSize}", "File size on disk");
                  EditorGUILayout.LabelField(sizeContent, EditorStyles.miniLabel, GUILayout.Width(80));
                  var commentsContent = new GUIContent($"💬 {fileInfo.commentLines} comments", "Number of lines containing comments");
                  EditorGUILayout.LabelField(commentsContent, EditorStyles.miniLabel, GUILayout.Width(120));
                  var modifiedContent = new GUIContent($"🕐 {fileInfo.LastModifiedTime:MM/dd HH:mm}", "Last modification date and time");
                  EditorGUILayout.LabelField(modifiedContent, EditorStyles.miniLabel, GUILayout.Width(120));
                  GUILayout.FlexibleSpace();
                  EditorGUILayout.EndHorizontal();
            }

#endregion

#region Search & Navigation

            public void DrawSearchSection(SearchManager searchManager)
            {
                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  bool newSearchFoldout = EditorGUILayout.Foldout(_settings.SearchFoldout, "🔍 Search & Navigation", true);

                  if (newSearchFoldout != _settings.SearchFoldout)
                  {
                        _settings.SearchFoldout = newSearchFoldout;
                  }

                  if (_settings.SearchFoldout)
                  {
                        EditorGUILayout.Space(3);

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Find:", GUILayout.Width(35));
                        GUI.SetNextControlName("SearchField");
                        string newSearch = EditorGUILayout.TextField(searchManager.SearchQuery);

                        if (newSearch != searchManager.SearchQuery)
                        {
                              searchManager.SearchQuery = newSearch;
                        }

                        GUILayout.Space(8);
                        var caseSensitiveContent = new GUIContent("Case sensitive", "Search with case sensitivity");
                        bool newCaseSensitive = EditorGUILayout.ToggleLeft(caseSensitiveContent, searchManager.CaseSensitiveSearch, GUILayout.Width(100));

                        if (newCaseSensitive != searchManager.CaseSensitiveSearch)
                        {
                              searchManager.CaseSensitiveSearch = newCaseSensitive;
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space(3);

                        DrawSearchNavigation(searchManager);

                        EditorGUILayout.Space(3);

                        DrawGoToLineSection(searchManager);
                  }

                  EditorGUILayout.EndVertical();
            }

            private void DrawSearchNavigation(SearchManager searchManager)
            {
                  EditorGUILayout.BeginHorizontal();

                  GUI.enabled = searchManager.HasSearchResults;
                  var previousContent = new GUIContent("◀ Previous", "Go to previous search result");

                  if (GUILayout.Button(previousContent, EditorStyles.miniButton, GUILayout.Width(80)) && searchManager.GoToPreviousResult())
                  {
                        ScrollToLine(searchManager.GetCurrentResultLine());
                  }

                  var nextContent = new GUIContent("Next ▶", "Go to next search result");

                  if (GUILayout.Button(nextContent, EditorStyles.miniButton, GUILayout.Width(80)) && searchManager.GoToNextResult())
                  {
                        ScrollToLine(searchManager.GetCurrentResultLine());
                  }

                  GUI.enabled = true;

                  GUILayout.Space(8);

                  string statusText = searchManager.GetSearchStatusText();

                  if (!string.IsNullOrEmpty(statusText))
                  {
                        var resultsContent = new GUIContent(statusText, "Current result / Total results");
                        EditorGUILayout.LabelField(resultsContent, EditorStyles.miniLabel, GUILayout.Width(60));
                  }

                  GUILayout.FlexibleSpace();

                  if (searchManager.HasSearchQuery)
                  {
                        var clearContent = new GUIContent("✖ Clear", "Clear search query and results");

                        if (GUILayout.Button(clearContent, EditorStyles.miniButton, GUILayout.Width(60)))
                        {
                              searchManager.ClearSearch();
                        }
                  }

                  EditorGUILayout.EndHorizontal();
            }

            private void DrawGoToLineSection(SearchManager searchManager)
            {
                  EditorGUILayout.BeginHorizontal();
                  var goToLineContent = new GUIContent("Go to line:", "Jump to a specific line number in the code");
                  EditorGUILayout.LabelField(goToLineContent, GUILayout.Width(70));

                  int newGoToLine = EditorGUILayout.IntField(searchManager.GoToLine, GUILayout.Width(60));
                  searchManager.SetGoToLine(newGoToLine);

                  var goButtonContent = new GUIContent("Go", "Jump to the specified line");

                  if (GUILayout.Button(goButtonContent, EditorStyles.miniButton, GUILayout.Width(40)))
                  {
                        ScrollToLine(searchManager.GetGoToLineZeroBased());
                  }

                  GUILayout.FlexibleSpace();
                  EditorGUILayout.EndHorizontal();
            }

#endregion

#region Options

            public void DrawOptionsSection()
            {
                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                  bool newOptionsFoldout = EditorGUILayout.Foldout(_settings.OptionsFoldout, "⚙️ Display Options", true);

                  if (newOptionsFoldout != _settings.OptionsFoldout)
                  {
                        _settings.OptionsFoldout = newOptionsFoldout;
                  }

                  if (_settings.OptionsFoldout)
                  {
                        EditorGUILayout.Space(3);

                        DrawToggleOption("Show line numbers", "Toggle display of line numbers in the preview", _settings.ShowLineNumbers,
                                    value => _settings.ShowLineNumbers = value);

                        EditorGUILayout.Space(3);

                        DrawSliderOption("Font size:", "Adjust the font size of the code preview", _settings.FontSize, 8, 20, value => _settings.FontSize = value, 70);

                        EditorGUILayout.Space(3);

                        DrawSliderOption("Preview height:", "Adjust the height of the code preview area", _settings.PreviewHeight, 200, 800,
                                    value => _settings.PreviewHeight = value, 90);

                        EditorGUILayout.Space(3);

                        DrawSliderOption("Max lines:", "Maximum number of lines to display for performance", _settings.MaxLinesToDisplay, 100, 10000,
                                    value => _settings.MaxLinesToDisplay = value, 70);

                        EditorGUILayout.Space(3);

                        DrawToggleOption("Enable syntax highlighting", "Enable or disable syntax highlighting for better performance", _settings.EnableSyntaxHighlighting,
                                    value => _settings.EnableSyntaxHighlighting = value);

                        EditorGUILayout.Space(3);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("Reset to Defaults", EditorStyles.miniButton, GUILayout.Width(120)))
                        {
                              _settings.ResetToDefaults();
                        }

                        EditorGUILayout.EndHorizontal();
                  }

                  EditorGUILayout.EndVertical();
            }

            private static void DrawToggleOption(string label, string tooltip, bool currentValue, Action<bool> setValue)
            {
                  EditorGUILayout.BeginHorizontal();
                  var content = new GUIContent(label, tooltip);
                  bool newValue = EditorGUILayout.Toggle(content, currentValue);

                  if (newValue != currentValue)
                  {
                        setValue(newValue);
                  }

                  EditorGUILayout.EndHorizontal();
            }

            private static void DrawSliderOption(string label, string tooltip, int currentValue, int minValue, int maxValue, Action<int> setValue, int labelWidth)
            {
                  EditorGUILayout.BeginHorizontal();
                  var content = new GUIContent(label, tooltip);
                  EditorGUILayout.LabelField(content, GUILayout.Width(labelWidth));
                  int newValue = EditorGUILayout.IntSlider(currentValue, minValue, maxValue);

                  if (newValue != currentValue)
                  {
                        setValue(newValue);
                  }

                  EditorGUILayout.EndHorizontal();
            }

#endregion

#region Code Preview

            public void DrawCodePreview(string processedContent, string[] lines)
            {
                  EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                  EditorGUILayout.LabelField("📄 Code Preview", EditorStyles.boldLabel);
                  EditorGUILayout.Space(5);

                  InitializeStyles();

                  _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, _scrollViewStyle, GUILayout.Height(_settings.PreviewHeight));
                  GUILayout.Label(processedContent, _codeStyle);
                  EditorGUILayout.EndScrollView();

                  HandleScrollToLine(lines);

                  EditorGUILayout.Space(5);
                  DrawQuickActions();
                  EditorGUILayout.EndVertical();
            }

#endregion

#region Quick Actions

            private static void DrawQuickActions()
            {
                  EditorGUILayout.BeginHorizontal();

                  var editContent = new GUIContent("📝 Edit Script", "Open the script in your default code editor");

                  if (GUILayout.Button(editContent, EditorStyles.miniButton))
                  {
                        AssetDatabase.OpenAsset(Selection.activeObject);
                  }

                  var showContent = new GUIContent("📁 Show in Project", "Highlight the script in the Project window");

                  if (GUILayout.Button(showContent, EditorStyles.miniButton))
                  {
                        EditorGUIUtility.PingObject(Selection.activeObject);
                  }

                  var copyPathContent = new GUIContent("📋 Copy Path", "Copy the script's file path to clipboard");

                  if (GUILayout.Button(copyPathContent, EditorStyles.miniButton))
                  {
                        EditorGUIUtility.systemCopyBuffer = AssetDatabase.GetAssetPath(Selection.activeObject);
                  }

                  var copyCodeContent = new GUIContent("📄 Copy Code", "Copy the entire script content to clipboard");

                  if (GUILayout.Button(copyCodeContent, EditorStyles.miniButton))
                  {
                        var script = Selection.activeObject as MonoScript;

                        if (script)
                        {
                              string path = AssetDatabase.GetAssetPath(script);

                              if (!string.IsNullOrEmpty(path))
                              {
                                    EditorGUIUtility.systemCopyBuffer = File.ReadAllText(path);
                              }
                        }
                  }

                  EditorGUILayout.EndHorizontal();
            }

#endregion

            private void HandleScrollToLine(string[] lines)
            {
                  if (_scrollToLineRequested && Event.current.type == EventType.Repaint)
                  {
                        _scrollToLineRequested = false;

                        if (lines != null && _targetLineIndex >= 0 && _targetLineIndex < lines.Length)
                        {
                              float baseLineHeight = _settings.FontSize * 1.2f;
                              float paddingPerLine = (_codeStyle.padding.top + _codeStyle.padding.bottom) / 10f;
                              float calculatedLineHeight = baseLineHeight + paddingPerLine;
                              float targetY = _targetLineIndex * calculatedLineHeight;
                              float centeredY = targetY - (_settings.PreviewHeight / 2f);
                              float totalContentHeight = lines.Length * calculatedLineHeight + _codeStyle.padding.vertical;
                              float maxScrollY = Mathf.Max(0, totalContentHeight - _settings.PreviewHeight);
                              _scrollPosition.y = Mathf.Clamp(centeredY, 0, maxScrollY);

                              if (EditorWindow.focusedWindow)
                              {
                                    EditorWindow.focusedWindow.Repaint();
                              }
                        }
                  }
            }

            public void ScrollToLine(int lineIndex)
            {
                  _targetLineIndex = lineIndex;
                  _scrollToLineRequested = true;
            }

            private static string GetExtensionForType(ScriptType scriptType)
            {
                  return scriptType switch
                  {
                              ScriptType.Json => ".json",
                              ScriptType.XML => ".xml",
                              ScriptType.Readme => ".md",
                              ScriptType.Yaml => ".yml",
                              _ => ""
                  };
            }

            public void Dispose()
            {
                  if (_scrollViewStyle?.normal.background != null)
                  {
                        Object.DestroyImmediate(_scrollViewStyle.normal.background);
                  }
            }
      }
}