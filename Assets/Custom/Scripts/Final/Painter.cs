using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Painter : MonoBehaviour
{
    public static Painter Instance { get; private set; }

    public Color BrushColor /*{ get; private set; }*/ = Color.red;
    public int BrushSize /*{ get; private set; }*/ = 5;

    private Dictionary<string,Stack<Color[]>> undoStack = new();
    public Dictionary<string, Stack<Color[]>> UndoStack => undoStack;

    private Dictionary<string, Stack<Color[]>> redoStack = new();
    public Dictionary<string, Stack<Color[]>> RedoStack => redoStack;

    private void Awake()
    {
        Instance = this;
    }

    public void Undo(Texture2D paintTexture, string clientId)
    {
        if (UndoStack[clientId].Count <= 0) return;
        if (!RedoStack.ContainsKey(clientId))
        {
            RedoStack.Add(clientId, new Stack<Color[]>());
        }
        SaveTextureState(RedoStack[clientId], paintTexture);
        paintTexture.SetPixels(UndoStack[clientId].Pop());
        paintTexture.Apply();
    }

    public void Redo(Texture2D paintTexture, string clientId)
    {
        if (RedoStack[clientId].Count <= 0) return;
        SaveTextureState(UndoStack[clientId], paintTexture);
        paintTexture.SetPixels(RedoStack[clientId].Pop());
        paintTexture.Apply();
    }

    public void SaveTextureState(Texture2D paintTexture, string clientId)
    {
        if (!UndoStack.ContainsKey(clientId))
        {
            UndoStack.Add(clientId, new Stack<Color[]>());
        }
        if (UndoStack[clientId].Count == 5)
        {
            TrimStack(clientId);
        }
        SaveTextureState(UndoStack[clientId], paintTexture);
    }

    public void SaveTextureState(Stack<Color[]> stack, Texture2D paintTexture)
    {
        stack.Push(paintTexture.GetPixels());
    }

    private void TrimStack(string clientId)
    {
        var tempList = new List<Color[]>(UndoStack[clientId]); // Convert to a list
        tempList.RemoveAt(tempList.Count - 1); // Remove the oldest element
        tempList.Reverse();
        UndoStack[clientId] = new Stack<Color[]>(tempList); // Recreate the stack
    }

    public void SetBrushColor(Color newColor)
    {
        BrushColor = newColor;
    }

    public void SetThickness(int newThickness)
    {
        BrushSize = newThickness;
    }

}
