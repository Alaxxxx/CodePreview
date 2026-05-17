using System;
using System.Collections.Generic;
using System.Text;
using OpalStudio.CodePreview.Editor.Core;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Settings;
using UnityEditor;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      internal sealed class SyntaxHighlighter
      {
            private const char SearchMarkerStart = '\uE000';
            private const char SearchMarkerEnd = '\uE001';

            private readonly Dictionary<ScriptType, BaseSyntaxHighlighter> _highlighters = new();

            private string[] _processedLines = Array.Empty<string>();

            private string[] _currentLines;
            private ScriptType _currentScriptType;
            private HashSet<int> _searchResults = new();
            private string _searchQuery = "";
            private bool _caseSensitiveSearch;

            internal SyntaxHighlighter()
            {
                  _highlighters[ScriptType.CSharp] = new CSharpSyntaxHighlighter();
                  _highlighters[ScriptType.Json] = new JsonSyntaxHighlighter();
                  _highlighters[ScriptType.XML] = new XmlSyntaxHighlighter();
                  _highlighters[ScriptType.Readme] = new ReadmeSyntaxHighlighter();
                  _highlighters[ScriptType.Yaml] = new YamlSyntaxHighlighter();
            }

            internal void ProcessContent(string[] lines, ScriptType scriptType, PreviewSettings settings)
            {
                  if (lines == null || lines.Length == 0)
                  {
                        _processedLines = Array.Empty<string>();

                        return;
                  }

                  _currentLines = lines;
                  _currentScriptType = scriptType;

                  if (!settings.ShouldUseSyntaxHighlighting(lines.Length))
                  {
                        _processedLines = BuildPlainLines(lines, settings);

                        return;
                  }

                  if (!_highlighters.TryGetValue(scriptType, out BaseSyntaxHighlighter highlighter))
                  {
                        _processedLines = BuildPlainLines(lines, settings);

                        return;
                  }

                  highlighter.Initialize(settings.IsDarkTheme);
                  HashSet<int> multiLineComments = highlighter.GetMultiLineCommentLines(lines);

                  _processedLines = new string[lines.Length];

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i].TrimEnd('\r');

                        string lineWithMarkers = InjectSearchMarkers(line, i);

                        bool isInMultiLine = multiLineComments.Contains(i);
                        string processedLine = highlighter.ProcessLine(lineWithMarkers, isInMultiLine);

                        processedLine = ConvertMarkersToHighlight(processedLine);

                        processedLine = ProcessLineNumbers(processedLine, i, settings.ShowLineNumbers, lines.Length);

                        _processedLines[i] = processedLine;
                  }
            }

            private static string[] BuildPlainLines(string[] lines, PreviewSettings settings)
            {
                  string[] result = new string[lines.Length];

                  for (int i = 0; i < lines.Length; i++)
                  {
                        result[i] = ProcessLineNumbers(lines[i], i, settings.ShowLineNumbers, lines.Length);
                  }

                  return result;
            }

            private static string ProcessLineNumbers(string line, int lineIndex, bool showLineNumbers, int totalLines)
            {
                  if (!showLineNumbers)
                  {
                        return line;
                  }

                  string lineNumber = (lineIndex + 1).ToString().PadLeft(totalLines.ToString().Length);

                  return $"<color=#808080>{lineNumber}</color>  {line}";
            }

            private string InjectSearchMarkers(string line, int lineIndex)
            {
                  if (string.IsNullOrEmpty(_searchQuery) || !_searchResults.Contains(lineIndex) || string.IsNullOrEmpty(line))
                  {
                        return line;
                  }

                  StringComparison comparison = _caseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                  var sb = new StringBuilder(line.Length + 16);
                  int searchLen = _searchQuery.Length;
                  int pos = 0;

                  while (pos < line.Length)
                  {
                        int matchIdx = line.IndexOf(_searchQuery, pos, comparison);

                        if (matchIdx < 0)
                        {
                              sb.Append(line, pos, line.Length - pos);

                              break;
                        }

                        if (matchIdx > pos)
                        {
                              sb.Append(line, pos, matchIdx - pos);
                        }

                        sb.Append(SearchMarkerStart);
                        sb.Append(line, matchIdx, searchLen);
                        sb.Append(SearchMarkerEnd);

                        pos = matchIdx + searchLen;
                  }

                  return sb.ToString();
            }

            private static string ConvertMarkersToHighlight(string line)
            {
                  if (string.IsNullOrEmpty(line))
                  {
                        return line;
                  }

                  if (line.IndexOf(SearchMarkerStart) < 0)
                  {
                        return line;
                  }

                  bool isDark = EditorGUIUtility.isProSkin;
                  string highlightColor = isDark ? "#FFEB3B" : "#FFD700";
                  const string textColor = "#000000";
                  string openTag = $"<mark={highlightColor}><color={textColor}><b>";
                  const string closeTag = "</b></color></mark>";

                  var sb = new StringBuilder(line.Length + 64);

                  foreach (char c in line)
                  {
                        if (c == SearchMarkerStart)
                        {
                              sb.Append(openTag);
                        }
                        else if (c == SearchMarkerEnd)
                        {
                              sb.Append(closeTag);
                        }
                        else
                        {
                              sb.Append(c);
                        }
                  }

                  return sb.ToString();
            }

            internal void UpdateSearchHighlighting(string searchQuery, HashSet<int> searchResults, bool caseSensitive = false)
            {
                  _searchQuery = searchQuery;
                  _searchResults = searchResults;
                  _caseSensitiveSearch = caseSensitive;

                  if (_currentLines != null)
                  {
                        var settings = new PreviewSettings();
                        settings.LoadPreferences();
                        ProcessContent(_currentLines, _currentScriptType, settings);
                  }
            }

            internal string[] GetProcessedLines() => _processedLines;

            internal void SetErrorContent(string errorMessage)
            {
                  _processedLines = new[] { $"<color=red>{errorMessage}</color>" };
            }
      }
}