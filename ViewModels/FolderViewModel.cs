using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.ViewModels
{
    public class FolderViewModel : INotifyPropertyChanged
    {
        private bool? _isSelected;
        private bool _isExpanded;
        private readonly Action _onSelectionChanged;

        public string Name { get; }
        public string FullPath { get; }
        public FolderViewModel? Parent { get; }
        public ObservableCollection<FolderViewModel> Children { get; } = new ObservableCollection<FolderViewModel>();

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? IsSelected
        {
            get => _isSelected;
            set
            {
                bool targetState = _isSelected != true;

                if (Parent == null && !targetState)
                {
                    return;
                }

                SetIsSelectedWithoutNotification(targetState);

                if (Children.Any())
                {
                    IsExpanded = targetState;
                }

                Parent?.UpdateParentStateWithoutNotification();

                _onSelectionChanged?.Invoke();
            }
        }

        public FolderViewModel(string name, string fullPath, FolderViewModel? parent, Action onSelectionChanged)
        {
            Name = name;
            FullPath = fullPath;
            Parent = parent;
            _onSelectionChanged = onSelectionChanged;
            _isSelected = true;
            _isExpanded = true;
        }

        private void SetIsSelectedWithoutNotification(bool value)
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));

            foreach (var child in Children)
            {
                child.SetIsSelectedWithoutNotification(value);
            }
        }

        private void UpdateParentStateWithoutNotification()
        {
            bool? newState;
            if (Children.All(c => c.IsSelected == true))
            {
                newState = true;
            }
            else if (Children.All(c => c.IsSelected == false || c.IsSelected == null))
            {
                if (Children.Any(c => c.IsSelected != false))
                {
                    newState = null;
                }
                else
                {
                    newState = false;
                }
            }
            else
            {
                newState = null;
            }

            if (_isSelected == newState) return;

            _isSelected = newState;
            OnPropertyChanged(nameof(IsSelected));
            Parent?.UpdateParentStateWithoutNotification();
        }

        public void SetIsExpandedRecursively(bool isExpanded)
        {
            if (Children.Any())
            {
                IsExpanded = isExpanded;
                foreach (var child in Children)
                {
                    child.SetIsExpandedRecursively(isExpanded);
                }
            }
        }

        public IEnumerable<FolderViewModel> GetAllNodes()
        {
            yield return this;
            foreach (var node in Children.SelectMany(child => child.GetAllNodes()))
            {
                yield return node;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
