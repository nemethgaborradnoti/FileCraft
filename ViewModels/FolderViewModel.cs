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
        private readonly Action _onStateChanging;
        private readonly Func<string, FolderViewModel, Task<IEnumerable<FolderViewModel>>>? _loadChildren;
        private bool _isProcessingSelectionChange = false;

        public string Name { get; }
        public string FullPath { get; }
        public FolderViewModel? Parent { get; }
        public ObservableCollection<FolderViewModel> Children { get; } = new ObservableCollection<FolderViewModel>();
        public bool IsDummy { get; init; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _onStateChanging?.Invoke();
                    _isExpanded = value;

                    if (_isExpanded && Children.Count == 1 && Children[0].IsDummy)
                    {
                        _ = LoadChildrenAsync();
                    }

                    OnPropertyChanged();
                    _onStateChanged?.Invoke();
                }
            }
        }

        public bool? IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isProcessingSelectionChange) return;

                _onStateChanging?.Invoke();

                _isProcessingSelectionChange = true;
                try
                {
                    bool newSelectionValue = (_isSelected != true);
                    SetIsSelected(newSelectionValue, updateChildren: true, updateParent: true);
                }
                finally
                {
                    _isProcessingSelectionChange = false;
                }

                _onStateChanged?.Invoke();
            }
        }

        public FolderViewModel(string name, string fullPath, FolderViewModel? parent, Action onStateChanged, Action onStateChanging, Func<string, FolderViewModel, Task<IEnumerable<FolderViewModel>>>? loadChildren)
        {
            Name = name;
            FullPath = fullPath;
            Parent = parent;
            _onStateChanged = onStateChanged;
            _onStateChanging = onStateChanging;
            _loadChildren = loadChildren;
            _isSelected = false;
            _isExpanded = false;
        }

        public static FolderViewModel CreateDummy()
        {
            return new FolderViewModel("Loading...", string.Empty, null, () => { }, () => { }, null)
            {
                IsDummy = true
            };
        }

        private async Task LoadChildrenAsync()
        {
            if (_loadChildren != null)
            {
                var loadedChildren = await _loadChildren(FullPath, this);

                Children.Clear();
                foreach (var child in loadedChildren)
                {
                    if (IsSelected.HasValue)
                    {
                        child.SetIsSelected(IsSelected.Value, updateChildren: true, updateParent: false);
                    }
                    Children.Add(child);
                }
                VerifyCheckState();
            }
        }

        private void SetIsSelected(bool? value, bool updateChildren, bool updateParent)
        {
            if (_isSelected == value) return;

            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));

            if (updateChildren)
            {
                foreach (var child in Children)
                {
                    child.SetIsSelected(value, true, false);
                }
            }

            if (updateParent && Parent != null)
            {
                Parent.VerifyCheckState();
            }
        }

        public void VerifyCheckState()
        {
            bool? newParentState;

            if (!Children.Any())
            {
                return;
            }

            if (Children.All(c => c.IsSelected == true))
            {
                newParentState = true;
            }
            else if (Children.All(c => c.IsSelected == false))
            {
                newParentState = false;
            }
            else
            {
                newParentState = null;
            }

            SetIsSelected(newParentState, false, true);
        }

        public async Task ApplyStateAsync(FolderState state)
        {
            _isSelected = state.IsSelected;
            _isExpanded = state.IsExpanded;

            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(IsExpanded));

            if (_isExpanded && Children.Count == 1 && Children[0].IsDummy)
            {
                await LoadChildrenAsync();
            }

            if (Parent != null)
            {
                Parent.VerifyCheckState();
            }
        }

        public void ApplyState(FolderState state)
        {
            _ = ApplyStateAsync(state);
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