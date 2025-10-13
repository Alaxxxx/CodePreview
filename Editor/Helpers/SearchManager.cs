using System;
using System.Collections.Generic;
using System.Linq;

namespace OpalStudio.CodePreview.Editor.Helpers
{
      public sealed class SearchManager
      {
            // Events
            public event Action OnSearchResultsChanged;

            // Search state
            private string _searchQuery = "";
            private string _lastProcessedSearchQuery = "";
            private bool _caseSensitiveSearch;
            private readonly HashSet<int> _searchResults = new();
            private int _currentSearchIndex = -1;

            // Navigation
            private int _goToLine = 1;

            // Properties
            public string SearchQuery
            {
                  get => _searchQuery;
                  set
                  {
                        if (_searchQuery != null && _searchQuery != value)
                        {
                              _searchQuery = value;
                        }
                  }
            }

            public bool CaseSensitiveSearch
            {
                  get => _caseSensitiveSearch;
                  set
                  {
                        if (_caseSensitiveSearch == value)
                        {
                              return;
                        }

                        _caseSensitiveSearch = value;

                        if (!string.IsNullOrEmpty(_searchQuery))
                        {
                              _lastProcessedSearchQuery = "";
                        }
                  }
            }

            public int GoToLine
            {
                  get => _goToLine;
                  set => _goToLine = Math.Max(1, value);
            }

            public int CurrentSearchIndex => _currentSearchIndex;
            public int SearchResultsCount => _searchResults.Count;
            public HashSet<int> SearchResults => _searchResults;
            public bool HasSearchResults => _searchResults.Count > 0;
            public bool HasSearchQuery => !string.IsNullOrEmpty(_searchQuery);

            public bool HasSearchQueryChanged()
            {
                  return _searchQuery != _lastProcessedSearchQuery;
            }

            public string GetSearchQuery() => _searchQuery;

            public HashSet<int> GetSearchResults() => _searchResults;

            public void PerformSearch(string[] lines)
            {
                  if (lines == null)
                  {
                        ClearSearch();

                        return;
                  }

                  _searchResults.Clear();
                  _currentSearchIndex = -1;

                  if (string.IsNullOrEmpty(_searchQuery))
                  {
                        _lastProcessedSearchQuery = _searchQuery;
                        OnSearchResultsChanged?.Invoke();

                        return;
                  }

                  StringComparison comparison = _caseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        if (lines[i] != null && lines[i].IndexOf(_searchQuery, comparison) >= 0)
                        {
                              _searchResults.Add(i);
                        }
                  }

                  if (_searchResults.Count > 0)
                  {
                        _currentSearchIndex = 0;
                  }

                  _lastProcessedSearchQuery = _searchQuery;
                  OnSearchResultsChanged?.Invoke();
            }

            public void ClearSearch()
            {
                  _searchQuery = "";
                  _lastProcessedSearchQuery = "";
                  _searchResults.Clear();
                  _currentSearchIndex = -1;
                  OnSearchResultsChanged?.Invoke();
            }

            public int GetCurrentResultLine()
            {
                  if (_currentSearchIndex >= 0 && _currentSearchIndex < _searchResults.Count)
                  {
                        return _searchResults.ToArray()[_currentSearchIndex];
                  }

                  return -1;
            }

            public bool GoToNextResult()
            {
                  if (_searchResults.Count == 0)
                  {
                        return false;
                  }

                  _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
                  OnSearchResultsChanged?.Invoke();

                  return true;
            }

            public bool GoToPreviousResult()
            {
                  if (_searchResults.Count == 0)
                  {
                        return false;
                  }

                  _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) % _searchResults.Count;
                  OnSearchResultsChanged?.Invoke();

                  return true;
            }

            public string GetSearchStatusText()
            {
                  if (!HasSearchQuery)
                  {
                        return "";
                  }

                  if (!HasSearchResults)
                  {
                        return "(0/0)";
                  }

                  return $"({_currentSearchIndex + 1}/{_searchResults.Count})";
            }

            public bool IsCurrentResult(int lineIndex)
            {
                  return _currentSearchIndex >= 0 && _currentSearchIndex < _searchResults.Count && _searchResults.ToArray()[_currentSearchIndex] == lineIndex;
            }

            public int GetGoToLineZeroBased() => Math.Max(0, _goToLine - 1);

            public void SetGoToLine(int lineNumber) => _goToLine = Math.Max(1, lineNumber);
      }
}