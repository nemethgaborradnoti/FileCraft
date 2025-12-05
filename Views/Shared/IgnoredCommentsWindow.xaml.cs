using FileCraft.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Application = System.Windows.Application;
namespace FileCraft.Views.Shared
{
    public partial class IgnoredCommentsWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<IgnoredFileViewModel> Files { get; } = new();

        private bool? _areAllSelected;
        public bool? AreAllSelected
        {
            get => _areAllSelected;
            set
            {
                if (_areAllSelected != value)
                {
                    _areAllSelected = value;
                    OnPropertyChanged();
                    if (_areAllSelected.HasValue)
                    {
                        UpdateAllSelection(_areAllSelected.Value);
                    }
                }
            }
        }

        private string _totalCountsDisplay = "Total: 0";
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

            var ignoredSet = new HashSet<string>(previouslyIgnoredFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var file in selectedFiles.OrderBy(f => f.RelativePath))
            {
                bool isIgnored = ignoredSet.Contains(file.RelativePath);
                var vm = new IgnoredFileViewModel(file.RelativePath, file.FullPath, isIgnored);
                vm.PropertyChanged += OnFilePropertyChanged;
                Files.Add(vm);
            }

            UpdateMasterSelection();
            UpdateTotalCount();
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
            foreach (var file in Files)
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

            _isUpdatingSelection = true;
            if (Files.All(f => f.IsSelected))
            {
                AreAllSelected = true;
            }
            else if (Files.All(f => !f.IsSelected))
            {
                AreAllSelected = false;
            }
            else
            {
                AreAllSelected = null;
            }
            _isUpdatingSelection = false;
        }

        private void UpdateTotalCount()
        {
            long total = Files.Where(f => f.IsSelected).Sum(f => (long)f.CommentCount);
            var nfi = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };
            TotalCountsDisplay = $"Total: {total.ToString("N0", nfi)}";
        }

        public IEnumerable<string> GetIgnoredFilePaths()
        {
            return Files.Where(f => f.IsSelected).Select(f => f.RelativePath);
        }

        private async void CountCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var file in Files)
            {
                file.CommentCountDisplay = "...";
                file.CommentCount = 0;
            }
            UpdateTotalCount();

            await Task.Run(() =>
            {
                foreach (var file in Files)
                {
                    try
                    {
                        if (File.Exists(file.FullPath))
                        {
                            var lines = File.ReadAllLines(file.FullPath);
                            int totalChars = 0;
                            foreach (var line in lines)
                            {
                                int index = line.IndexOf("///");
                                if (index >= 0)
                                {
                                    int charsInThisLine = line.Length - (index + 3);
                                    if (charsInThisLine > 0)
                                    {
                                        totalChars += charsInThisLine;
                                    }
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
                            Application.Current.Dispatcher.Invoke(() => file.CommentCountDisplay = "N/A");
                        }
                    }
                    catch
                    {
                        Application.Current.Dispatcher.Invoke(() => file.CommentCountDisplay = "Err");
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