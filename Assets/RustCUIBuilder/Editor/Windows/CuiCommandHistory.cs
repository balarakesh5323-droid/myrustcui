using System;
using System.Collections.Generic;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Editor.Windows
{
    public interface ICuiCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }

    public class DocumentSnapshotCommand : ICuiCommand
    {
        public string Description { get; }
        private readonly CuiDocument _doc;
        private readonly CuiDocument _beforeState;
        private readonly CuiDocument _afterState;

        public DocumentSnapshotCommand(string description, CuiDocument doc, CuiDocument beforeState, CuiDocument afterState)
        {
            Description = description;
            _doc = doc;
            _beforeState = beforeState;
            _afterState = afterState;
        }

        public void Execute()
        {
            ApplyState(_afterState);
        }

        public void Undo()
        {
            ApplyState(_beforeState);
        }

        private void ApplyState(CuiDocument state)
        {
            _doc.Elements.Clear();
            foreach (var e in state.Elements)
            {
                _doc.Elements.Add(e.Clone(false, e.Name));
            }
            _doc.NotifyModified();
        }
    }

    /// <summary>
    /// Transactional Undo/Redo history stack for Rust CUI Builder.
    /// </summary>
    public class CuiCommandHistory
    {
        private readonly Stack<ICuiCommand> _undoStack = new Stack<ICuiCommand>();
        private readonly Stack<ICuiCommand> _redoStack = new Stack<ICuiCommand>();
        private const int MaxHistory = 50;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event Action OnHistoryChanged;

        public void Record(ICuiCommand command)
        {
            if (command == null) return;
            _undoStack.Push(command);
            _redoStack.Clear();

            if (_undoStack.Count > MaxHistory)
            {
                var temp = new List<ICuiCommand>(_undoStack);
                temp.RemoveAt(temp.Count - 1);
                _undoStack.Clear();
                for (int i = temp.Count - 1; i >= 0; i--) _undoStack.Push(temp[i]);
            }

            OnHistoryChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
            OnHistoryChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
            OnHistoryChanged?.Invoke();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
