using System.Windows.Input;

namespace FileCraft.Shared.Helpers
{
    public static class TreeViewItemHelper
    {
        public static void HandlePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox)
            {
                e.Handled = true;

                if (checkBox.IsChecked == true)
                {
                    checkBox.IsChecked = false;
                }
                else
                {
                    checkBox.IsChecked = true;
                }
            }
        }
    }
}
