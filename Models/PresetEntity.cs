using LiteDB;

namespace FileCraft.Models
{
    public class PresetEntity
    {
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public string AppVersion { get; set; } = string.Empty;
        public PresetStatistics Statistics { get; set; } = new();
        public SaveData Data { get; set; } = new();
    }
}