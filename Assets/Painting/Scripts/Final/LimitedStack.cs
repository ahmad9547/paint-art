using System.Collections.Generic;
using UnityEngine;

public class LimitedStack<T>
{
    private Stack<T> stack;   // Main stack to store items
    private int maxSize;      // Maximum size of the stack

    // Constructor to initialize the stack with a maximum size
    public LimitedStack(int maxSize)
    {
        if (maxSize <= 0)
        {
            Debug.LogError("Max size must be greater than 0.");
        }

        this.maxSize = maxSize;
        stack = new Stack<T>();
    }

    public List<T> ToList()
    {
        return new List<T>(stack);
    }

    // Push method to add an item to the stack
    public void Push(T item)
    {
        if (stack.Count >= maxSize)
        {
            RemoveOldestElement();
        }
        stack.Push(item);
    }

    // Pop method to retrieve and remove the most recent item
    public T Pop()
    {
        if (stack.Count == 0)
        {
            Debug.LogError("Stack is empty.");
        }

        return stack.Pop();
    }

    // Peek method to view the most recent item without removing it
    public T Peek()
    {
        if (stack.Count == 0)
        {
            Debug.LogError("Stack is empty.");
        }

        return stack.Peek();
    }

    // Method to get the current stack count
    public int Count => stack.Count;

    // Private helper method to remove the oldest item
    private void RemoveOldestElement()
    {
        var tempList = new List<T>(stack); // Convert to a list
        tempList.RemoveAt(tempList.Count - 1); // Remove the oldest element
        stack = new Stack<T>(tempList); // Recreate the stack
    }

    // Method to print all stack items
    public void PrintStack()
    {
        Debug.Log($"Current stack: {string.Join(", ", stack)}");
    }

    public void Clear()
    {
        stack.Clear();
    }
}
