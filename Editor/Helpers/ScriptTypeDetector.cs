using System.Collections.Generic;
using System.IO;
using OpalStudio.CodePreview.Editor.Data;

namespace OpalStudio.CodePreview.Editor.Helpers
{
      public static class ScriptTypeDetector
      {
            private readonly static Dictionary<string, ScriptType> ExtensionMap = new()
            {
                        { ".cs", ScriptType.CSharp },
                        { ".shader", ScriptType.Shader },
                        { ".hlsl", ScriptType.Shader },
                        { ".cginc", ScriptType.Shader },
                        { ".json", ScriptType.Json },
                        { ".xml", ScriptType.XML },
                        { ".js", ScriptType.JavaScript }
            };

            public static ScriptType DetectType(string filePath)
            {
                  if (string.IsNullOrEmpty(filePath))
                  {
                        return ScriptType.Unknown;
                  }

                  string extension = Path.GetExtension(filePath).ToLower();

                  return ExtensionMap.GetValueOrDefault(extension, ScriptType.Unknown);
            }
      }
}