using System.Windows;
using Application = System.Windows.Application;

namespace FileCraft.Views.Shared
{
    public partial class EditIgnoredFoldersWindow : Window
    {
        public string IgnoredFoldersText { get; private set; } = string.Empty;

        public EditIgnoredFoldersWindow(string currentFolders)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            FoldersTextBox.Text = currentFolders;
            FoldersTextBox.Focus();
            FoldersTextBox.CaretIndex = FoldersTextBox.Text.Length;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            IgnoredFoldersText = FoldersTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
