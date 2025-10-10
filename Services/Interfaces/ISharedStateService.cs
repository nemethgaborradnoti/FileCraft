using System.ComponentModel;

namespace FileCraft.Services.Interfaces
{
    public interface ISharedStateService : INotifyPropertyChanged
    {
        string SourcePath { get; set; }
        string DestinationPath { get; set; }
        List<string> IgnoredFolders { get; set; }
        bool IgnoreNormalComments { get; set; }
        bool IgnoreXmlComments { get; set; }
    }
}
