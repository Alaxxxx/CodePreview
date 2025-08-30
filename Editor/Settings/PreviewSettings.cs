using System;
using UnityEditor;
using UnityEngine;

namespace CodePreview.Editor.Settings
{
      [Serializable]
      public class PreviewSettings
      {
            // EditorPrefs keys
            private const string PrefFontSize = "EnhancedScriptPreview_FontSize";
            private const string PrefPreviewHeight = "EnhancedScriptPreview_PreviewHeight";
            private const string PrefShowLineNumbers = "EnhancedScriptPreview_ShowLineNumbers";
            private const string PrefSearchFoldout = "EnhancedScriptPreview_SearchFoldout";
            private const string PrefOptionsFoldout = "EnhancedScriptPreview_OptionsFoldout";
            private const string PrefMaxLinesToDisplay = "EnhancedScriptPreview_MaxLinesToDisplay";
            private const string PrefEnableSyntaxHighlighting = "EnhancedScriptPreview_EnableSyntaxHighlighting";

            // Events
            public event Action OnSettingsChanged;

            private int _fontSize = 11;
            public int FontSize
            {
                  get => _fontSize;
                  set
                  {
                        if (_fontSize != value)
                        {
                              _fontSize = Mathf.Clamp(value, 8, 20);
                              EditorPrefs.SetInt(PrefFontSize, _fontSize);
                              OnSettingsChanged?.Invoke();
                        }
                  }
            }

            private int _previewHeight = 400;
            public int PreviewHeight
            {
                  get => _previewHeight;
                  set
                  {
                        if (_previewHeight != value)
                        {
                              _previewHeight = Mathf.Clamp(value, 200, 800);
                              EditorPrefs.SetInt(PrefPreviewHeight, _previewHeight);
                              OnSettingsChanged?.Invoke();
                        }
                  }
            }

            private bool _showLineNumbers = true;
            public bool ShowLineNumbers
            {
                  get => _showLineNumbers;
                  set
                  {
                        if (_showLineNumbers != value)
                        {
                              _showLineNumbers = value;
                              EditorPrefs.SetBool(PrefShowLineNumbers, _showLineNumbers);
                              OnSettingsChanged?.Invoke();
                        }
                  }
            }

            private bool _searchFoldout = true;
            public bool SearchFoldout
            {
                  get => _searchFoldout;
                  set
                  {
                        if (_searchFoldout != value)
                        {
                              _searchFoldout = value;
                              EditorPrefs.SetBool(PrefSearchFoldout, _searchFoldout);
                        }
                  }
            }

            private bool _optionsFoldout;
            public bool OptionsFoldout
            {
                  get => _optionsFoldout;
                  set
                  {
                        if (_optionsFoldout != value)
                        {
                              _optionsFoldout = value;
                              EditorPrefs.SetBool(PrefOptionsFoldout, _optionsFoldout);
                        }
                  }
            }

            private int _maxLinesToDisplay = 1000;
            public int MaxLinesToDisplay
            {
                  get => _maxLinesToDisplay;
                  set
                  {
                        if (_maxLinesToDisplay != value)
                        {
                              _maxLinesToDisplay = Mathf.Clamp(value, 100, 10000);
                              EditorPrefs.SetInt(PrefMaxLinesToDisplay, _maxLinesToDisplay);
                              OnSettingsChanged?.Invoke();
                        }
                  }
            }

            private bool _enableSyntaxHighlighting = true;
            public bool EnableSyntaxHighlighting
            {
                  get => _enableSyntaxHighlighting;
                  set
                  {
                        if (_enableSyntaxHighlighting != value)
                        {
                              _enableSyntaxHighlighting = value;
                              EditorPrefs.SetBool(PrefEnableSyntaxHighlighting, _enableSyntaxHighlighting);
                              OnSettingsChanged?.Invoke();
                        }
                  }
            }

            public bool IsDarkTheme => EditorGUIUtility.isProSkin;

            public void LoadPreferences()
            {
                  _fontSize = EditorPrefs.GetInt(PrefFontSize, 11);
                  _previewHeight = EditorPrefs.GetInt(PrefPreviewHeight, 400);
                  _showLineNumbers = EditorPrefs.GetBool(PrefShowLineNumbers, true);
                  _searchFoldout = EditorPrefs.GetBool(PrefSearchFoldout, true);
                  _optionsFoldout = EditorPrefs.GetBool(PrefOptionsFoldout, false);
                  _maxLinesToDisplay = EditorPrefs.GetInt(PrefMaxLinesToDisplay, 1000);
                  _enableSyntaxHighlighting = EditorPrefs.GetBool(PrefEnableSyntaxHighlighting, true);
            }

            public void SavePreferences()
            {
                  EditorPrefs.SetInt(PrefFontSize, _fontSize);
                  EditorPrefs.SetInt(PrefPreviewHeight, _previewHeight);
                  EditorPrefs.SetBool(PrefShowLineNumbers, _showLineNumbers);
                  EditorPrefs.SetBool(PrefSearchFoldout, _searchFoldout);
                  EditorPrefs.SetBool(PrefOptionsFoldout, _optionsFoldout);
                  EditorPrefs.SetInt(PrefMaxLinesToDisplay, _maxLinesToDisplay);
                  EditorPrefs.SetBool(PrefEnableSyntaxHighlighting, _enableSyntaxHighlighting);
            }

            public void ResetToDefaults()
            {
                  FontSize = 11;
                  PreviewHeight = 400;
                  ShowLineNumbers = true;
                  SearchFoldout = true;
                  OptionsFoldout = false;
                  MaxLinesToDisplay = 1000;
                  EnableSyntaxHighlighting = true;
            }

            public bool ShouldUseSyntaxHighlighting(int lineCount)
            {
                  return _enableSyntaxHighlighting && lineCount <= _maxLinesToDisplay;
            }

            public bool ShouldLimitContent(int lineCount)
            {
                  return lineCount > _maxLinesToDisplay;
            }
      }
}