using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board.Client.Models
{
    public class CanvasHistory<T>
    {
        private Stack<T> HistoryStack { get; set; }
        protected List<T> HistoryList { get; set; }
        private Stack<T> RedoStack { get; set; }
        private int _capacity;
        public int Count => HistoryList?.Count ?? 0;
        public CanvasHistory(int capacity)
        {
            _capacity = capacity;
            HistoryStack = new Stack<T>(capacity);
            RedoStack = new Stack<T>(capacity);
            HistoryList = new List<T>();
        }
        public void Clear()
        {
            HistoryList.Clear();
            HistoryStack.Clear();
            RedoStack.Clear();
        }
        public T this[int index]
        {
            get => HistoryList[index];
            set => HistoryList.Insert(index, value);
        }
        public void Insert(T item)
        {
            RedoStack.Clear();
            Push(item);
        }
        public void Push(T item)
        {
            HistoryList.Add(item);
            if (HistoryList.Count > _capacity)
            {
                HistoryList.RemoveAt(0);
                HistoryStack = new Stack<T>(HistoryList);
            }
            else
            {
                HistoryStack.Push(item);
            }

        }
        public T Pop()
        {
            if (HistoryList.Count == 0) return default;
            HistoryList.RemoveAt(HistoryList.Count - 1);
            return HistoryStack.Pop();
        }
        public (bool, T) TryUndo()
        {
            if (HistoryList.Count < 2) return (false, default);
            HistoryList.RemoveAt(HistoryList.Count - 1);
            var pop = HistoryStack.Pop();
            RedoStack.Push(pop);
            return (true, HistoryStack.Peek());
        }
        public (bool, T) TryRedo()
        {
            if (RedoStack.Count == 0) return (false, default);
            var pop = RedoStack.Pop();
            Push(pop);
            return (true, pop);
        }
    }
}
