using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileCraft.Services
{
    public class UndoService : IUndoService
    {
        private readonly List<SaveData> _undoStack = new List<SaveData>();
        private readonly Stack<SaveData> _redoStack = new Stack<SaveData>();
        private const int MaxHistorySize = 10;

        public bool CanUndo => _undoStack.Any();
        public bool CanRedo => _redoStack.Any();

        public event Action? HistoryChanged;

        public void RecordState(SaveData state)
        {
            if (_undoStack.Count >= MaxHistorySize)
            {
                _undoStack.RemoveAt(0);
            }
            _undoStack.Add(state);
            _redoStack.Clear();
            HistoryChanged?.Invoke();
        }

        public SaveData Undo(SaveData currentState)
        {
            if (!CanUndo) throw new InvalidOperationException("Nothing to undo.");

            _redoStack.Push(currentState);
            var previousState = _undoStack.Last();
            _undoStack.RemoveAt(_undoStack.Count - 1);

            HistoryChanged?.Invoke();
            return previousState;
        }

        public SaveData Redo(SaveData currentState)
        {
            if (!CanRedo) throw new InvalidOperationException("Nothing to redo.");

            _undoStack.Add(currentState);
            var nextState = _redoStack.Pop();

            HistoryChanged?.Invoke();
            return nextState;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            HistoryChanged?.Invoke();
        }
    }
}
