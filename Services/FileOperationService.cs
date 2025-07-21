using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        public Task<string> ExportFolderContentsAsync(string sourcePath, string destinationPath, bool includeSubfolders)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");

                var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(sourcePath, "*.*", searchOption);

                if (files.Length == 0)
                {
                    throw new InvalidOperationException("The selected folder contains no files to export.");
                }

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("Name;Size (KB);Modification Date;Creation Date;Last Access Date;Format");

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    csvBuilder.AppendLine(
                        $"{fileInfo.Name};" +
                        $"{(fileInfo.Length / 1024.0):F2};" +
                        $"{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss};" +
                        $"{fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss};" +
                        $"{fileInfo.LastAccessTime:yyyy-MM-dd HH:mm:ss};" +
                        $"{fileInfo.Extension}");
                }

                string outputFileName = $"ExportedContents_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationPath, outputFileName);
                File.WriteAllText(outputFilePath, csvBuilder.ToString());

                return outputFilePath;
            });
        }

        /// <summary>
        /// Generates a text file representing the folder structure.
        /// </summary>
        /// <param name="sourcePath">The root directory to start from.</param>
        /// <param name="destinationPath">The directory where the output file will be saved.</param>
        /// <param name="excludedFolderPaths">A set of full folder paths to exclude from the tree.</param>
        /// <returns>The path to the generated file.</returns>
        public Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolderPaths)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");

                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(new DirectoryInfo(sourcePath).Name);

                BuildTree(sourcePath, "", treeBuilder, excludedFolderPaths, true);

                string outputFileName = $"treestructure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationPath, outputFileName);
                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                return outputFilePath;
            });
        }

        private void BuildTree(string directoryPath, string indent, StringBuilder builder, ISet<string> excludedFolderPaths, bool isLastParent)
        {
            try
            {
                // Filter subdirectories based on the full path for robust exclusion.
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

        public Task<string> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<string> selectedFilePaths)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNull(selectedFilePaths, nameof(selectedFilePaths));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));

                if (!selectedFilePaths.Any())
                {
                    throw new InvalidOperationException("No files were selected for export.");
                }

                var contentBuilder = new StringBuilder();

                foreach (var filePath in selectedFilePaths)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        string fileContent = File.ReadAllText(filePath);

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

                string outputFileName = $"FileContentsExport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationPath, outputFileName);
                File.WriteAllText(outputFilePath, contentBuilder.ToString());

                return outputFilePath;
            });
        }
    }
}
