using System.Collections.Generic;

namespace JungleMaze.LevelEditor
{
    /// <summary>
    /// Undo/Redo bằng ảnh chụp JSON của cả map: đơn giản, không lệch trạng thái, đủ nhanh với vài trăm vật thể.
    /// Gọi <see cref="Record"/> ngay TRƯỚC mỗi thay đổi.
    /// </summary>
    public sealed class LevelHistory
    {
        private readonly int _capacity;
        private readonly LinkedList<string> _undo = new LinkedList<string>();
        private readonly Stack<string> _redo = new Stack<string>();

        public LevelHistory(int capacity = 200) => _capacity = capacity;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Record(string snapshot)
        {
            if (_undo.Last != null && _undo.Last.Value == snapshot) return;
            _undo.AddLast(snapshot);
            if (_undo.Count > _capacity) _undo.RemoveFirst();
            _redo.Clear();
        }

        public bool TryUndo(string current, out string previous)
        {
            previous = null;
            if (_undo.Count == 0) return false;
            previous = _undo.Last.Value;
            _undo.RemoveLast();
            _redo.Push(current);
            return true;
        }

        public bool TryRedo(string current, out string next)
        {
            next = null;
            if (_redo.Count == 0) return false;
            next = _redo.Pop();
            _undo.AddLast(current);
            return true;
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }
    }
}
