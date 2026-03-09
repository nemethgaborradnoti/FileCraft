using LiteDB;

namespace FileCraft.Services.Interfaces
{
    public interface IDatabaseService : IDisposable
    {
        void Initialize();
        ILiteCollection<T> GetCollection<T>(string name);
        void CreateBackup(string label);
    }
}