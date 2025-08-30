using System.Collections.Generic;

namespace OpalStudio.CodePreview.Editor.Core
{
      public abstract class BaseSyntaxHighlighter
      {
            protected Dictionary<string, string> Colors { get; private set; }

            public abstract void Initialize(bool isDarkTheme);

            public abstract string ProcessLine(string line, bool isInMultiLineComment);

            public abstract HashSet<int> GetMultiLineCommentLines(string[] lines);

            protected void SetColors(Dictionary<string, string> colors)
            {
                  Colors = colors;
            }

            protected static string ApplyColorTag(string text, string color)
            {
                  return $"<color={color}>{text}</color>";
            }
      }
}