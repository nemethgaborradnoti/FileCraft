using FileCraft.ViewModels.Shared;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class ViewContentWindow : Window
    {
        public ViewContentWindow(ViewContentViewModel viewModel)
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