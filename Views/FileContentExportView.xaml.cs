using FileCraft.ViewModels;
using System.Windows;

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
            if (this.DataContext is MainViewModel viewModel)
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
    }
}
