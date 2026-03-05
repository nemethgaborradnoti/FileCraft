using FileCraft.Shared.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class PresetItemViewModel : INotifyPropertyChanged
    {
        public object Id { get; }
        public string Name { get; }
        public DateTime LastModified { get; }
        public string Description { get; }
        public object RawData { get; }

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

        public ICommand LoadCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        public event Action<PresetItemViewModel>? LoadRequested;
        public event Action<PresetItemViewModel>? DeleteRequested;
        public event Action<PresetItemViewModel>? RenameRequested;
        public event Action<PresetItemViewModel>? ViewDetailsRequested;

        public PresetItemViewModel(object id, string name, DateTime lastModified, string description, object rawData)
        {
            Id = id;
            Name = name;
            LastModified = lastModified;
            Description = description;
            RawData = rawData;

            LoadCommand = new RelayCommand(_ => LoadRequested?.Invoke(this));
            DeleteCommand = new RelayCommand(_ => DeleteRequested?.Invoke(this));
            RenameCommand = new RelayCommand(_ => RenameRequested?.Invoke(this));
            ViewDetailsCommand = new RelayCommand(_ => ViewDetailsRequested?.Invoke(this));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}