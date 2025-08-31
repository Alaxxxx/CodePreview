using System;
using System.IO;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Helpers;
using OpalStudio.CodePreview.Editor.Highlighters;
using OpalStudio.CodePreview.Editor.Settings;
using OpalStudio.CodePreview.Editor.View;
using UnityEditor;
using UnityEngine;
using FileInfo = OpalStudio.CodePreview.Editor.Data.FileInfo;

namespace OpalStudio.CodePreview.Editor
{
      [CustomEditor(typeof(TextAsset), true)]
      public sealed class TextAssetCodePreview : UnityEditor.Editor
      {
            private UnityEditor.Editor _defaultEditor;

            private FileManager _fileManager;
            private UIRenderer _uiRenderer;
            private SearchManager _searchManager;
            private PreviewSettings _settings;
            private SyntaxHighlighter _syntaxHighlighter;

            private TextAsset _lastAsset;
            private bool _needsRefresh = true;

            private void OnEnable()
            {
                  var textAsset = (TextAsset)target;

                  if (!IsFileHandled(textAsset))
                  {
                        _defaultEditor = CreateEditor(target, Type.GetType("UnityEditor.TextAssetInspector, UnityEditor"));
                  }
            }

            private void OnDisable()
            {
                  if (_defaultEditor != null)
                  {
                        DestroyImmediate(_defaultEditor);
                  }
            }

            public override void OnInspectorGUI()
            {
                  GUI.enabled = true;

                  var textAsset = (TextAsset)target;

                  if (IsFileHandled(textAsset))
                  {
                        DrawCodePreview(textAsset);
                  }
                  else
                  {
                        if (_defaultEditor)
                        {
                              _defaultEditor.OnInspectorGUI();
                        }
                        else
                        {
                              DrawDefaultInspector();
                        }
                  }
            }

            private static bool IsFileHandled(TextAsset textAsset)
            {
                  string filePath = AssetDatabase.GetAssetPath(textAsset);
                  ScriptType scriptType = ScriptTypeDetector.DetectType(filePath);

                  return scriptType != ScriptType.Unknown;
            }

            private void DrawCodePreview(TextAsset textAsset)
            {
                  if (_fileManager == null)
                  {
                        InitializeComponents();
                        _settings.LoadPreferences();
                  }

                  string filePath = AssetDatabase.GetAssetPath(textAsset);
                  bool hasChanges = HasFileChanged(filePath);

                  if (textAsset != _lastAsset || _needsRefresh || hasChanges)
                  {
                        RefreshContent(textAsset);
                        _lastAsset = textAsset;
                        _needsRefresh = false;
                  }

                  if (_searchManager.HasSearchQueryChanged())
                  {
                        _searchManager.PerformSearch(_fileManager!.GetLines());
                        _syntaxHighlighter.UpdateSearchHighlighting(_searchManager.GetSearchQuery(), _searchManager.GetSearchResults());
                  }

                  ScriptType scriptType = ScriptTypeDetector.DetectType(filePath);
                  UIRenderer.DrawHeaderForTextAsset(textAsset, _fileManager!.GetFileInfo(), scriptType);
                  EditorGUILayout.Space(8);
                  _uiRenderer.DrawSearchSection(_searchManager);
                  EditorGUILayout.Space(8);
                  _uiRenderer.DrawOptionsSection();
                  EditorGUILayout.Space(8);
                  _uiRenderer.DrawCodePreview(_syntaxHighlighter.GetProcessedContent(), _fileManager.GetLines());

                  GUI.enabled = true;
            }

            private void InitializeComponents()
            {
                  _settings = new PreviewSettings();
                  _fileManager = new FileManager();
                  _searchManager = new SearchManager();
                  _uiRenderer = new UIRenderer(_settings);
                  _syntaxHighlighter = new SyntaxHighlighter();

                  // Wire up events
                  _searchManager.OnSearchResultsChanged += OnSearchResultsChanged;
                  _settings.OnSettingsChanged += OnSettingsChanged;
            }

            private void RefreshContent(TextAsset textAsset)
            {
                  try
                  {
                        string filePath = AssetDatabase.GetAssetPath(textAsset);
                        string[] lines = textAsset.text.Split('\n');

                        if (lines.Length > _settings.MaxLinesToDisplay)
                        {
                              string[] limitedLines = new string[_settings.MaxLinesToDisplay];
                              Array.Copy(lines, limitedLines, _settings.MaxLinesToDisplay);
                              _fileManager.LoadFromContent(limitedLines, filePath);
                              _fileManager.SetLimitedLines(limitedLines);
                        }
                        else
                        {
                              _fileManager.LoadFromContent(lines, filePath);
                        }

                        ScriptType scriptType = ScriptTypeDetector.DetectType(filePath);
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);
                        _searchManager.PerformSearch(_fileManager.GetDisplayLines());
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error refreshing content: {e.Message}");
                        _syntaxHighlighter.SetErrorContent($"Error loading file: {e.Message}");
                  }
            }

            private bool HasFileChanged(string filePath)
            {
                  if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                  {
                        return false;
                  }

                  FileInfo fileInfo = _fileManager.GetFileInfo();

                  if (fileInfo == null)
                  {
                        return true;
                  }

                  return File.GetLastWriteTime(filePath) != fileInfo.LastModifiedTime;
            }

            private void OnSearchResultsChanged()
            {
                  _syntaxHighlighter.UpdateSearchHighlighting(_searchManager.GetSearchQuery(), _searchManager.GetSearchResults());
            }

            private void OnSettingsChanged()
            {
                  _uiRenderer.RefreshStyles();

                  if (_fileManager.HasContent())
                  {
                        string filePath = AssetDatabase.GetAssetPath(_lastAsset);
                        ScriptType scriptType = ScriptTypeDetector.DetectType(filePath);
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);
                  }
            }
      }
}