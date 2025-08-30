using System;

namespace CodePreview.Editor.Data
{
      [Serializable]
      public class FileInfo
      {
            public DateTime LastModifiedTime;
            public long fileSize;
            public int totalLines;
            public int totalWords;
            public int totalChars;
            public int commentLines;

            public string FormattedSize => FormatFileSize(fileSize);

            private static string FormatFileSize(long bytes)
            {
                  string[] sizes = { "B", "KB", "MB", "GB" };
                  double len = bytes;
                  int order = 0;

                  while (len >= 1024 && order < sizes.Length - 1)
                  {
                        order++;
                        len /= 1024;
                  }

                  return $"{len:0.##} {sizes[order]}";
            }
      }
}