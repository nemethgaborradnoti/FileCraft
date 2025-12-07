using FileCraft.ViewModels.Shared;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class IgnoredCommentsWindow : Window
    {
        private readonly IgnoredCommentsViewModel _viewModel;

        public IgnoredCommentsWindow(IgnoredCommentsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Owner = Application.Current.MainWindow;

            _viewModel.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        public IEnumerable<string> GetIgnoredFilePaths()
        {
            return _viewModel.GetIgnoredFilePaths();
        }
    }
}