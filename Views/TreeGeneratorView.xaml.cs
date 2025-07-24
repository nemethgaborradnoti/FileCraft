using System.Windows.Input;

namespace FileCraft.Views
{
    public partial class TreeGeneratorView : System.Windows.Controls.UserControl
    {
        public TreeGeneratorView()
        {
            InitializeComponent();
        }

        private void FolderTreeCheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
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
