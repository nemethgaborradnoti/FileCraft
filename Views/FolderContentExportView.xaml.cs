using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FileCraft.ViewModels;

namespace FileCraft.Views
{
    public partial class FolderContentExportView : System.Windows.Controls.UserControl
    {
        public FolderContentExportView()
        {
            InitializeComponent();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel && viewModel.ExportFolderContentsCommand.CanExecute(null))
            {
                var options = new Dictionary<string, object>
                {
                    { "IncludeSubfolders", includeSubfoldersCheckBox.IsChecked ?? false }
                };
                viewModel.ExportFolderContentsCommand.Execute(options);
            }
        }
    }
}
