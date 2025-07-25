using FileCraft.Shared.Helpers;
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
            TreeViewItemHelper.HandlePreviewMouseDown(sender, e);
        }
    }
}