using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.Shared.Validation;
using System.IO;
using System.Text;

namespace FileCraft.Services
{
    public class FileOperationService : IFileOperationService
    {
        private readonly ISharedStateService _sharedStateService;
        private readonly IFileQueryService _fileQueryService;

        public FileOperationService(ISharedStateService sharedStateService, IFileQueryService fileQueryService)
        {
            _sharedStateService = sharedStateService;
            _fileQueryService = fileQueryService;
        }

        public async Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName, IEnumerable<string> selectedColumns)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrEmpty(includedFolderPaths, nameof(includedFolderPaths), "No folders were selected to export from.");
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedColumns, nameof(selectedColumns), "No columns were selected for export.");

            var allFiles = _fileQueryService.GetAllFiles(includedFolderPaths).OrderBy(f => f.FullName).ToList();

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

            foreach (var fileInfo in allFiles)
            {
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
                var ignoredFolderNames = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);

                var subDirectories = Directory.GetDirectories(directoryPath)
                    .Where(d => !excludedFolderPaths.Contains(d))
                    .Where(d => !ignoredFolderNames.Contains(new DirectoryInfo(d).Name))
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

        public async Task<(string FilePath, int XmlCommentLines, int XmlCommentChars)> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName, ISet<string> filesToIgnoreXmlComments)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedFiles, nameof(selectedFiles), "No files were selected for export.");

            var contentBuilder = new StringBuilder();
            var selectedFilesList = selectedFiles.ToList();
            int totalXmlCommentLines = 0;
            int totalXmlCommentChars = 0;

            for (int i = 0; i < selectedFilesList.Count; i++)
            {
                var file = selectedFilesList[i];
                try
                {
                    contentBuilder.AppendLine($"=== {file.RelativePath} file contains:");
                    contentBuilder.AppendLine();

                    var lines = await File.ReadAllLinesAsync(file.FullPath);
                    bool shouldIgnoreXmlComments = filesToIgnoreXmlComments.Contains(file.RelativePath);

                    foreach (var line in lines)
                    {
                        string finalLine = line;
                        bool commentRemoved = false;

                        if (shouldIgnoreXmlComments)
                        {
                            var stats = IgnoreCommentsHelper.CalculateXmlCommentStats(line);

                            if (stats.IsXmlComment)
                            {
                                totalXmlCommentChars += stats.CommentLength;
                                totalXmlCommentLines++;

                                int commentIndex = line.Length - stats.CommentLength;
                                finalLine = line.Substring(0, commentIndex);
                                commentRemoved = true;
                            }
                        }

                        if (commentRemoved && string.IsNullOrWhiteSpace(finalLine))
                        {
                            continue;
                        }
                        else
                        {
                            contentBuilder.AppendLine(finalLine.TrimEnd());
                        }
                    }

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
            return (outputFilePath, totalXmlCommentLines, totalXmlCommentChars);
        }
    }
}