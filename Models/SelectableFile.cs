using FileCraft.ViewModels.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.Models
{
    public class SelectableFile : INotifyPropertyChanged, ISelectable
    {
        private bool _isSelected;
        private Action? _onStateChanging;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _onStateChanging?.Invoke();
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SetIsSelectedInternal(bool value)
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public void SetStateChangingAction(Action onStateChanging)
        {
            _onStateChanging = onStateChanging;
        }

        public string FileName { get; set; } = string.Empty;

        public string FullPath { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
