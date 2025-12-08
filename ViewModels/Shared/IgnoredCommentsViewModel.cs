using FileCraft.Models;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class IgnoredCommentsViewModel : BaseViewModel
    {
        private readonly List<IgnoredFileViewModel> _allFiles = new();
        public ObservableCollection<IgnoredFileViewModel> FilteredFiles { get; } = new();
        private readonly Debouncer _debouncer;
        private bool _isUpdatingSelection;

        private string _searchFilter = string.Empty;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value;
                    OnPropertyChanged();
                    _debouncer.Debounce();
                }
            }
        }

        private bool? _areAllSelected;
        public bool? AreAllSelected
        {
            get => _areAllSelected;
            set
            {
                bool selectAll = _areAllSelected != true;
                UpdateAllSelection(selectAll);
            }
        }

        private string _totalCountsDisplay = "";
        public string TotalCountsDisplay
        {
            get => _totalCountsDisplay;
            set
            {
                if (_totalCountsDisplay != value)
                {
                    _totalCountsDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public event Action<bool?>? RequestClose;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public IgnoredCommentsViewModel(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles)
        {
            _debouncer = new Debouncer(() => Application.Current.Dispatcher.Invoke(ApplyFilter));
            _totalCountsDisplay = $"{ResourceHelper.GetString("IgnoredComments_TotalLabel")} 0";

            var ignoredSet = new HashSet<string>(previouslyIgnoredFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var file in selectedFiles.OrderBy(f => f.RelativePath))
            {
                bool isIgnored = ignoredSet.Contains(file.RelativePath);
                var vm = new IgnoredFileViewModel(file.RelativePath, file.FullPath, isIgnored);
                vm.PropertyChanged += OnFilePropertyChanged;
                _allFiles.Add(vm);
            }

            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            ApplyFilter();
            UpdateTotalCount();

            _ = CountComments();
        }

        private void ApplyFilter()
        {
            FilteredFiles.Clear();
            foreach (var file in _allFiles)
            {
                if (string.IsNullOrWhiteSpace(SearchFilter) || file.RelativePath.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    FilteredFiles.Add(file);
                }
            }
            UpdateMasterSelection();
        }

        private void OnFilePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IgnoredFileViewModel.IsSelected))
            {
                UpdateMasterSelection();
                UpdateTotalCount();
            }
        }

        private void UpdateAllSelection(bool isSelected)
        {
            _isUpdatingSelection = true;
            foreach (var file in FilteredFiles)
            {
                file.IsSelected = isSelected;
            }
            _isUpdatingSelection = false;
            UpdateMasterSelection();
            UpdateTotalCount();
        }

        private void UpdateMasterSelection()
        {
            if (_isUpdatingSelection) return;

            bool? newState = null;

            if (FilteredFiles.Count == 0)
            {
                newState = false;
            }
            else if (FilteredFiles.All(f => f.IsSelected))
            {
                newState = true;
            }
            else if (FilteredFiles.All(f => !f.IsSelected))
            {
                newState = false;
            }

            if (_areAllSelected != newState)
            {
                _areAllSelected = newState;
                OnPropertyChanged(nameof(AreAllSelected));
            }
        }

        private void UpdateTotalCount()
        {
            long total = _allFiles.Where(f => f.IsSelected).Sum(f => (long)f.CommentCount);
            var nfi = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
            TotalCountsDisplay = $"{ResourceHelper.GetString("IgnoredComments_TotalLabel")} {total.ToString("N0", nfi)}";
        }

        public IEnumerable<string> GetIgnoredFilePaths()
        {
            return _allFiles.Where(f => f.IsSelected).Select(f => f.RelativePath);
        }

        private async Task CountComments()
        {
            foreach (var file in _allFiles)
            {
                file.CommentCountDisplay = "...";
                file.CommentCount = 0;
            }
            UpdateTotalCount();

            await Task.Run(() =>
            {
                foreach (var file in _allFiles)
                {
                    try
                    {
                        if (File.Exists(file.FullPath))
                        {
                            var lines = File.ReadAllLines(file.FullPath);
                            int totalChars = 0;
                            foreach (var line in lines)
                            {
                                var stats = IgnoreCommentsHelper.CalculateXmlCommentStats(line);
                                if (stats.IsXmlComment)
                                {
                                    totalChars += stats.CommentLength;
                                }
                            }
                            int finalCount = totalChars;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                file.CommentCount = finalCount;
                                file.CommentCountDisplay = finalCount.ToString();
                            });
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() => file.CommentCountDisplay = ResourceHelper.GetString("IgnoredComments_StatusNA"));
                        }
                    }
                    catch
                    {
                        Application.Current.Dispatcher.Invoke(() => file.CommentCountDisplay = ResourceHelper.GetString("IgnoredComments_StatusErr"));
                    }
                }
            });

            // Sort by comment count descending
            var sortedFiles = _allFiles.OrderByDescending(f => f.CommentCount).ToList();
            _allFiles.Clear();
            _allFiles.AddRange(sortedFiles);

            // Refresh filtered list after sort
            Application.Current.Dispatcher.Invoke(() => ApplyFilter());

            UpdateTotalCount();
        }

        private void Save()
        {
            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }
    }
}