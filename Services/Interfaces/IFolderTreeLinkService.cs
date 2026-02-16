using FileCraft.ViewModels.Shared;

namespace FileCraft.Services.Interfaces
{
    public interface IFolderTreeLinkService
    {
        event Action OnLinksChanged;
        void RegisterManager(string id, FolderTreeManager manager);
        void CreateLink(string managerId1, string managerId2);
        void RemoveLink(string managerId);
        List<List<string>> GetLinkGroups();
        void LoadLinkGroups(List<List<string>> groups);
        List<string> GetLinkedPeers(string managerId);
    }
}