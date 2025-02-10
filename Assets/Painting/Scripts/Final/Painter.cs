using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class Painter : MonoBehaviour
{
    public static Painter Instance { get; private set; }

    public Color BrushColor /*{ get; private set; }*/ = Color.red;
    public int BrushSize /*{ get; private set; }*/ = 5;

    private Dictionary<int,Stack<Color[]>> undoStack = new();
    public Dictionary<int, Stack<Color[]>> UndoStack => undoStack;

    private Dictionary<int, Stack<Color[]>> redoStack = new();
    public Dictionary<int, Stack<Color[]>> RedoStack => redoStack;

    //private PhotonView _photonView;
    private int _clientId = 1;//=> _photonView.ViewID;

    private void Awake()
    {
        Instance = this;
        //_photonView = GetComponent<PhotonView>();
    }

    public void DrawLine(string name, Vector2 value, Vector2 currentUV, int brushSize, Color color)
    {
        string hex = ColorToHex(color);
        //_photonView.RPC("DrawLineRpc", RpcTarget.All, name, value, currentUV, brushSize, hex, _clientId);
    }

    [PunRPC]
    private void DrawLineRpc(string name, Vector2 startUV, Vector2 currentUV, int brushSize, string hex, int id)
    {
        BrushSize = brushSize;
        BrushColor = HexToColor(hex);
        PaintableTexture paintable = GameObject.Find(name).GetComponent<PaintableTexture>(); // Find the paintable object
        if (paintable != null)
        {

            paintable.DrawLine(startUV, currentUV);
        }
    }

    public void Paint(string name, Vector2 currentUV, int brushSize, Color color)
    {
        string hex = ColorToHex(color);
        //_photonView.RPC("PaintRpc", RpcTarget.All, name, currentUV, brushSize, hex, _clientId);
    }

    [PunRPC]
    private void PaintRpc(string name, Vector2 currentUV, int brushSize, string hex, int id)
    {
        BrushColor = HexToColor(hex);
        BrushSize = brushSize;
        PaintableTexture paintable = GameObject.Find(name).GetComponent<PaintableTexture>(); // Find the paintable object
        if (paintable != null)
        {
            paintable.Paint(currentUV);
        }
    }

    public void Undo(Texture2D paintTexture, string name)
    {
        //_photonView.RPC("UndoRpc", RpcTarget.All, name, _clientId);
        OnUndo(paintTexture, _clientId);
    }

    [PunRPC]
    private void UndoRpc(string name, int id)
    {
        Texture2D texture = GameObject.Find(name).GetComponent<PaintableTexture>().PaintTexture;
        OnUndo(texture, id);
    }

    private void OnUndo(Texture2D paintTexture, int clientId)
    {
        if (UndoStack[clientId].Count <= 0) return;
        Debug.Log($"Client ID: {clientId}");
        if (!RedoStack.ContainsKey(clientId))
        {
            RedoStack.Add(clientId, new Stack<Color[]>());
        }
        RedoStack[_clientId].Push(paintTexture.GetPixels());
        //SaveTextureState(RedoStack[clientId], paintTexture);
        paintTexture.SetPixels(UndoStack[clientId].Pop());
        paintTexture.Apply();
    }

    public void Redo(Texture2D paintTexture, string name)
    {
        //_photonView.RPC("RedoRpc", RpcTarget.All, name, _clientId);
        OnRedo(paintTexture, _clientId);
    }

    [PunRPC]
    private void RedoRpc(string name, int id)
    {
        Texture2D texture = GameObject.Find(name).GetComponent<PaintableTexture>().PaintTexture;
        Painter.Instance.OnRedo(texture, id);
    }

    private void OnRedo(Texture2D paintTexture, int clientId)
    {
        if (RedoStack[clientId].Count <= 0) return;
        Debug.Log($"Client ID: {clientId}");
        UndoStack[_clientId].Push(paintTexture.GetPixels());
        //SaveTextureState(UndoStack[clientId], paintTexture);
        paintTexture.SetPixels(RedoStack[clientId].Pop());
        paintTexture.Apply();
    }

    public void SaveTextureState(Texture2D paintTexture, string name)
    {
        if (!UndoStack.ContainsKey(_clientId))
        {
            UndoStack.Add(_clientId, new Stack<Color[]>());
        }
        if (UndoStack[_clientId].Count == 5)
        {
            TrimStack(_clientId);
        }
        UndoStack[_clientId].Push(paintTexture.GetPixels());

        //SaveTextureState(UndoStack[_clientId], paintTexture);

        //paintTexture.ToString();

        //var hexArray = ColorsToHexArray(paintTexture.GetPixels());
        //_photonView.RPC("SaveTextureState", RpcTarget.Others, paintTexture.ToString(), name, _clientId);
    }

    [PunRPC]
    private void SaveTextureState(string action, string name, int id)
    {
        //var colors = HexArrayToColors(action);

        //Texture2D texture = action as Texture2D;

        //UndoStack[id].Push(((Texture2D)action).GetPixels());
    }

    private void TrimStack(int clientId)
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

    #region Utilities Methods

    string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}"; // Converts to hex format like "#FF5733FF"
    }

    Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return Color.white; // Default fallback
    }

    string[] ColorsToHexArray(Color[] colors)
    {
        string[] hexArray = new string[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            hexArray[i] = $"#{ColorUtility.ToHtmlStringRGBA(colors[i])}"; // Convert to Hex
        }
        return hexArray;
    }

    Color[] HexArrayToColors(string[] hexArray)
    {
        Color[] colors = new Color[hexArray.Length];
        for (int i = 0; i < hexArray.Length; i++)
        {
            if (!ColorUtility.TryParseHtmlString(hexArray[i], out colors[i]))
            {
                colors[i] = Color.clear; // Default fallback
            }
        }
        return colors;
    }

    #endregion
}
