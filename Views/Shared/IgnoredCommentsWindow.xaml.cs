using FileCraft.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class IgnoredCommentsWindow : Window
    {
        public ObservableCollection<IgnoredFileViewModel> Files { get; } = new();

        public IgnoredCommentsWindow(IEnumerable<SelectableFile> selectedFiles, IEnumerable<string> previouslyIgnoredFiles)
        {
            InitializeComponent();
            DataContext = this;
            Owner = System.Windows.Application.Current.MainWindow;

            var ignoredSet = new HashSet<string>(previouslyIgnoredFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var file in selectedFiles.OrderBy(f => f.RelativePath))
            {
                bool isIgnored = ignoredSet.Contains(file.RelativePath);
                Files.Add(new IgnoredFileViewModel(file.RelativePath, isIgnored));
            }
        }

        public IEnumerable<string> GetIgnoredFilePaths()
        {
            return Files.Where(f => f.IsSelected).Select(f => f.RelativePath);
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
    }

    public class IgnoredFileViewModel : INotifyPropertyChanged
    {
        public string RelativePath { get; }

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

        public IgnoredFileViewModel(string relativePath, bool isSelected)
        {
            RelativePath = relativePath;
            IsSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}