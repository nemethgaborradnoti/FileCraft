using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(string title, string content)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            Title = title;
            PreviewTextBox.Text = content;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
