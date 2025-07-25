﻿using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        public async Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrEmpty(includedFolderPaths, nameof(includedFolderPaths), "No folders were selected to export from.");
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

            var allFiles = new List<string>();
            foreach (var folderPath in includedFolderPaths)
            {
                if (Directory.Exists(folderPath))
                {
                    allFiles.AddRange(Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly));
                }
            }

            Guard.AgainstNullOrEmpty(allFiles, nameof(allFiles), "The selected folders contain no files to export.");

            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine("Name;Size (KB);Modification Date;Creation Date;Last Access Date;Format;Full Path");

            foreach (var filePath in allFiles.OrderBy(f => f))
            {
                var fileInfo = new FileInfo(filePath);
                csvBuilder.AppendLine(
                    $"{fileInfo.Name};" +
                    $"{(fileInfo.Length / 1024.0):F2};" +
                    $"{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss};" +
                    $"{fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss};" +
                    $"{fileInfo.LastAccessTime:yyyy-MM-dd HH:mm:ss};" +
                    $"{fileInfo.Extension};" +
                    $"{fileInfo.FullName}");
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, csvBuilder.ToString());

            return outputFilePath;
        }

        public async Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolderPaths, string outputFileName)
        {
            Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");
            Guard.AgainstNull(excludedFolderPaths, nameof(excludedFolderPaths));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

            StringBuilder treeBuilder = new StringBuilder();
            treeBuilder.AppendLine(new DirectoryInfo(sourcePath).Name);

            BuildTree(sourcePath, "", treeBuilder, excludedFolderPaths, true);

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, treeBuilder.ToString());

            return outputFilePath;
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder, ISet<string> excludedFolderPaths, bool isLastParent)
        {
            try
            {
                var subDirectories = Directory.GetDirectories(directoryPath)
                    .Where(d => !excludedFolderPaths.Contains(d))
                    .OrderBy(d => d)
                    .ToArray();

                var files = Directory.GetFiles(directoryPath)
                    .OrderBy(f => f)
                    .ToArray();

                for (int i = 0; i < subDirectories.Length; i++)
                {
                    var subDirInfo = new DirectoryInfo(subDirectories[i]);
                    bool isLast = (i == subDirectories.Length - 1) && (files.Length == 0);
                    builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{subDirInfo.Name}");
                    BuildTree(subDirInfo.FullName, indent + (isLast ? "    " : "│   "), builder, excludedFolderPaths, isLast);
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    bool isLast = i == files.Length - 1;
                    builder.AppendLine($"{indent}{(isLast ? "└── " : "├── ")}{fileInfo.Name}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                builder.AppendLine($"{indent}└── [Access Denied]");
            }
        }

        public async Task<string> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<string> selectedFilePaths, string outputFileName)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedFilePaths, nameof(selectedFilePaths), "No files were selected for export.");

            var contentBuilder = new StringBuilder();

            foreach (var filePath in selectedFilePaths)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    string fileContent = await File.ReadAllTextAsync(filePath);

                    contentBuilder.AppendLine($"===== {fileInfo.Name} file contains:");
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine(fileContent);
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine("===============");
                }
                catch (Exception ex)
                {
                    contentBuilder.AppendLine($"--- Could not read file: {Path.GetFileName(filePath)}. Error: {ex.Message} ---");
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine();
                }
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, contentBuilder.ToString());

            return outputFilePath;
        }
    }
}
