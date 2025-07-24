using FileCraft.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileCraft.ViewModels
{
    public class FolderViewModel : INotifyPropertyChanged
    {
        private bool? _isSelected;
        private bool _isExpanded;
        private readonly Action _onStateChanged;

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
                    _onStateChanged?.Invoke();
                }
            }
        }

        public bool? IsSelected
        {
            get => _isSelected;
            set => SetIsSelected(value, updateChildren: true, updateParent: true);
        }

        public FolderViewModel(string name, string fullPath, FolderViewModel? parent, Action onStateChanged)
        {
            Name = name;
            FullPath = fullPath;
            Parent = parent;
            _onStateChanged = onStateChanged;
            _isSelected = true;
            _isExpanded = true;
        }

        private void SetIsSelected(bool? value, bool updateChildren, bool updateParent)
        {
            if (_isSelected == value) return;

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));

            if (updateChildren && _isSelected.HasValue)
            {
                if (_isSelected.Value)
                {
                    SetIsExpandedRecursively(true);
                }
                else
                {
                    IsExpanded = false;
                }

                foreach (var child in Children)
                {
                    child.SetIsSelected(_isSelected, true, false);
                }
            }

            if (updateParent && Parent != null)
            {
                Parent.VerifyCheckState();
            }

            _onStateChanged?.Invoke();
        }

        private void VerifyCheckState()
        {
            bool? state = null;
            if (Children.All(c => c.IsSelected == true))
            {
                state = true;
            }
            else if (Children.All(c => c.IsSelected == false))
            {
                state = false;
            }

            SetIsSelected(state, false, true);
        }

        public void ApplyState(FolderState state)
        {
            _isSelected = state.IsSelected;
            _isExpanded = state.IsExpanded;

            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsExpanded));
        }

        public void SetIsExpandedRecursively(bool isExpanded)
        {
            IsExpanded = isExpanded;
            foreach (var child in Children)
            {
                child.SetIsExpandedRecursively(isExpanded);
            }
            _onStateChanged?.Invoke();
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
