using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.Windows;

namespace FileCraft
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IDialogService _dialogService;

        public MainWindow(MainViewModel viewModel, IDialogService dialogService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _dialogService = dialogService;

            this.DataContext = _viewModel;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.SaveSettings();
        }

        private void SourceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = _dialogService.SelectFolder("Select the common source folder");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.SourcePath = selectedPath;
            }
        }

        private void DestinationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = _dialogService.SelectFolder("Select the common destination folder");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.DestinationPath = selectedPath;
            }
        }
    }
}
