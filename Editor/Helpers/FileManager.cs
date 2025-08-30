using System;
using System.IO;
using UnityEditor;
using FileInfo = CodePreview.Editor.Data.FileInfo;

namespace CodePreview.Editor.Helpers
{
      public sealed class FileManager
      {
            private string _filePath;
            private FileInfo _fileInfo;
            private string[] _originalLines;
            private string[] _displayLines;
            private bool _isLimited;

            public bool CheckForChanges(MonoScript script)
            {
                  string path = AssetDatabase.GetAssetPath(script);

                  if (string.IsNullOrEmpty(path) || !File.Exists(path))
                  {
                        return false;
                  }

                  DateTime lastWrite = File.GetLastWriteTime(path);

                  if (_filePath == path && lastWrite == _fileInfo?.LastModifiedTime)
                  {
                        return false;
                  }

                  return true;
            }

            public void LoadScript(MonoScript script)
            {
                  _filePath = AssetDatabase.GetAssetPath(script);

                  if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                  {
                        throw new FileNotFoundException($"Script file not found: {_filePath}");
                  }

                  try
                  {
                        string rawContent = File.ReadAllText(_filePath);
                        _originalLines = rawContent.Split('\n');
                        _displayLines = _originalLines;
                        _isLimited = false;

                        _fileInfo = CalculateFileInfo(_originalLines);
                        _fileInfo.LastModifiedTime = File.GetLastWriteTime(_filePath);
                        _fileInfo.fileSize = new System.IO.FileInfo(_filePath).Length;
                  }
                  catch (Exception e)
                  {
                        throw new Exception($"Error reading script: {e.Message}", e);
                  }
            }

            public void LoadFromContent(string[] lines, string filePath)
            {
                  _filePath = filePath;
                  _originalLines = lines;
                  _displayLines = lines;
                  _isLimited = false;

                  _fileInfo = CalculateFileInfo(_originalLines);

                  if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
                  {
                        _fileInfo.LastModifiedTime = File.GetLastWriteTime(_filePath);
                        _fileInfo.fileSize = new System.IO.FileInfo(_filePath).Length;
                  }
                  else
                  {
                        _fileInfo.LastModifiedTime = DateTime.Now;
                        _fileInfo.fileSize = string.Join("\n", lines).Length;
                  }
            }

            public void SetLimitedLines(string[] limitedLines)
            {
                  _displayLines = limitedLines;
                  _isLimited = true;
            }

            public string[] GetLines() => _originalLines;

            public string[] GetDisplayLines() => _displayLines;

            public string GetFilePath() => _filePath;

            public FileInfo GetFileInfo() => _fileInfo;

            public bool HasContent() => _originalLines is { Length: > 0 };

            public bool IsLimited() => _isLimited;

            private static FileInfo CalculateFileInfo(string[] lines)
            {
                  var info = new FileInfo
                  {
                              totalLines = lines.Length
                  };

                  foreach (string line in lines)
                  {
                        info.totalChars += line.Length;
                        info.totalWords += line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                        string trimmed = line.TrimStart();

                        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*") || trimmed.Contains("*/"))
                        {
                              info.commentLines++;
                        }
                  }

                  return info;
            }

            public bool HasChangedSince(DateTime lastCheck)
            {
                  if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                  {
                        return false;
                  }

                  return File.GetLastWriteTime(_filePath) > lastCheck;
            }

            public bool ShouldLimitProcessing(int maxLines)
            {
                  return _originalLines != null && _originalLines.Length > maxLines;
            }
      }
}