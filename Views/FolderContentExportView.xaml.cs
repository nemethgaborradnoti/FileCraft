using FileCraft.Shared.Helpers;
using System.Windows.Input;

namespace FileCraft.Views
{
    public partial class FolderContentExportView : System.Windows.Controls.UserControl
    {
        public FolderContentExportView()
        {
            InitializeComponent();
        }

        private void FolderTreeCheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItemHelper.HandlePreviewMouseDown(sender, e);
        }
    }
}