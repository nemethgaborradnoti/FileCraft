using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.Windows;

namespace FileCraft
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel, IDialogService dialogService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.SaveSettings();
        }
    }
}
