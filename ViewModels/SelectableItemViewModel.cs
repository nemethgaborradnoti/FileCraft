using FileCraft.ViewModels.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.ViewModels
{
    public class SelectableItemViewModel : INotifyPropertyChanged, ISelectable
    {
        private bool _isSelected;
        private readonly Action? _onStateChanging;

        public string Name { get; }

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

        public SelectableItemViewModel(string name, bool isSelected, Action? onStateChanging)
        {
            Name = name;
            _isSelected = isSelected;
            _onStateChanging = onStateChanging;
        }

        public void SetIsSelectedInternal(bool value)
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
