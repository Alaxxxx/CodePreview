using System;
using CodePreview.Editor.Data;
using CodePreview.Editor.Helpers;
using CodePreview.Editor.Highlighters;
using CodePreview.Editor.Settings;
using CodePreview.Editor.View;
using UnityEditor;
using UnityEngine;

namespace CodePreview.Editor
{
      [CustomEditor(typeof(MonoScript))]
      public sealed class EnhancedScriptPreview : UnityEditor.Editor
      {
            // Core components
            private FileManager _fileManager;
            private UIRenderer _uiRenderer;
            private SearchManager _searchManager;
            private PreviewSettings _settings;
            private SyntaxHighlighter _syntaxHighlighter;

            // Internal state
            private MonoScript _lastScript;
            private bool _needsRefresh = true;

            private void OnEnable()
            {
                  InitializeComponents();
                  _settings.LoadPreferences();
            }

            private void OnDisable()
            {
                  _settings.SavePreferences();
                  _uiRenderer?.Dispose();
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

            public override void OnInspectorGUI()
            {
                  var script = (MonoScript)target;

                  if (!script)
                  {
                        return;
                  }

                  bool hasChanges = _fileManager.CheckForChanges(script);

                  if (script != _lastScript || _needsRefresh || hasChanges)
                  {
                        RefreshContent(script);
                        _lastScript = script;
                        _needsRefresh = false;
                  }

                  if (_searchManager.HasSearchQueryChanged())
                  {
                        _searchManager.PerformSearch(_fileManager.GetLines());
                        _syntaxHighlighter.UpdateSearchHighlighting(_searchManager.GetSearchQuery(), _searchManager.GetSearchResults());
                  }

                  UIRenderer.DrawHeader(script, _fileManager.GetFileInfo());
                  EditorGUILayout.Space(8);
                  _uiRenderer.DrawSearchSection(_searchManager);
                  EditorGUILayout.Space(8);
                  _uiRenderer.DrawOptionsSection();
                  EditorGUILayout.Space(8);
                  _uiRenderer.DrawCodePreview(_syntaxHighlighter.GetProcessedContent(), _fileManager.GetLines());
            }

            private void RefreshContent(MonoScript script)
            {
                  try
                  {
                        _fileManager.LoadScript(script);
                        string[] lines = _fileManager.GetLines();

                        if (lines != null && lines.Length > _settings.MaxLinesToDisplay)
                        {
                              string[] limitedLines = new string[_settings.MaxLinesToDisplay];
                              Array.Copy(lines, limitedLines, _settings.MaxLinesToDisplay);
                              _fileManager.SetLimitedLines(limitedLines);

                              Debug.LogWarning(
                                          $"Script preview limited to {_settings.MaxLinesToDisplay} lines for performance. " + $"Full file has {lines.Length} lines.");
                        }

                        ScriptType scriptType = ScriptTypeDetector.DetectType(_fileManager.GetFilePath());
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);
                        _searchManager.PerformSearch(_fileManager.GetDisplayLines());
                  }
                  catch (Exception e)
                  {
                        Debug.LogError($"Error refreshing script content: {e.Message}");
                        _syntaxHighlighter.SetErrorContent($"Error loading file: {e.Message}");
                  }
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
                        ScriptType scriptType = ScriptTypeDetector.DetectType(_fileManager.GetFilePath());
                        _syntaxHighlighter.ProcessContent(_fileManager.GetDisplayLines(), scriptType, _settings);
                  }
            }
      }
}