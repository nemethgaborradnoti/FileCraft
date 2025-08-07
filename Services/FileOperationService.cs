using FileCraft.Models;
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
                { "Size (byte)", fi => fi.Length.ToString() },
                { "CreationTime", fi => $"{fi.CreationTime:yyyy-MM-dd HH:mm:ss}" },
                { "LastWriteTime", fi => $"{fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}" },
                { "LastAccessTime", fi => $"{fi.LastAccessTime:yyyy-MM-dd HH:mm:ss}" },
                { "IsReadOnly", fi => fi.IsReadOnly.ToString() },
                { "Attributes", fi => fi.Attributes.ToString() },
                { "FullPath", fi => fi.FullName },
                { "Parent", fi => fi.Directory?.Name ?? string.Empty },
                { "Format", fi => fi.Extension }
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

        public async Task<string> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedFiles, nameof(selectedFiles), "No files were selected for export.");

            var contentBuilder = new StringBuilder();
            var selectedFilesList = selectedFiles.ToList();

            for (int i = 0; i < selectedFilesList.Count; i++)
            {
                var file = selectedFilesList[i];
                try
                {
                    string fileContent = await File.ReadAllTextAsync(file.FullPath);

                    contentBuilder.AppendLine($"=== {file.RelativePath} file contains:");
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine(fileContent);

                    if (i < selectedFilesList.Count - 1)
                    {
                        contentBuilder.AppendLine();
                        contentBuilder.AppendLine("===========");
                    }
                }
                catch (Exception ex)
                {
                    contentBuilder.AppendLine($"--- Could not read file: {file.FileName}. Error: {ex.Message} ---");
                    contentBuilder.AppendLine();
                }
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, contentBuilder.ToString());

            return outputFilePath;
        }

        public async Task<string> RenameFilesAsync(string sourcePath, string destinationPath, string outputFileName, bool appendTimestamp, bool includeFolders)
        {
            Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
            Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNonExistentDirectory(destinationPath, "The selected destination folder does not exist.");
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

            var itemsToRename = new List<string>();
            itemsToRename.AddRange(Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly));
            if (includeFolders)
            {
                itemsToRename.AddRange(Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly));
            }

            if (!itemsToRename.Any())
            {
                return "No items found in the source directory to rename.";
            }

            var sortedOriginalItems = itemsToRename.OrderBy(f => f).ToList();
            var tempMappings = new Dictionary<string, string>();
            var failedDuringTemp = new List<string>();

            await Task.Run(() =>
            {
                foreach (var originalPath in sortedOriginalItems)
                {
                    try
                    {
                        string tempPath = Path.Combine(sourcePath, Guid.NewGuid().ToString() + ".tmp_fc");
                        Directory.Move(originalPath, tempPath);
                        tempMappings.Add(tempPath, originalPath);
                    }
                    catch
                    {
                        failedDuringTemp.Add(Path.GetFileName(originalPath));
                    }
                }
            });

            var logBuilder = new StringBuilder();
            var failedFinal = new List<string>();
            int successCount = 0;
            int currentNumber = 1;

            logBuilder.AppendLine($"--- SOURCE PATH ---");
            logBuilder.AppendLine($"{sourcePath}");
            logBuilder.AppendLine();
            logBuilder.AppendLine("--- RENAME DETAILS ---");

            var sortedTempItems = tempMappings.OrderBy(kvp => kvp.Value).ToList();

            await Task.Run(() =>
            {
                foreach (var kvp in sortedTempItems)
                {
                    string tempPath = kvp.Key;
                    string originalPath = kvp.Value;
                    string originalName = Path.GetFileName(originalPath);
                    try
                    {
                        bool isDirectory = File.GetAttributes(tempPath).HasFlag(FileAttributes.Directory);
                        string extension = isDirectory ? "" : Path.GetExtension(originalPath);
                        string finalName = $"{currentNumber:D4}{extension}";
                        string finalPath = Path.Combine(sourcePath, finalName);

                        Directory.Move(tempPath, finalPath);
                        logBuilder.AppendLine($"{originalName} -> {finalName}");
                        successCount++;
                        currentNumber++;
                    }
                    catch (Exception ex)
                    {
                        failedFinal.Add($"{originalName} (Reason: {ex.Message})");
                    }
                }
            });

            var allFailed = failedDuringTemp.Select(f => $"{f} (Reason: Failed during temporary rename phase)").ToList();
            allFailed.AddRange(failedFinal);

            logBuilder.AppendLine("\n--- SUMMARY ---");
            logBuilder.AppendLine($"Successfully renamed: {successCount}");
            logBuilder.AppendLine($"Could not rename: {allFailed.Count}");

            if (allFailed.Any())
            {
                logBuilder.AppendLine("\n--- FAILED ITEMS ---");
                foreach (var failed in allFailed)
                {
                    logBuilder.AppendLine(failed);
                }
            }

            string finalFileName = appendTimestamp
                ? $"{outputFileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}"
                : outputFileName;

            string logFilePath = Path.Combine(destinationPath, $"{finalFileName}.txt");
            await File.WriteAllTextAsync(logFilePath, logBuilder.ToString());

            return logFilePath;
        }
    }
}
