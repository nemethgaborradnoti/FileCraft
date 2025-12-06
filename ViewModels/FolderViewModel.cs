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
                    _onStateChanging?.Invoke();
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

        public FolderViewModel(string name, string fullPath, FolderViewModel? parent, Action onStateChanged, Action onStateChanging)
        {
            Name = name;
            FullPath = fullPath;
            Parent = parent;
            _onStateChanged = onStateChanged;
            _onStateChanging = onStateChanging;
            _isSelected = false;
            _isExpanded = true;
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

        private void VerifyCheckState()
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
            else
            {
                newParentState = null;
            }

            SetIsSelected(newParentState, false, true);
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
