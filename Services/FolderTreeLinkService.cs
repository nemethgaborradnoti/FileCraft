using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileCraft.Services
{
    public class FolderTreeLinkService : IFolderTreeLinkService
    {
        private readonly Dictionary<string, FolderTreeManager> _registeredManagers = new();
        private List<List<string>> _linkGroups = new();
        private readonly Dictionary<string, ObservableCollection<FolderViewModel>> _sharedStates = new();

        private bool _isPropagating = false;

        public event Action? OnLinksChanged;

        public void RegisterManager(string id, FolderTreeManager manager)
        {
            if (_registeredManagers.ContainsKey(id))
            {
                _registeredManagers[id].PropertyChanged -= OnManagerPropertyChanged;
            }

            _registeredManagers[id] = manager;
            manager.PropertyChanged += OnManagerPropertyChanged;
        }

        private void OnManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isPropagating) return;

            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                if (sender is FolderTreeManager manager)
                {
                    PropagateRootChange(manager);
                }
            }
        }

        private void PropagateRootChange(FolderTreeManager sourceManager)
        {
            var group = _linkGroups.FirstOrDefault(g => g.Contains(sourceManager.Id));
            if (group == null || !group.Any()) return;

            _isPropagating = true;
            try
            {
                var leaderId = group.First();
                _sharedStates[leaderId] = sourceManager.RootFolders;

                foreach (var memberId in group)
                {
                    if (memberId == sourceManager.Id) continue;

                    if (_registeredManagers.TryGetValue(memberId, out var peerManager))
                    {
                        peerManager.SetSharedRootFolders(sourceManager.RootFolders, sourceManager.CurrentSourcePath);
                    }
                }
            }
            finally
            {
                _isPropagating = false;
            }
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
                var newLeaderId = group.First();
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
            var leaderManager = _registeredManagers[leaderId];
            var sharedPath = leaderManager.CurrentSourcePath;

            foreach (var memberId in group)
            {
                if (memberId == leaderId) continue;

                if (_registeredManagers.TryGetValue(memberId, out var memberManager))
                {
                    memberManager.SetSharedRootFolders(sharedRoot, sharedPath);
                }
            }
        }
    }
}