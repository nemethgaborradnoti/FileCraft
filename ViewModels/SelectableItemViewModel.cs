using FileCraft.ViewModels.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.ViewModels
{
    public class SelectableItemViewModel : INotifyPropertyChanged, ISelectable
    {
        private bool _isSelected;

        public string Name { get; }

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

        public SelectableItemViewModel(string name, bool isSelected = true)
        {
            Name = name;
            _isSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
