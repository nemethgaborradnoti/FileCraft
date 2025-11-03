using FileCraft.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Application = System.Windows.Application;

namespace FileCraft.Views.Shared
{
    public partial class BulkSearchWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dictionary<string, SelectableFile> _allAvailableFilesLookup;
        public ObservableCollection<FoundFileViewModel> FilteredFoundFiles { get; } = new();
        private readonly List<FoundFileViewModel> _allFoundFileViewModelsCache = new();

        private bool? _areAllFoundFilesSelected = false;
        private bool _isUpdatingSelectAll = false;

        public bool? AreAllFoundFilesSelected
        {
            get => _areAllFoundFilesSelected;
            set
            {
                bool selectAll = _areAllFoundFilesSelected != true;

                _areAllFoundFilesSelected = selectAll;
                _isUpdatingSelectAll = true;
                foreach (var file in FilteredFoundFiles)
                {
                    file.IsSelected = selectAll;
                }
                _isUpdatingSelectAll = false;
                OnPropertyChanged();
            }
        }

        public BulkSearchWindow(IEnumerable<SelectableFile> allFiles)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = this;
            _allAvailableFilesLookup = allFiles.ToDictionary(
                f => f.RelativePath,
                f => f,
                StringComparer.OrdinalIgnoreCase);
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var matchedFiles = new HashSet<SelectableFile>();
            var countBuilder = new StringBuilder();

            int lineCount = InputTextBox.LineCount;
            for (int i = 0; i < lineCount; i++)
            {
                string line = InputTextBox.GetLineText(i);
                string term = line.Trim();
                int count = 0;

                if (!string.IsNullOrEmpty(term))
                {
                    foreach (var file in _allAvailableFilesLookup.Values)
                    {
                        if (file.RelativePath.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            count++;
                            matchedFiles.Add(file);
                        }
                    }
                }

                countBuilder.AppendLine(count > 0 ? count.ToString() : string.Empty);
            }

            CountTextBlock.Text = countBuilder.ToString();

            FilteredFoundFiles.Clear();
            foreach (var file in matchedFiles.OrderBy(f => f.RelativePath))
            {
                var vm = _allFoundFileViewModelsCache.FirstOrDefault(v => v.BackingFile == file);
                if (vm == null)
                {
                    vm = new FoundFileViewModel(file, UpdateSelectAllState);
                    _allFoundFileViewModelsCache.Add(vm);
                }
                FilteredFoundFiles.Add(vm);
            }

            UpdateSelectAllState();
        }

        private void InputTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                CountScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
            if (e.HorizontalChange != 0)
            {
                CountScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void UpdateSelectAllState()
        {
            if (_isUpdatingSelectAll) return;

            int selectedCount = FilteredFoundFiles.Count(f => f.IsSelected);
            if (selectedCount == 0)
            {
                _areAllFoundFilesSelected = false;
            }
            else if (selectedCount == FilteredFoundFiles.Count && FilteredFoundFiles.Count > 0)
            {
                _areAllFoundFilesSelected = true;
            }
            else
            {
                _areAllFoundFilesSelected = null;
            }
            OnPropertyChanged(nameof(AreAllFoundFilesSelected));
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var vm in _allFoundFileViewModelsCache)
            {
                if (vm.IsSelected != vm.OriginalIsSelected)
                {
                    vm.BackingFile.IsSelected = vm.IsSelected;
                }
            }
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FoundFileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly Action _onSelectionChanged;
        private bool _isSelected;
        public SelectableFile BackingFile { get; }
        public bool OriginalIsSelected { get; }

        public string RelativePath => BackingFile.RelativePath;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    _onSelectionChanged?.Invoke();
                }
            }
        }

        public FoundFileViewModel(SelectableFile backingFile, Action onSelectionChanged)
        {
            BackingFile = backingFile;
            OriginalIsSelected = backingFile.IsSelected;
            _isSelected = backingFile.IsSelected;
            _onSelectionChanged = onSelectionChanged;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
