using FileCraft.Services.Interfaces;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        public async Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName, IEnumerable<string> selectedColumns)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrEmpty(includedFolderPaths, nameof(includedFolderPaths), "No folders were selected to export from.");
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedColumns, nameof(selectedColumns), "No columns were selected for export.");

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
            csvBuilder.AppendLine(string.Join(";", selectedColumns));

            var columnExtractors = new Dictionary<string, Func<FileInfo, string>>
            {
                { "Name", fi => fi.Name },
                { "Size (KB)", fi => $"{(fi.Length / 1024.0):F2}" },
                { "Modification Date", fi => $"{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}" },
                { "Creation Date", fi => $"{fi.CreationTime:yyyy-MM-dd HH:mm:ss}" },
                { "Last Access Date", fi => $"{fi.LastAccessTime:yyyy-MM-dd HH:mm:ss}" },
                { "Format", fi => fi.Extension },
                { "Full Path", fi => fi.FullName }
            };

            foreach (var filePath in allFiles.OrderBy(f => f))
            {
                var fileInfo = new FileInfo(filePath);
                var lineParts = new List<string>();
                foreach (var column in selectedColumns)
                {
                    if (columnExtractors.TryGetValue(column, out var extractor))
                    {
                        lineParts.Add(extractor(fileInfo));
                    }
                }
                csvBuilder.AppendLine(string.Join(";", lineParts));
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
