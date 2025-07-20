using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.Windows;

namespace FileCraft
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IDialogService _dialogService;

        public MainWindow()
        {
            InitializeComponent();

            IDialogService dialogService = new DialogService();
            IFileOperationService fileOperationService = new FileOperationService();

            _viewModel = new MainViewModel(fileOperationService, dialogService);

            _dialogService = dialogService;

            this.DataContext = _viewModel;
        }

        private void SourceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = _dialogService.SelectFolder("Select the common source folder");
            if (selectedPath != null)
            {
                _viewModel.SourcePath = selectedPath;
            }
        }

        private void DestinationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = _dialogService.SelectFolder("Select the common destination folder");
            if (selectedPath != null)
            {
                _viewModel.DestinationPath = selectedPath;
            }
        }
    }
}
