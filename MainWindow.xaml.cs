using System.IO;
using System.Text;
using System.Windows;

namespace FileCraft
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SourceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the source folder";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                sourcePathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void DestinationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Select the destination folder for saving";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                destinationPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string sourceDirectory = sourcePathTextBox.Text;
            string destinationDirectory = destinationPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(destinationDirectory))
            {
                statusTextBlock.Text = "Error: Both source and destination folders must be selected!";
                return;
            }

            if (!Directory.Exists(sourceDirectory))
            {
                statusTextBlock.Text = "Error: The source folder does not exist!";
                return;
            }

            statusTextBlock.Text = "Processing...";

            try
            {
                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(sourceDirectory);

                BuildTree(sourceDirectory, "", treeBuilder);

                string outputFileName = $"directory_structure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationDirectory, outputFileName);

                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                statusTextBlock.Text = $"Done! Saved to: {outputFilePath}";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = $"An error occurred: {ex.Message}";
            }
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder)
        {
            string[] subDirectories;
            string[] files;

            try
            {
                subDirectories = Directory.GetDirectories(directoryPath);
                files = Directory.GetFiles(directoryPath);
            }
            catch (UnauthorizedAccessException)
            {
                builder.AppendLine($"{indent}+-- [Access Denied]");
                return;
            }


            for (int i = 0; i < subDirectories.Length; i++)
            {
                var subDir = subDirectories[i];
                bool isLast = (i == subDirectories.Length - 1) && (files.Length == 0);
                builder.AppendLine($"{indent}{(isLast ? "+-- " : "|-- ")}{Path.GetFileName(subDir)}");
                BuildTree(subDir, indent + (isLast ? "    " : "|   "), builder);
            }

            for (int i = 0; i < files.Length; i++)  
            {
                var file = files[i];
                bool isLast = i == files.Length - 1;
                builder.AppendLine($"{indent}{(isLast ? "+-- " : "|-- ")}{Path.GetFileName(file)}");
            }
        }
    }
}
