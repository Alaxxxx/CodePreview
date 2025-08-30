using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      public sealed class CSharpSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private readonly static Dictionary<string, Regex> RegexCache = new();

            private readonly static string[] Keywords =
            {
                        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
                        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
                        "void", "volatile", "while", "var", "async", "await", "dynamic", "yield", "where",
                        "select", "from", "group", "into", "orderby", "join", "let", "ascending", "descending",
                        "on", "equals", "by", "value", "global", "partial", "when", "add", "remove", "get", "set", "init"
            };

            private readonly static string[] UnityTypes =
            {
                        "MonoBehaviour", "Transform", "GameObject", "Component", "Rigidbody", "Rigidbody2D",
                        "Collider", "Collider2D", "Renderer", "Camera", "Light", "AudioSource", "Animation",
                        "Animator", "Canvas", "RectTransform", "Image", "Text", "Button", "Vector2", "Vector3",
                        "Vector4", "Quaternion", "Color", "Color32", "Mathf", "Time", "Input", "Physics",
                        "Physics2D", "Random", "Debug", "ScriptableObject", "SerializeField", "Range", "Header",
                        "Space", "Tooltip", "System.Serializable", "TextMesh", "Mesh", "Material", "Texture",
                        "Texture2D", "Sprite", "AudioClip", "AnimationClip", "RuntimeAnimatorController", "ParticleSystem"
            };

            public override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                              ["keyword"] = isDarkTheme ? "#569CD6" : "#0000FF",
                              ["comment"] = isDarkTheme ? "#6A9955" : "#008000",
                              ["string"] = isDarkTheme ? "#CE9178" : "#A31515",
                              ["unityType"] = isDarkTheme ? "#4EC9B0" : "#2B91AF",
                              ["number"] = isDarkTheme ? "#B5CEA8" : "#098658",
                              ["preprocessor"] = isDarkTheme ? "#C586C0" : "#9B59B6",
                              ["attribute"] = isDarkTheme ? "#FFD700" : "#FF8C00",
                              ["method"] = isDarkTheme ? "#DCDCAA" : "#795E26",
                              ["customType"] = isDarkTheme ? "#4EC9B0" : "#2B91AF"
                  });
            }

            public override string ProcessLine(string line, bool isInMultiLineComment)
            {
                  if (string.IsNullOrEmpty(line))
                  {
                        return line;
                  }

                  if (isInMultiLineComment)
                  {
                        return ApplyColorTag(line, this.Colors["comment"]);
                  }

                  if (line.TrimStart().StartsWith("//", StringComparison.OrdinalIgnoreCase))
                  {
                        return ApplyColorTag(line, this.Colors["comment"]);
                  }

                  string result = line;

                  result = ApplyPreprocessorHighlighting(result);
                  result = ApplyAttributeHighlighting(result);
                  result = ApplyStringHighlighting(result);
                  result = ApplyCommentHighlighting(result);
                  result = ApplyNumberHighlighting(result);
                  result = ApplyKeywordHighlighting(result);
                  result = ApplyUnityTypeHighlighting(result);
                  result = ApplyMethodHighlighting(result);
                  result = ApplyCustomTypeHighlighting(result);
                  result = ApplyUsingHighlighting(result);

                  return result;
            }

            public override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  var multiLineComments = new HashSet<int>();
                  string fullText = string.Join("\n", lines);

                  Regex regex = GetOrCreateRegex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);

                  foreach (Match match in regex.Matches(fullText))
                  {
                        int start = fullText[..match.Index].Count(static c => c == '\n');
                        int end = fullText[..(match.Index + match.Length)].Count(static c => c == '\n');

                        for (int i = start; i <= end; i++)
                        {
                              multiLineComments.Add(i);
                        }
                  }

                  return multiLineComments;
            }

            private static Regex GetOrCreateRegex(string pattern, RegexOptions options)
            {
                  string key = $"{pattern}_{options}";

                  if (!RegexCache.TryGetValue(key, out Regex regex))
                  {
                        regex = new Regex(pattern, options);
                        RegexCache[key] = regex;
                  }

                  return regex;
            }

            private string ApplyPreprocessorHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^\s*#\w+.*$", RegexOptions.Multiline);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["preprocessor"]));
            }

            private string ApplyAttributeHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\[[\w\.\(\),\s=""]+\]", RegexOptions.None);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["attribute"]));
            }

            private string ApplyStringHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"", RegexOptions.None);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["string"]));
            }

            private string ApplyCommentHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"//.*$", RegexOptions.Multiline);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["comment"]));
            }

            private string ApplyNumberHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\b\d+\.?\d*[fFdDmMlLuU]?\b", RegexOptions.None);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["number"]));
            }

            private string ApplyKeywordHighlighting(string result)
            {
                  string pattern = @"\b(" + string.Join("|", Keywords) + @")\b";
                  Regex regex = GetOrCreateRegex(pattern, RegexOptions.None);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["keyword"]));
            }

            private string ApplyUnityTypeHighlighting(string result)
            {
                  string pattern = @"\b(" + string.Join("|", UnityTypes.Select(Regex.Escape)) + @")\b";
                  Regex regex = GetOrCreateRegex(pattern, RegexOptions.None);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["unityType"]));
            }

            private string ApplyMethodHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\b([A-Z][a-zA-Z0-9]*)\s*(?=\()", RegexOptions.None);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["method"]));
            }

            private string ApplyCustomTypeHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\b([A-Z][a-zA-Z0-9]*)\b(?![\s\S]*?</color>)(?!\s*\()", RegexOptions.None);

                  return regex.Replace(result, match =>
                  {
                        string word = match.Value;

                        if (!UnityTypes.Contains(word) && !Keywords.Contains(word.ToLower()))
                        {
                              return ApplyColorTag(word, this.Colors["customType"]);
                        }

                        return word;
                  });
            }

            private string ApplyUsingHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"(using)\s+([\w\.]+);", RegexOptions.None);

                  return regex.Replace(result, match => $"{ApplyColorTag("using", this.Colors["keyword"])} {ApplyColorTag(match.Groups[2].Value, this.Colors["customType"])};");
            }
      }
}