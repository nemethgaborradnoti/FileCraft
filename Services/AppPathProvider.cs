using FileCraft.Services.Interfaces;
using System;
using System.IO;

namespace FileCraft.Services
{
    public class AppPathProvider : IAppPathProvider
    {
        private readonly string _appDirectory;
        private readonly string _backupDirectory;

        public AppPathProvider()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _appDirectory = Path.Combine(appData, "FileCraft");
            _backupDirectory = Path.Combine(_appDirectory, "Backups");

            Directory.CreateDirectory(_appDirectory);
            Directory.CreateDirectory(_backupDirectory);
        }

        public string GetAppDirectory()
        {
            return _appDirectory;
        }

        public string GetBackupDirectory()
        {
            return _backupDirectory;
        }
    }
}