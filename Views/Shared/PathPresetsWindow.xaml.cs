using FileCraft.ViewModels.Functional;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class PathPresetsWindow : Window
    {
        public PathPresetsWindow(PathPresetsViewModel viewModel)
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