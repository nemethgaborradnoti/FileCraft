using LiteDB;

namespace FileCraft.Models
{
    public class DatabaseMetadata
    {
        [BsonId]
        public int Id { get; set; }
        public string LastAppVersion { get; set; } = string.Empty;
        public DateTime LastAccess { get; set; }
    }
}