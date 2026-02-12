using System;
using System.Collections.Generic;

public class PriorityQueue<T> where T : IComparable<T>
{
    private List<T> heap = new List<T>();

    public void Enqueue(T item)
    {
        heap.Add(item);
        int i = heap.Count - 1;

        // Bubble up
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].CompareTo(heap[i]) <= 0) break;

            T temp = heap[parent];
            heap[parent] = heap[i];
            heap[i] = temp;
            i = parent;
        }
    }

    public T Dequeue()
    {
        if (heap.Count == 0) throw new System.Exception("Queue is empty");

        T result = heap[0];
        heap.RemoveAt(0);

        // Bubble down
        int i = 0;
        while (true)
        {
            int smallest = i;
            int left = 2 * i + 1;
            int right = 2 * i + 2;

            if (left < heap.Count && heap[left].CompareTo(heap[smallest]) < 0)
                smallest = left;

            if (right < heap.Count && heap[right].CompareTo(heap[smallest]) < 0)
                smallest = right;

            if (smallest == i) break;

            T temp = heap[i];
            heap[i] = heap[smallest];
            heap[smallest] = temp;
            i = smallest;
        }

        return result;
    }

    public T Peek()
    {
        if (heap.Count == 0) throw new System.Exception("Queue is empty");
        return heap[0];
    }

    public bool Contains(T item)
    {
        return heap.Contains(item);
    }

    public int Count
    {
        get { return heap.Count; }
    }

    public bool IsEmpty()
    {
        return heap.Count == 0;
    }

    public void Clear()
    {
        heap.Clear();
    }
}
