using FileCraft.Models;
using FileCraft.Services.Interfaces;
using LiteDB;
using System;
using System.IO;
using System.Reflection;

namespace FileCraft.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private LiteDatabase? _database;
        private readonly string _currentVersion;

        public DatabaseService(IAppPathProvider appPathProvider)
        {
            string appFolder = appPathProvider.GetAppDirectory();
            _backupDirectory = appPathProvider.GetBackupDirectory();
            _databasePath = Path.Combine(appFolder, "FileCraft.db");

            var assembly = Assembly.GetEntryAssembly();
            _currentVersion = assembly?.GetName().Version?.ToString() ?? "1.0.0";
        }

        public void Initialize()
        {
            if (File.Exists(_databasePath))
            {
                CheckAndPerformBackupIfNeeded();
            }

            _database = new LiteDatabase($"Filename={_databasePath};Connection=Shared");
            UpdateMetadata();
        }

        public ILiteCollection<T> GetCollection<T>(string name)
        {
            if (_database == null)
            {
                Initialize();
            }
            return _database!.GetCollection<T>(name);
        }

        public void CreateBackup(string label)
        {
            if (!File.Exists(_databasePath)) return;

            if (_database != null)
            {
                _database.Dispose();
                _database = null;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupName = $"FileCraft_{label}_{timestamp}.db";
            string backupPath = Path.Combine(_backupDirectory, backupName);

            File.Copy(_databasePath, backupPath, true);

            Initialize();
        }

        public void Dispose()
        {
            _database?.Dispose();
        }

        private void CheckAndPerformBackupIfNeeded()
        {
            try
            {
                string storedVersion = "0.0.0";
                using (var tempDb = new LiteDatabase($"Filename={_databasePath};ReadOnly=true"))
                {
                    var metaCollection = tempDb.GetCollection<DatabaseMetadata>("Metadata");
                    var metadata = metaCollection.FindOne(Query.All());
                    if (metadata != null)
                    {
                        storedVersion = metadata.LastAppVersion;
                    }
                }

                if (storedVersion != _currentVersion)
                {
                    string backupName = $"AutoBackup_v{storedVersion}_to_v{_currentVersion}_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                    string backupPath = Path.Combine(_backupDirectory, backupName);
                    File.Copy(_databasePath, backupPath, true);
                }
            }
            catch
            {
            }
        }

        private void UpdateMetadata()
        {
            if (_database == null) return;

            var metaCollection = _database.GetCollection<DatabaseMetadata>("Metadata");
            var metadata = metaCollection.FindOne(Query.All());

            if (metadata == null)
            {
                metadata = new DatabaseMetadata
                {
                    LastAppVersion = _currentVersion,
                    LastAccess = DateTime.Now
                };
                metaCollection.Insert(metadata);
            }
            else
            {
                metadata.LastAppVersion = _currentVersion;
                metadata.LastAccess = DateTime.Now;
                metaCollection.Update(metadata);
            }
        }
    }
}