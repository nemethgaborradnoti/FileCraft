using FileCraft.Models;
using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class ExitConfirmationWindow : Window
    {
        public ExitConfirmationResult Result { get; private set; } = ExitConfirmationResult.Cancel;

        public ExitConfirmationWindow(string title, string message)
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;
            this.Title = title;
            MessageTextBlock.Text = message;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ExitConfirmationResult.Save;
            this.Close();
        }

        private void DontSaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ExitConfirmationResult.DontSave;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = ExitConfirmationResult.Cancel;
            this.Close();
        }
    }
}
