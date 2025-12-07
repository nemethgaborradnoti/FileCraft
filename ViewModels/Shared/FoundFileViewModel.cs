using FileCraft.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.ViewModels.Shared
{
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