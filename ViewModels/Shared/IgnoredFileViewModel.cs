using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.ViewModels.Shared
{
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