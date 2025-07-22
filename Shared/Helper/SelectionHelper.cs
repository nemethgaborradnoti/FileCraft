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
    }
}
