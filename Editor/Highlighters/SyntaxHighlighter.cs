using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Settings;
using UnityEditor;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class SyntaxHighlighter
      {
            private readonly Dictionary<ScriptType, BaseSyntaxHighlighter> _highlighters = new();
            private string _processedContent = "";
            private string[] _currentLines;
            private ScriptType _currentScriptType;
            private HashSet<int> _searchResults = new();
            private string _searchQuery = "";

            internal SyntaxHighlighter()
            {
                  _highlighters[ScriptType.CSharp] = new CSharpSyntaxHighlighter();
                  _highlighters[ScriptType.Json] = new JsonSyntaxHighlighter();
                  _highlighters[ScriptType.XML] = new XmlSyntaxHighlighter();
                  _highlighters[ScriptType.Readme] = new ReadmeSyntaxHighlighter();
                  _highlighters[ScriptType.Yaml] = new YamlSyntaxHighlighter();

                  // TODO: Add other highlighters
            }

            internal void ProcessContent(string[] lines, ScriptType scriptType, PreviewSettings settings)
            {
                  if (lines == null || lines.Length == 0)
                  {
                        _processedContent = "";

                        return;
                  }

                  _currentLines = lines;
                  _currentScriptType = scriptType;

                  if (!settings.ShouldUseSyntaxHighlighting(lines.Length))
                  {
                        var sb = new StringBuilder();

                        for (int i = 0; i < lines.Length; i++)
                        {
                              string processedLine = ProcessLineNumbers(lines[i], i, settings.ShowLineNumbers, lines.Length);
                              sb.AppendLine(processedLine);
                        }

                        _processedContent = sb.ToString();

                        return;
                  }

                  if (!_highlighters.TryGetValue(scriptType, out BaseSyntaxHighlighter highlighter))
                  {
                        ProcessAsPlainText(lines, settings);

                        return;
                  }

                  highlighter.Initialize(settings.IsDarkTheme);
                  HashSet<int> multiLineComments = highlighter.GetMultiLineCommentLines(lines);

                  var contentBuilder = new StringBuilder(lines.Length * 50);

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i].TrimEnd('\r');
                        bool isInMultiLineComment = multiLineComments.Contains(i);
                        string processedLine = highlighter.ProcessLine(line, isInMultiLineComment);

                        processedLine = ApplySearchHighlighting(processedLine, i);
                        processedLine = ProcessLineNumbers(processedLine, i, settings.ShowLineNumbers, lines.Length);

                        contentBuilder.AppendLine(processedLine);
                  }

                  _processedContent = contentBuilder.ToString();
            }

            private void ProcessAsPlainText(string[] lines, PreviewSettings settings)
            {
                  var sb = new StringBuilder();

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string processedLine = ApplySearchHighlighting(lines[i], i);
                        processedLine = ProcessLineNumbers(processedLine, i, settings.ShowLineNumbers, lines.Length);
                        sb.AppendLine(processedLine);
                  }

                  _processedContent = sb.ToString();
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

            private string ApplySearchHighlighting(string line, int lineIndex)
            {
                  if (string.IsNullOrEmpty(_searchQuery) || !_searchResults.Contains(lineIndex))
                  {
                        return line;
                  }

                  bool isDark = EditorGUIUtility.isProSkin;
                  string highlightColor = isDark ? "#FFEB3B" : "#FFD700";
                  const string textColor = "#000000";

                  string escapedTerm = Regex.Escape(_searchQuery);

                  return Regex.Replace(line, escapedTerm, $"<mark={highlightColor}><color={textColor}><b>$0</b></color></mark>", RegexOptions.IgnoreCase);
            }

            internal void UpdateSearchHighlighting(string searchQuery, HashSet<int> searchResults)
            {
                  _searchQuery = searchQuery;
                  _searchResults = searchResults;

                  if (_currentLines != null)
                  {
                        var settings = new PreviewSettings();
                        ProcessContent(_currentLines, _currentScriptType, settings);
                  }
            }

            internal string GetProcessedContent() => _processedContent;

            internal void SetErrorContent(string errorMessage)
            {
                  _processedContent = $"<color=red>{errorMessage}</color>";
            }
      }
}