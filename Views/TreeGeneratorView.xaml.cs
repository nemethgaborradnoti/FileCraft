using FileCraft.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FileCraft.Views
{
    public partial class TreeGeneratorView : System.Windows.Controls.UserControl
    {
        private readonly IDialogService _dialogService;

        public TreeGeneratorView()
        {
            InitializeComponent();
            _dialogService = new DialogService();

            // Set default values for the exclude folders TextBox.
            excludeFoldersTextBox.Text = "obj;bin;.git;.vs";
        }

        private void SourceBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = _dialogService.SelectFolder("Select the source folder");
            if (selectedPath != null)
            {
                sourcePathTextBox.Text = selectedPath;
            }
        }

        private void DestinationBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPath = _dialogService.SelectFolder("Select the destination folder for saving");
            if (selectedPath != null)
            {
                destinationPathTextBox.Text = selectedPath;
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
                var excludedFolders = new HashSet<string>(
                    excludeFoldersTextBox.Text.Split(';')
                                         .Select(f => f.Trim())
                                         .Where(f => !string.IsNullOrWhiteSpace(f)),
                    StringComparer.OrdinalIgnoreCase);

                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(sourceDirectory);

                BuildTree(sourceDirectory, "", treeBuilder, excludedFolders);

                string outputFileName = $"treestructure{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationDirectory, outputFileName);

                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                statusTextBlock.Text = $"Done! Saved to: {outputFilePath}";
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = $"An error occurred: {ex.Message}";
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
