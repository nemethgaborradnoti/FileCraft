using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Functional;
using System.Windows;
using System.Windows.Input;

namespace FileCraft.Views
{
    public partial class FileContentExportView : System.Windows.Controls.UserControl
    {
        public FileContentExportView()
        {
            InitializeComponent();
        }

        private void OnCheckAll(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is FileContentExportViewModel viewModel)
            {
                bool isChecked = (sender as System.Windows.Controls.CheckBox)?.IsChecked ?? false;
                if (isChecked)
                {
                    if (viewModel.SelectAllFilesCommand.CanExecute(null))
                        viewModel.SelectAllFilesCommand.Execute(null);
                }
                else
                {
                    if (viewModel.DeselectAllFilesCommand.CanExecute(null))
                        viewModel.DeselectAllFilesCommand.Execute(null);
                }
            }
        }

        private void OnCheckAllExtensions(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is FileContentExportViewModel viewModel)
            {
                bool isChecked = (sender as System.Windows.Controls.CheckBox)?.IsChecked ?? false;
                if (isChecked)
                {
                    if (viewModel.SelectAllExtensionsCommand.CanExecute(null))
                        viewModel.SelectAllExtensionsCommand.Execute(null);
                }
                else
                {
                    if (viewModel.DeselectAllExtensionsCommand.CanExecute(null))
                        viewModel.DeselectAllExtensionsCommand.Execute(null);
                }
            }
        }

        private void FolderTreeCheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItemHelper.HandlePreviewMouseDown(sender, e);
        }
    }
}
