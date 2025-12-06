using System.Windows;

namespace FileCraft.Views.Shared
{
    public partial class NotificationWindow : Window
    {
        public string IconGlyph { get; }
        public Brush IconBrush { get; }

        public NotificationWindow(string title, string message, string iconGlyph, Brush iconBrush)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            this.Title = title;
            MessageTextBlock.Text = message;
            IconGlyph = iconGlyph;
            IconBrush = iconBrush;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}