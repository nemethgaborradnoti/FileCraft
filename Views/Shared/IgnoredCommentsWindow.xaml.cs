using FileCraft.Models;
using FileCraft.Shared.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class IgnoredCommentsWindow : Window, INotifyPropertyChanged
    {
        private readonly List<IgnoredFileViewModel> _allFiles = new();
        public ObservableCollection<IgnoredFileViewModel> FilteredFiles { get; } = new();
        private readonly Timer _debounceTimer;

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
                    _debounceTimer.Change(300, Timeout.Infinite);
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

        private bool _isUpdatingSelection;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IgnoredCommentsWindow(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles)
        {
            InitializeComponent();
            DataContext = this;
            Owner = System.Windows.Application.Current.MainWindow;
            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _totalCountsDisplay = $"{ResourceHelper.GetString("IgnoredComments_TotalLabel")} 0";

            var ignoredSet = new HashSet<string>(previouslyIgnoredFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var file in selectedFiles.OrderBy(f => f.RelativePath))
            {
                bool isIgnored = ignoredSet.Contains(file.RelativePath);
                var vm = new IgnoredFileViewModel(file.RelativePath, file.FullPath, isIgnored);
                vm.PropertyChanged += OnFilePropertyChanged;
                _allFiles.Add(vm);
            }

            ApplyFilter();
            UpdateTotalCount();
        }

        private void OnDebounceTimerElapsed(object? state)
        {
            Application.Current.Dispatcher.Invoke(ApplyFilter);
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

        private void OnFilePropertyChanged(object? sender, PropertyChangedEventArgs e)
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

        private async void CountCommentsButton_Click(object sender, RoutedEventArgs e)
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

            UpdateTotalCount();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class IgnoredFileViewModel : INotifyPropertyChanged
    {
        public string RelativePath { get; }
        public string FullPath { get; }
        public int CommentCount { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _commentCountDisplay = "-";
        public string CommentCountDisplay
        {
            get => _commentCountDisplay;
            set
            {
                if (_commentCountDisplay != value)
                {
                    _commentCountDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        public IgnoredFileViewModel(string relativePath, string fullPath, bool isSelected)
        {
            RelativePath = relativePath;
            FullPath = fullPath;
            IsSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}