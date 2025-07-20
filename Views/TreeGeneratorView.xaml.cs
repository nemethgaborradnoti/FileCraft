using System.Windows;
using FileCraft.ViewModels;

namespace FileCraft.Views
{
    public partial class TreeGeneratorView : System.Windows.Controls.UserControl
    {
        public TreeGeneratorView()
        {
            InitializeComponent();
            excludeFoldersTextBox.Text = "obj;bin;.git;.vs";
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainViewModel viewModel && viewModel.GenerateTreeStructureCommand.CanExecute(null))
            {
                var options = new Dictionary<string, object>
                {
                    { "ExcludeFoldersText", excludeFoldersTextBox.Text ?? "" }
                };
                viewModel.GenerateTreeStructureCommand.Execute(options);
            }
        }
    }
}
