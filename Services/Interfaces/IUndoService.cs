using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IUndoService
    {
        bool CanUndo { get; }
        bool CanRedo { get; }
        event Action HistoryChanged;
        void RecordState(SaveData state);
        SaveData Undo(SaveData currentState);
        SaveData Redo(SaveData currentState);
        void Clear();
    }
}
