using FileCraft.ViewModels.Shared;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class PresetDetailsWindow : Window
    {
        public PresetDetailsWindow(PresetDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Owner = Application.Current.MainWindow;

            viewModel.RequestClose += () =>
            {
                Close();
            };
        }
    }
}