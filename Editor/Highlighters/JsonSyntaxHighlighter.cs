using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodePreview.Editor.Core;
using UnityEngine;

namespace CodePreview.Editor.Highlighters
{
      public class JsonSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private static readonly Dictionary<string, Regex> RegexCache = new();

            // JSON validation patterns
            private static readonly string[] ValidEscapeSequences = { "\\\"", "\\\\", "\\/", "\\b", "\\f", "\\n", "\\r", "\\t" };

            public override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                              // Property keys
                              ["propertyKey"] = isDarkTheme ? "#9CDCFE" : "#0451A5",

                              // Values
                              ["stringValue"] = isDarkTheme ? "#CE9178" : "#A31515",
                              ["numberValue"] = isDarkTheme ? "#B5CEA8" : "#098658",
                              ["booleanValue"] = isDarkTheme ? "#569CD6" : "#0000FF",
                              ["nullValue"] = isDarkTheme ? "#FF6B6B" : "#FF0000",

                              // Structure
                              ["punctuation"] = isDarkTheme ? "#D4D4D4" : "#000000",
                              ["brackets"] = isDarkTheme ? "#FFD700" : "#8B4513",
                              ["braces"] = isDarkTheme ? "#DA70D6" : "#800080",

                              // Special cases
                              ["invalidJson"] = isDarkTheme ? "#F44747" : "#FF0000",
                              ["comment"] = isDarkTheme ? "#6A9955" : "#008000", // Pour JSON avec commentaires (non-standard)
                              ["unicodeEscape"] = isDarkTheme ? "#DCDCAA" : "#795E26",

                              // Nesting levels (pour visualiser la profondeur)
                              ["level0"] = isDarkTheme ? "#FFFFFF" : "#000000",
                              ["level1"] = isDarkTheme ? "#E6E6FA" : "#191970",
                              ["level2"] = isDarkTheme ? "#FFE4E1" : "#8B0000",
                              ["level3"] = isDarkTheme ? "#F0FFF0" : "#006400",
                              ["level4"] = isDarkTheme ? "#FFF8DC" : "#FF8C00",
                              ["level5"] = isDarkTheme ? "#F5F5DC" : "#4B0082"
                  });
            }

            public override string ProcessLine(string line, bool isInMultiLineComment)
            {
                  if (string.IsNullOrEmpty(line))
                        return line;

                  string result = line;

                  // Calculer le niveau d'indentation pour la coloration par niveau
                  int nestingLevel = CalculateNestingLevel(line);
                  string levelColor = GetLevelColor(nestingLevel);

                  try
                  {
                        // 1. Traiter les commentaires non-standards (// et /* */)
                        result = ProcessComments(result);

                        // 2. Traiter les chaînes de caractères avec échappements
                        result = ProcessStrings(result);

                        // 3. Traiter les clés de propriétés
                        result = ProcessPropertyKeys(result);

                        // 4. Traiter les valeurs numériques (entiers, décimaux, scientifiques)
                        result = ProcessNumbers(result);

                        // 5. Traiter les booléens et null
                        result = ProcessBooleanAndNull(result);

                        // 6. Traiter les caractères de structure
                        result = ProcessStructuralCharacters(result);

                        // 7. Détecter les erreurs JSON communes
                        result = HighlightJsonErrors(result);

                        // 8. Appliquer la couleur de niveau si pas d'autres colorations
                        result = ApplyLevelColoring(result, levelColor);
                  }
                  catch (System.Exception e)
                  {
                        Debug.LogWarning($"JSON highlighting error on line: {line}. Error: {e.Message}");

                        // En cas d'erreur, retourner la ligne originale
                        return line;
                  }

                  return result;
            }

            public override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  var multiLineComments = new HashSet<int>();
                  bool inComment = false;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i];

                        if (!inComment && line.Contains("/*"))
                        {
                              inComment = true;
                              multiLineComments.Add(i);
                        }
                        else if (inComment)
                        {
                              multiLineComments.Add(i);

                              if (line.Contains("*/"))
                              {
                                    inComment = false;
                              }
                        }
                  }

                  return multiLineComments;
            }

            private string ProcessComments(string line)
            {
                  // Commentaires de ligne //
                  var singleLineRegex = GetOrCreateRegex(@"//.*$", RegexOptions.None);
                  line = singleLineRegex.Replace(line, match => ApplyColorTag(match.Value, Colors["comment"]));

                  // Commentaires multi-lignes /* */
                  var multiLineRegex = GetOrCreateRegex(@"/\*.*?\*/", RegexOptions.None);
                  line = multiLineRegex.Replace(line, match => ApplyColorTag(match.Value, Colors["comment"]));

                  return line;
            }

            private string ProcessStrings(string line)
            {
                  // Regex complexe pour gérer les chaînes avec échappements
                  var stringRegex = GetOrCreateRegex("\"(?:[^\"\\\\]|\\\\.)*\"", RegexOptions.None);

                  line = stringRegex.Replace(line, match =>
                  {
                        string str = match.Value;

                        // Vérifier si c'est une clé (suivie de :) ou une valeur
                        int index = line.IndexOf(str);
                        bool isKey = false;

                        if (index >= 0 && index + str.Length < line.Length)
                        {
                              string afterString = line.Substring(index + str.Length).TrimStart();
                              isKey = afterString.StartsWith(":");
                        }

                        if (isKey)
                        {
                              return str; // Les clés seront traitées par ProcessPropertyKeys
                        }
                        else
                        {
                              // Mettre en évidence les échappements Unicode
                              str = HighlightUnicodeEscapes(str);

                              return ApplyColorTag(str, Colors["stringValue"]);
                        }
                  });

                  return line;
            }

            private string HighlightUnicodeEscapes(string str)
            {
                  // Échappements Unicode \uXXXX
                  var unicodeRegex = GetOrCreateRegex(@"\\u[0-9a-fA-F]{4}", RegexOptions.None);

                  return unicodeRegex.Replace(str, match => ApplyColorTag(match.Value, Colors["unicodeEscape"]));
            }

            private string ProcessPropertyKeys(string line)
            {
                  // Clés de propriétés : "key" :
                  var keyRegex = GetOrCreateRegex("\"(?:[^\"\\\\]|\\\\.)*\"\\s*:", RegexOptions.None);

                  return keyRegex.Replace(line, match =>
                  {
                        string fullMatch = match.Value;
                        string keyPart = fullMatch.Substring(0, fullMatch.LastIndexOf(':'));
                        string colonPart = fullMatch.Substring(fullMatch.LastIndexOf(':'));

                        return ApplyColorTag(keyPart, Colors["propertyKey"]) + ApplyColorTag(colonPart, Colors["punctuation"]);
                  });
            }

            private string ProcessNumbers(string line)
            {
                  // Nombres JSON valides (entiers, décimaux, exponentiels)
                  var numberRegex = GetOrCreateRegex(@"(?<=[:\[\s,]|^)\s*-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?\s*(?=[,\]\}]|$)", RegexOptions.None);

                  return numberRegex.Replace(line, match =>
                  {
                        string number = match.Value.Trim();
                        string whitespace = match.Value.Replace(number, "");

                        return whitespace.Substring(0, whitespace.Length - whitespace.TrimStart().Length) + ApplyColorTag(number, Colors["numberValue"]) +
                               whitespace.TrimStart();
                  });
            }

            private string ProcessBooleanAndNull(string line)
            {
                  // Booléens et null en tant que valeurs (pas dans des chaînes)
                  var boolNullRegex = GetOrCreateRegex(@"(?<=[:\[\s,]|^)\s*(true|false|null)\s*(?=[,\]\}]|$)", RegexOptions.None);

                  return boolNullRegex.Replace(line, match =>
                  {
                        string value = match.Groups[1].Value;
                        string whitespace = match.Value.Replace(value, "");
                        string color = value == "null" ? Colors["nullValue"] : Colors["booleanValue"];

                        return whitespace.Substring(0, whitespace.Length - whitespace.TrimStart().Length) + ApplyColorTag(value, color) + whitespace.TrimStart();
                  });
            }

            private string ProcessStructuralCharacters(string line)
            {
                  // Traitement des caractères structurels en évitant les chaînes de caractères
                  string result = line;

                  // Crochets pour les tableaux
                  result = ProcessCharacterOutsideStrings(result, '[', Colors["brackets"]);
                  result = ProcessCharacterOutsideStrings(result, ']', Colors["brackets"]);

                  // Accolades pour les objets
                  result = ProcessCharacterOutsideStrings(result, '{', Colors["braces"]);
                  result = ProcessCharacterOutsideStrings(result, '}', Colors["braces"]);

                  // Virgules - traitement simplifié et sécurisé
                  result = ProcessCharacterOutsideStrings(result, ',', Colors["punctuation"]);

                  return result;
            }

            private string ProcessCharacterOutsideStrings(string line, char targetChar, string color)
            {
                  if (string.IsNullOrEmpty(line))
                        return line;

                  var result = new StringBuilder(line.Length * 2);
                  bool inString = false;
                  bool escaped = false;

                  for (int i = 0; i < line.Length; i++)
                  {
                        char c = line[i];

                        if (escaped)
                        {
                              escaped = false;
                              result.Append(c);

                              continue;
                        }

                        if (c == '\\' && inString)
                        {
                              escaped = true;
                              result.Append(c);

                              continue;
                        }

                        if (c == '"')
                        {
                              inString = !inString;
                              result.Append(c);

                              continue;
                        }

                        if (c == targetChar && !inString)
                        {
                              // Appliquer la couleur au caractère cible
                              result.Append(ApplyColorTag(c.ToString(), color));
                        }
                        else
                        {
                              result.Append(c);
                        }
                  }

                  return result.ToString();
            }

            private string HighlightJsonErrors(string line)
            {
                  // Erreurs courantes JSON
                  var errors = new[]
                  {
                              // Virgule en trop avant }
                              (@",\s*}", "Trailing comma before }"),

                              // Virgule en trop avant ]
                              (@",\s*]", "Trailing comma before ]"),

                              // Clé sans guillemets
                              (@"(?<=[{,]\s*)[a-zA-Z_][a-zA-Z0-9_]*\s*:", "Unquoted key"),

                              // Guillemets simples au lieu de doubles
                              (@"'[^']*'", "Single quotes instead of double quotes"),

                              // Valeurs undefined
                              (@"undefined", "Undefined value (not valid in JSON)"),

                              // Nombres avec zéros en préfixe
                              (@"(?<=[:\[\s,])-?0\d+", "Leading zeros in numbers")
                  };

                  foreach (var (pattern, _) in errors)
                  {
                        var errorRegex = GetOrCreateRegex(pattern, RegexOptions.None);
                        line = errorRegex.Replace(line, match => ApplyColorTag(match.Value, Colors["invalidJson"]));
                  }

                  return line;
            }

            private int CalculateNestingLevel(string line)
            {
                  int level = 0;
                  bool inString = false;
                  bool escaped = false;

                  foreach (char c in line)
                  {
                        if (escaped)
                        {
                              escaped = false;

                              continue;
                        }

                        if (c == '\\' && inString)
                        {
                              escaped = true;

                              continue;
                        }

                        if (c == '"')
                        {
                              inString = !inString;

                              continue;
                        }

                        if (!inString)
                        {
                              if (c == '{' || c == '[')
                                    level++;
                              else if (c == '}' || c == ']')
                                    level = System.Math.Max(0, level - 1);
                        }
                  }

                  return level;
            }

            private string GetLevelColor(int level)
            {
                  string[] levelKeys = { "level0", "level1", "level2", "level3", "level4", "level5" };
                  int index = System.Math.Min(level, levelKeys.Length - 1);

                  return Colors[levelKeys[index]];
            }

            private string ApplyLevelColoring(string line, string levelColor)
            {
                  // Applique la couleur de niveau seulement aux parties non colorées
                  // Cette fonction est simplifiée - dans un vrai cas il faudrait parser plus finement
                  return line;
            }

            private static Regex GetOrCreateRegex(string pattern, RegexOptions options)
            {
                  string key = $"{pattern}_{options}";

                  if (!RegexCache.TryGetValue(key, out var regex))
                  {
                        regex = new Regex(pattern, options);
                        RegexCache[key] = regex;
                  }

                  return regex;
            }

            // Méthodes utilitaires pour la validation JSON
            public static bool IsValidJsonLine(string line)
            {
                  if (string.IsNullOrWhiteSpace(line))
                        return true;

                  try
                  {
                        // Validation basique - peut être étendue
                        var trimmed = line.Trim();

                        return !trimmed.Contains("undefined") && !trimmed.EndsWith(",}") && !trimmed.EndsWith(",]");
                  }
                  catch
                  {
                        return false;
                  }
            }

            public static string[] GetJsonValidationErrors(string[] lines)
            {
                  var errors = new List<string>();
                  int braceCount = 0;
                  int bracketCount = 0;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i];
                        bool inString = false;
                        bool escaped = false;

                        foreach (char c in line)
                        {
                              if (escaped)
                              {
                                    escaped = false;

                                    continue;
                              }

                              if (c == '\\' && inString)
                              {
                                    escaped = true;

                                    continue;
                              }

                              if (c == '"')
                              {
                                    inString = !inString;

                                    continue;
                              }

                              if (!inString)
                              {
                                    switch (c)
                                    {
                                          case '{':
                                                braceCount++;

                                                break;
                                          case '}':
                                                braceCount--;

                                                if (braceCount < 0)
                                                      errors.Add($"Line {i + 1}: Unmatched closing brace");

                                                break;
                                          case '[':
                                                bracketCount++;

                                                break;
                                          case ']':
                                                bracketCount--;

                                                if (bracketCount < 0)
                                                      errors.Add($"Line {i + 1}: Unmatched closing bracket");

                                                break;
                                    }
                              }
                        }

                        if (!IsValidJsonLine(line))
                        {
                              errors.Add($"Line {i + 1}: Invalid JSON syntax");
                        }
                  }

                  if (braceCount != 0)
                        errors.Add($"Unmatched braces: {braceCount}");

                  if (bracketCount != 0)
                        errors.Add($"Unmatched brackets: {bracketCount}");

                  return errors.ToArray();
            }
      }
}