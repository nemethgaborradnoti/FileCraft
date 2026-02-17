using FileCraft.Models;
using FileCraft.Services.Interfaces;
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

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");

            using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                await writer.WriteLineAsync(string.Join(";", selectedColumns));

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
                    await writer.WriteLineAsync(string.Join(";", lineParts));
                }
            }

            return outputFilePath;
        }

        public async Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolderPaths, string outputFileName, TreeGenerationMode mode)
        {
            Guard.AgainstNullOrWhiteSpace(sourcePath, nameof(sourcePath));
            Guard.AgainstNullOrWhiteSpace(destinationPath, nameof(destinationPath));
            Guard.AgainstNonExistentDirectory(sourcePath, "The selected source folder does not exist.");
            Guard.AgainstNull(excludedFolderPaths, nameof(excludedFolderPaths));
            Guard.AgainstNullOrWhiteSpace(outputFileName, nameof(outputFileName));

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");

            using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                if (mode == TreeGenerationMode.Structured)
                {
                    await writer.WriteLineAsync(new DirectoryInfo(sourcePath).Name);
                    await BuildTreeAsync(sourcePath, "", writer, excludedFolderPaths, true);
                }
                else if (mode == TreeGenerationMode.PathsOnly)
                {
                    await BuildPathsOnlyAsync(sourcePath, sourcePath, writer, excludedFolderPaths);
                }
            }

            return outputFilePath;
        }

        private async Task BuildTreeAsync(string directoryPath, string indent, StreamWriter writer, ISet<string> excludedFolderPaths, bool isLastParent)
        {
            try
            {
                var ignoredFolderNames = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);

                var subDirectories = Directory.EnumerateDirectories(directoryPath)
                    .Where(d => !excludedFolderPaths.Contains(d))
                    .Where(d => !ignoredFolderNames.Contains(new DirectoryInfo(d).Name))
                    .OrderBy(d => d)
                    .ToArray();

                var files = Directory.EnumerateFiles(directoryPath)
                    .OrderBy(f => f)
                    .ToArray();

                for (int i = 0; i < subDirectories.Length; i++)
                {
                    var subDirInfo = new DirectoryInfo(subDirectories[i]);
                    bool isLast = (i == subDirectories.Length - 1) && (files.Length == 0);
                    await writer.WriteLineAsync($"{indent}{(isLast ? "└── " : "├── ")}{subDirInfo.Name}");
                    await BuildTreeAsync(subDirInfo.FullName, indent + (isLast ? "    " : "│   "), writer, excludedFolderPaths, isLast);
                }

                for (int i = 0; i < files.Length; i++)
                {
                    var fileInfo = new FileInfo(files[i]);
                    bool isLast = i == files.Length - 1;
                    await writer.WriteLineAsync($"{indent}{(isLast ? "└── " : "├── ")}{fileInfo.Name}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                await writer.WriteLineAsync($"{indent}└── [Access Denied]");
            }
        }

        private async Task BuildPathsOnlyAsync(string directoryPath, string rootPath, StreamWriter writer, ISet<string> excludedFolderPaths)
        {
            try
            {
                var ignoredFolderNames = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);

                var subDirectories = Directory.EnumerateDirectories(directoryPath)
                    .Where(d => !excludedFolderPaths.Contains(d))
                    .Where(d => !ignoredFolderNames.Contains(new DirectoryInfo(d).Name))
                    .OrderBy(d => d);

                var files = Directory.EnumerateFiles(directoryPath)
                    .OrderBy(f => f);

                foreach (var dir in subDirectories)
                {
                    await writer.WriteLineAsync(Path.GetRelativePath(rootPath, dir));
                    await BuildPathsOnlyAsync(dir, rootPath, writer, excludedFolderPaths);
                }

                foreach (var file in files)
                {
                    await writer.WriteLineAsync(Path.GetRelativePath(rootPath, file));
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

            string outputFilePath = Path.Combine(destinationPath, $"{outputFileName}.txt");
            var result = new FileExportResult();
            var selectedFilesList = selectedFiles.ToList();
            int newLineLength = Environment.NewLine.Length;

            using (var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                for (int i = 0; i < selectedFilesList.Count; i++)
                {
                    var file = selectedFilesList[i];
                    try
                    {
                        string header = $"=== {file.RelativePath} file contains:";
                        await writer.WriteLineAsync(header);
                        result.ExportedLines++;
                        result.ExportedCharacters += header.Length + newLineLength;

                        await writer.WriteLineAsync();
                        result.ExportedLines++;
                        result.ExportedCharacters += newLineLength;

                        bool shouldIgnoreXmlComments = filesToIgnoreXmlComments.Contains(file.RelativePath);

                        if (shouldIgnoreXmlComments)
                        {
                            result.FilesWithIgnoredCommentsCount++;
                        }

                        foreach (var line in File.ReadLines(file.FullPath))
                        {
                            ReadOnlyMemory<char> lineMemory = line.AsMemory();
                            bool commentRemoved = false;

                            if (shouldIgnoreXmlComments)
                            {
                                var stats = FileCraft.Shared.Helpers.IgnoreCommentsHelper.CalculateXmlCommentStats(lineMemory.Span);

                                if (stats.IsXmlComment)
                                {
                                    result.IgnoredCharacters += stats.CommentLength;
                                    result.IgnoredLines++;

                                    int commentIndex = lineMemory.Length - stats.CommentLength;
                                    lineMemory = lineMemory.Slice(0, commentIndex);
                                    commentRemoved = true;
                                }
                            }

                            if (commentRemoved)
                            {
                                var span = lineMemory.Span;
                                int len = span.Length;
                                while (len > 0 && char.IsWhiteSpace(span[len - 1]))
                                {
                                    len--;
                                }
                                lineMemory = lineMemory.Slice(0, len);
                            }

                            if (commentRemoved && lineMemory.Length == 0)
                            {
                                continue;
                            }
                            else
                            {
                                if (commentRemoved)
                                {
                                    await writer.WriteLineAsync(lineMemory);
                                    result.ExportedLines++;
                                    result.ExportedCharacters += lineMemory.Length + newLineLength;
                                }
                                else
                                {
                                    var trimmedLine = line.TrimEnd();
                                    await writer.WriteLineAsync(trimmedLine);
                                    result.ExportedLines++;
                                    result.ExportedCharacters += trimmedLine.Length + newLineLength;
                                }
                            }
                        }

                        if (i < selectedFilesList.Count - 1)
                        {
                            await writer.WriteLineAsync();
                            result.ExportedLines++;
                            result.ExportedCharacters += newLineLength;

                            string footer = "===========";
                            await writer.WriteLineAsync(footer);
                            result.ExportedLines++;
                            result.ExportedCharacters += footer.Length + newLineLength;
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"--- Could not read file: {file.FileName}. Error: {ex.Message} ---";
                        await writer.WriteLineAsync(errorMsg);
                        result.ExportedLines++;
                        result.ExportedCharacters += errorMsg.Length + newLineLength;

                        await writer.WriteLineAsync();
                        result.ExportedLines++;
                        result.ExportedCharacters += newLineLength;
                    }
                }
            }

            result.OutputFilePath = outputFilePath;
            return result;
        }
    }
}