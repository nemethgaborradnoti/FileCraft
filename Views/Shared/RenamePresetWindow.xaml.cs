using System.Windows;
using System.Windows.Media.Imaging;

namespace FileCraft.Views.Shared
{
    public partial class RenamePresetWindow : Window
    {
        public string NewPresetName { get; private set; } = string.Empty;

        public RenamePresetWindow(string currentName, int presetNumber)
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;
            PromptText.Text = $"Preset {presetNumber} name:";
            NameTextBox.Text = currentName;
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NewPresetName = NameTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
            else
            {
                string iconPath = (System.Windows.Application.Current.FindResource("IconWarning") as BitmapImage)?.UriSource.ToString() ?? string.Empty;
                var notification = new NotificationWindow("Invalid Input", "Preset name cannot be empty.", iconPath);
                notification.ShowDialog();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
