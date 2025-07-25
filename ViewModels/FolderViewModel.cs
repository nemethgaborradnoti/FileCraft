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
        private bool _isProcessingSelectionChange = false;
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
            set
            {
                if (_isProcessingSelectionChange) return;
                SetIsSelected(value, updateChildren: true, updateParent: true);
            }
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

            _isProcessingSelectionChange = true;
            try
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                _onStateChanged?.Invoke();

                if (updateChildren && _isSelected.HasValue)
                {
                    var queue = new Queue<FolderViewModel>(Children);
                    while (queue.Count > 0)
                    {
                        var child = queue.Dequeue();

                        child._isSelected = _isSelected;
                        child.OnPropertyChanged(nameof(IsSelected));
                        child._onStateChanged?.Invoke();

                        if (_isSelected.Value && !child.IsExpanded)
                        {
                            child.IsExpanded = true;
                        }

                        foreach (var grandChild in child.Children)
                        {
                            queue.Enqueue(grandChild);
                        }
                    }
                }

                if (updateParent && Parent != null)
                {
                    Parent.VerifyCheckState();
                }
            }
            finally
            {
                _isProcessingSelectionChange = false;
            }
        }

        private void VerifyCheckState()
        {
            bool? state;
            if (Children.Any() && Children.All(c => c.IsSelected == true))
            {
                state = true;
            }
            else if (Children.Any() && Children.All(c => c.IsSelected == false))
            {
                state = false;
            }
            else
            {
                state = null;
            }

            if (_isSelected != state)
            {
                SetIsSelected(state, false, true);
            }
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
            var queue = new Queue<FolderViewModel>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node.IsExpanded != isExpanded)
                {
                    node.IsExpanded = isExpanded;
                }

                foreach (var child in node.Children)
                {
                    queue.Enqueue(child);
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
