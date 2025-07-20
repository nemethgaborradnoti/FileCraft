using FileCraft.Services;
using FileCraft.ViewModels;
using System.IO;
using System.Text;
using System.Windows;

namespace FileCraft.Views
{
    public partial class TreeGeneratorView : System.Windows.Controls.UserControl
    {
        private readonly IDialogService _dialogService;

        public TreeGeneratorView()
        {
            InitializeComponent();
            _dialogService = new DialogService();
            excludeFoldersTextBox.Text = "obj;bin;.git;.vs";
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel == null)
            {
                _dialogService.ShowNotification("Application Error", "Could not access the main view model.");
                return;
            }

            string sourceDirectory = viewModel.SourcePath;
            string destinationDirectory = viewModel.DestinationPath;

            if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(destinationDirectory))
            {
                _dialogService.ShowNotification("Validation Error", "Please select both a source and a destination folder in the common area above.");
                return;
            }

            if (!Directory.Exists(sourceDirectory))
            {
                _dialogService.ShowNotification("Path Error", "The selected source folder does not exist.");
                return;
            }

            try
            {
                var excludedFolders = new HashSet<string>(
                    excludeFoldersTextBox.Text.Split(';')
                                             .Select(f => f.Trim())
                                             .Where(f => !string.IsNullOrWhiteSpace(f)),
                    StringComparer.OrdinalIgnoreCase);

                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(sourceDirectory);

                BuildTree(sourceDirectory, "", treeBuilder, excludedFolders);

                string outputFileName = $"treestructure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationDirectory, outputFileName);

                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                _dialogService.ShowNotification("Success", $"Tree structure file was created successfully!\n\nSaved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred:\n\n{ex.Message}");
            }
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder, HashSet<string> excludedFolders)
        {
            string[] subDirectories;
            string[] files;

            try
            {
                subDirectories = Directory.GetDirectories(directoryPath)
                                          .Where(d => !excludedFolders.Contains(Path.GetFileName(d)))
                                          .ToArray();
                files = Directory.GetFiles(directoryPath);
            }
            catch (UnauthorizedAccessException)
            {
                builder.AppendLine($"{indent}└── [Access Denied]");
                return;
            }

            for (int i = 0; i < subDirectories.Length; i++)
            {
                var subDir = subDirectories[i];
                bool isLast = (i == subDirectories.Length - 1) && (files.Length == 0);
                builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{Path.GetFileName(subDir)}");
                BuildTree(subDir, indent + (isLast ? "    " : "│   "), builder, excludedFolders);
            }

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                bool isLast = i == files.Length - 1;
                builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{Path.GetFileName(file)}");
            }
        }
    }
}
