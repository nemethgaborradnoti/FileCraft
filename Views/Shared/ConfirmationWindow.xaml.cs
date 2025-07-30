using FileCraft.ViewModels.Shared;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class ConfirmationWindow : Window
    {
        public ConfirmationWindow(ConfirmationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Owner = System.Windows.Application.Current.MainWindow;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
