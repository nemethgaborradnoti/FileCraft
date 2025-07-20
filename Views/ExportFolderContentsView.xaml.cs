using FileCraft.Services;
using FileCraft.ViewModels;
using System.IO;
using System.Text;
using System.Windows;

namespace FileCraft.Views
{
    public partial class ExportFolderContentsView : System.Windows.Controls.UserControl
    {
        private readonly IDialogService _dialogService;

        public ExportFolderContentsView()
        {
            InitializeComponent();
            _dialogService = new DialogService();
        }


        private void ExportButton_Click(object sender, RoutedEventArgs e)
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
                var searchOption = includeSubfoldersCheckBox.IsChecked == true
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var files = Directory.GetFiles(sourceDirectory, "*.*", searchOption);

                if (files.Length == 0)
                {
                    _dialogService.ShowNotification("Information", "The selected folder (and its subfolders, if checked) contains no files to export.");
                    return;
                }

                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("Name;Size (KB);Modification Date;Creation Date;Last Access Date;Format");

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    string name = fileInfo.Name;
                    string sizeKb = (fileInfo.Length / 1024.0).ToString("F2");
                    string modificationDate = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                    string creationDate = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                    string lastAccessDate = fileInfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss");
                    string format = fileInfo.Extension;
                    csvBuilder.AppendLine($"{name};{sizeKb};{modificationDate};{creationDate};{lastAccessDate};{format}");
                }

                string outputFileName = $"ExportedContents_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string outputFilePath = Path.Combine(destinationDirectory, outputFileName);

                File.WriteAllText(outputFilePath, csvBuilder.ToString());

                _dialogService.ShowNotification("Success", $"Folder contents exported successfully!\n\n{files.Length} files were processed.\nSaved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred during export:\n\n{ex.Message}");
            }
        }
    }
}
