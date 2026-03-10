using FileCraft.Models;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class PresetListViewModel : BaseViewModel
    {
        private readonly ObservableCollection<PresetItemViewModel> _itemsSource;
        private readonly Debouncer _filterDebouncer;
        private string _searchText = string.Empty;
        private PresetSortCriteria _sortBy = PresetSortCriteria.DateModified;
        private bool _isDescending = true;

        public ICollectionView ItemsView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    _filterDebouncer.Debounce();
                }
            }
        }

        public PresetSortCriteria SortBy
        {
            get => _sortBy;
            set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    OnPropertyChanged();
                    ApplySort();
                    SortChanged?.Invoke();
                }
            }
        }

        public bool IsDescending
        {
            get => _isDescending;
            set
            {
                if (_isDescending != value)
                {
                    _isDescending = value;
                    OnPropertyChanged();
                    ApplySort();
                    SortChanged?.Invoke();
                }
            }
        }

        public ObservableCollection<SelectableItemViewModel> SortOptions { get; } = new();

        public ICommand ToggleSortDirectionCommand { get; }
        public ICommand SaveNewCommand { get; }

        public event Action? SaveNewRequested;
        public event Action<PresetItemViewModel>? LoadItemRequested;
        public event Action<PresetItemViewModel>? OverwriteItemRequested;
        public event Action<PresetItemViewModel>? DeleteItemRequested;
        public event Action<PresetItemViewModel>? RenameItemRequested;
        public event Action<PresetItemViewModel>? ViewItemDetailsRequested;
        public event Action? SortChanged;

        public PresetListViewModel()
        {
            _itemsSource = new ObservableCollection<PresetItemViewModel>();
            ItemsView = CollectionViewSource.GetDefaultView(_itemsSource);
            ItemsView.Filter = FilterItem;

            _filterDebouncer = new Debouncer(RefreshView);

            ToggleSortDirectionCommand = new RelayCommand(_ => IsDescending = !IsDescending);
            SaveNewCommand = new RelayCommand(_ => SaveNewRequested?.Invoke());

            ApplySort();
        }

        public void InitializeSort(PresetSortCriteria sortBy, bool isDescending)
        {
            _sortBy = sortBy;
            _isDescending = isDescending;
            OnPropertyChanged(nameof(SortBy));
            OnPropertyChanged(nameof(IsDescending));
            ApplySort();
        }

        public void SetItems(IEnumerable<PresetItemViewModel> items)
        {
            _itemsSource.Clear();
            foreach (var item in items)
            {
                HookItemEvents(item);
                _itemsSource.Add(item);
            }
            ItemsView.Refresh();
        }

        public void AddItem(PresetItemViewModel item)
        {
            HookItemEvents(item);
            _itemsSource.Add(item);
        }

        public void RemoveItem(PresetItemViewModel item)
        {
            UnhookItemEvents(item);
            _itemsSource.Remove(item);
        }

        public void UpdateItem(PresetItemViewModel item)
        {
            ItemsView.Refresh();
        }

        private void HookItemEvents(PresetItemViewModel item)
        {
            item.LoadRequested += OnItemLoadRequested;
            item.OverwriteRequested += OnItemOverwriteRequested;
            item.DeleteRequested += OnItemDeleteRequested;
            item.RenameRequested += OnItemRenameRequested;
            item.ViewDetailsRequested += OnItemViewDetailsRequested;
        }

        private void UnhookItemEvents(PresetItemViewModel item)
        {
            item.LoadRequested -= OnItemLoadRequested;
            item.OverwriteRequested -= OnItemOverwriteRequested;
            item.DeleteRequested -= OnItemDeleteRequested;
            item.RenameRequested -= OnItemRenameRequested;
            item.ViewDetailsRequested -= OnItemViewDetailsRequested;
        }

        private void OnItemLoadRequested(PresetItemViewModel item) => LoadItemRequested?.Invoke(item);
        private void OnItemOverwriteRequested(PresetItemViewModel item) => OverwriteItemRequested?.Invoke(item);
        private void OnItemDeleteRequested(PresetItemViewModel item) => DeleteItemRequested?.Invoke(item);
        private void OnItemRenameRequested(PresetItemViewModel item) => RenameItemRequested?.Invoke(item);
        private void OnItemViewDetailsRequested(PresetItemViewModel item) => ViewItemDetailsRequested?.Invoke(item);

        private bool FilterItem(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is PresetItemViewModel item)
            {
                return item.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return true;
        }

        private void RefreshView()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ItemsView.Refresh();
            });
        }

        private void ApplySort()
        {
            ItemsView.SortDescriptions.Clear();
            var direction = IsDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;

            switch (SortBy)
            {
                case PresetSortCriteria.Name:
                    ItemsView.SortDescriptions.Add(new SortDescription(nameof(PresetItemViewModel.Name), direction));
                    break;
                case PresetSortCriteria.DateModified:
                    ItemsView.SortDescriptions.Add(new SortDescription(nameof(PresetItemViewModel.LastModified), direction));
                    break;
            }
        }
    }
}