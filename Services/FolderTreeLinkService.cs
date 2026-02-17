using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;

namespace FileCraft.Services
{
    public class FolderTreeLinkService : IFolderTreeLinkService
    {
        private readonly Dictionary<string, FolderTreeManager> _registeredManagers = new();
        private List<List<string>> _linkGroups = new();
        private readonly Dictionary<string, ObservableCollection<FolderViewModel>> _sharedStates = new();

        public event Action? OnLinksChanged;

        public void RegisterManager(string id, FolderTreeManager manager)
        {
            _registeredManagers[id] = manager;
        }

        public void CreateLink(string managerId1, string managerId2)
        {
            var group1 = _linkGroups.FirstOrDefault(g => g.Contains(managerId1));
            var group2 = _linkGroups.FirstOrDefault(g => g.Contains(managerId2));

            if (group1 != null && group1 == group2) return;

            if (group1 == null && group2 == null)
            {
                var newGroup = new List<string> { managerId1, managerId2 };
                _linkGroups.Add(newGroup);
                EstablishSharedState(newGroup);
            }
            else if (group1 != null && group2 == null)
            {
                group1.Add(managerId2);
                UpdateGroupState(group1);
            }
            else if (group1 == null && group2 != null)
            {
                group2.Add(managerId1);
                UpdateGroupState(group2);
            }
            else
            {
                var mergedGroup = group1!.Union(group2!).ToList();
                _linkGroups.Remove(group1);
                _linkGroups.Remove(group2!);
                _linkGroups.Add(mergedGroup);
                EstablishSharedState(mergedGroup);
            }

            OnLinksChanged?.Invoke();
        }

        public void RemoveLink(string managerId)
        {
            var group = _linkGroups.FirstOrDefault(g => g.Contains(managerId));
            if (group == null) return;

            var leaderId = group.First();

            if (_registeredManagers.TryGetValue(managerId, out var managerToUnlink))
            {
                managerToUnlink.UnlinkAndCloneState();
            }

            group.Remove(managerId);

            if (group.Count < 2)
            {
                if (group.Any() && _registeredManagers.TryGetValue(group.First(), out var lastManager))
                {
                    lastManager.UnlinkAndCloneState();
                }
                _linkGroups.Remove(group);
                if (_sharedStates.ContainsKey(leaderId))
                {
                    _sharedStates.Remove(leaderId);
                }
            }
            else if (managerId == leaderId)
            {
                if (_sharedStates.ContainsKey(leaderId))
                {
                    _sharedStates.Remove(leaderId);
                }
                EstablishSharedState(group);
            }

            OnLinksChanged?.Invoke();
        }

        public List<List<string>> GetLinkGroups()
        {
            return _linkGroups.Select(group => new List<string>(group)).ToList();
        }

        public void LoadLinkGroups(List<List<string>> groups)
        {
            _linkGroups = groups.Select(g => g.ToList()).ToList();
            _sharedStates.Clear();

            foreach (var group in _linkGroups)
            {
                if (group.Any())
                {
                    EstablishSharedState(group);
                }
            }
            OnLinksChanged?.Invoke();
        }

        public List<string> GetLinkedPeers(string managerId)
        {
            var group = _linkGroups.FirstOrDefault(g => g.Contains(managerId));
            if (group == null)
            {
                return new List<string>();
            }
            return group.Where(id => id != managerId).ToList();
        }

        private void EstablishSharedState(List<string> group)
        {
            if (!group.Any() || !_registeredManagers.ContainsKey(group.First())) return;

            var leaderId = group.First();
            var leaderManager = _registeredManagers[leaderId];
            _sharedStates[leaderId] = leaderManager.RootFolders;

            UpdateGroupState(group);
        }

        private void UpdateGroupState(List<string> group)
        {
            if (!group.Any()) return;
            var leaderId = group.First();

            if (!_sharedStates.ContainsKey(leaderId))
            {
                EstablishSharedState(group);
                return;
            }

            var sharedRoot = _sharedStates[leaderId];
            foreach (var memberId in group)
            {
                if (_registeredManagers.TryGetValue(memberId, out var memberManager))
                {
                    memberManager.SetSharedRootFolders(sharedRoot);
                }
            }
        }
    }
}