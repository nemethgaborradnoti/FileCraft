using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        public Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNull(includedFolderPaths, nameof(includedFolderPaths));
                Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

                var allFiles = new List<string>();
                foreach (var folderPath in includedFolderPaths)
                {
                    if (Directory.Exists(folderPath))
                    {
                        allFiles.AddRange(Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly));
                    }
                }

                if (allFiles.Count == 0)
                {
                    throw new InvalidOperationException("The selected folders contain no files to export.");
                }

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
                File.WriteAllText(outputFilePath, csvBuilder.ToString());

                return outputFilePath;
            });
        }

        public Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolderPaths, string outputFileName)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");
                Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

                StringBuilder treeBuilder = new StringBuilder();
                treeBuilder.AppendLine(new DirectoryInfo(sourcePath).Name);

                BuildTree(sourcePath, "", treeBuilder, excludedFolderPaths, true);

                string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
                File.WriteAllText(outputFilePath, treeBuilder.ToString());

                return outputFilePath;
            });
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

        public Task<string> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<string> selectedFilePaths, string outputFileName)
        {
            return Task.Run(() =>
            {
                Guard.AgainstNull(selectedFilePaths, nameof(selectedFilePaths));
                Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
                Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

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

                string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
                File.WriteAllText(outputFilePath, contentBuilder.ToString());

                return outputFilePath;
            });
        }
    }
}
