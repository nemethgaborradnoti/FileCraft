using FileCraft.ViewModels.Interfaces;

namespace FileCraft.Shared.Helpers
{
    public static class SelectionHelper
    {
        public static void SetSelectionState<T>(IEnumerable<T> items, bool isSelected) where T : ISelectable
        {
            foreach (var item in items)
            {
                item.IsSelected = isSelected;
            }
        }

        public static bool? GetMasterSelectionState<T>(ICollection<T> items) where T : ISelectable
        {
            if (items == null || !items.Any())
            {
                return false;
            }

            int selectedCount = items.Count(i => i.IsSelected);

            if (selectedCount == 0)
            {
                return false;
            }
            if (selectedCount == items.Count)
            {
                return true;
            }
            return null;
        }
    }
}
