namespace FileCraft.Views.Shared
{
    using System.Windows;

    public partial class NotificationWindow : Window
    {
        public string IconPath { get; }

        public NotificationWindow(string title, string message, string iconPath)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            this.Title = title;
            MessageTextBlock.Text = message;
            IconPath = iconPath;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}