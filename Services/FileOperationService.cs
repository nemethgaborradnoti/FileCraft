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

            var ignoredFolders = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);
            var allFiles = _fileQueryService.GetAllFiles(includedFolderPaths, ignoredFolders).OrderBy(f => f.FullName).ToList();

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

        public async Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolderPaths, string outputFileName, TreeGenerationMode mode)
        {
            Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");
            Guard.AgainstNull(excludedFolderPaths, nameof(excludedFolderPaths));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

            StringBuilder contentBuilder = new StringBuilder();

            if (mode == TreeGenerationMode.Structured)
            {
                contentBuilder.AppendLine(new DirectoryInfo(sourcePath).Name);
                BuildTree(sourcePath, "", contentBuilder, excludedFolderPaths, true);
            }
            else if (mode == TreeGenerationMode.PathsOnly)
            {
                BuildPathsOnly(sourcePath, sourcePath, contentBuilder, excludedFolderPaths);
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, contentBuilder.ToString());

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

        private void BuildPathsOnly(string directoryPath, string rootPath, StringBuilder builder, ISet<string> excludedFolderPaths)
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

                foreach (var dir in subDirectories)
                {
                    builder.AppendLine(Path.GetRelativePath(rootPath, dir));
                    BuildPathsOnly(dir, rootPath, builder, excludedFolderPaths);
                }

                foreach (var file in files)
                {
                    builder.AppendLine(Path.GetRelativePath(rootPath, file));
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public async Task<FileExportResult> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName, ISet<string> filesToIgnoreXmlComments)
        {
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));
            Guard.AgainstNullOrEmpty(selectedFiles, nameof(selectedFiles), "No files were selected for export.");

            var contentBuilder = new StringBuilder();
            var selectedFilesList = selectedFiles.ToList();

            var result = new FileExportResult();
            long exportedLines = 0;

            for (int i = 0; i < selectedFilesList.Count; i++)
            {
                var file = selectedFilesList[i];
                try
                {
                    contentBuilder.AppendLine($"=== {file.RelativePath} file contains:");
                    exportedLines++;
                    contentBuilder.AppendLine();
                    exportedLines++;

                    var lines = await File.ReadAllLinesAsync(file.FullPath);
                    bool shouldIgnoreXmlComments = filesToIgnoreXmlComments.Contains(file.RelativePath);

                    if (shouldIgnoreXmlComments)
                    {
                        result.FilesWithIgnoredCommentsCount++;
                    }

                    foreach (var line in lines)
                    {
                        string finalLine = line;
                        bool commentRemoved = false;

                        if (shouldIgnoreXmlComments)
                        {
                            var stats = IgnoreCommentsHelper.CalculateXmlCommentStats(line);

                            if (stats.IsXmlComment)
                            {
                                result.IgnoredCharacters += stats.CommentLength;
                                result.IgnoredLines++;

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
                            exportedLines++;
                        }
                    }

                    if (i < selectedFilesList.Count - 1)
                    {
                        contentBuilder.AppendLine();
                        exportedLines++;
                        contentBuilder.AppendLine("===========");
                        exportedLines++;
                    }
                }
                catch (Exception ex)
                {
                    contentBuilder.AppendLine($"--- Could not read file: {file.FileName}. Error: {ex.Message} ---");
                    exportedLines++;
                    contentBuilder.AppendLine();
                    exportedLines++;
                }
            }

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            await File.WriteAllTextAsync(outputFilePath, contentBuilder.ToString());

            result.OutputFilePath = outputFilePath;
            result.ExportedCharacters = contentBuilder.Length;
            result.ExportedLines = exportedLines;

            return result;
        }
    }
}