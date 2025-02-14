using CodeMonkey.Utils;
using System.Collections.Generic;
using UnityEngine;

public class Painter : MonoBehaviour
{
    public static Painter Instance { get; private set; }

    #region Brush Properties
    public Color BrushColor = Color.red;
    public int BrushSize = 5;
    #endregion

    #region Undo/Redo System
    private Dictionary<int, Stack<Color[]>> undoStack = new();
    public Dictionary<int, Stack<Color[]>> UndoStack => undoStack;

    private Dictionary<int, Stack<Color[]>> redoStack = new();
    public Dictionary<int, Stack<Color[]>> RedoStack => redoStack;
    private Stack<GameObject> _paintedUndoObjects = new();
    private Stack<GameObject> _paintedRedoObjects = new();
    #endregion

    #region Private Fields
    private int _clientId => 1;
    public GameObject LastPaintedObject { get; set; }
    private bool _isDrawing = false;
    private bool _ctrl = true;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        HandleInput();
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        if (UtilsClass.IsPointerOverUI()) return;

        if (!_isDrawing)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _ctrl = Input.GetKey(KeyCode.LeftControl);
#endif
            if (Input.GetKeyDown(KeyCode.Z) && _ctrl && UndoStack.Count > 0)
            {
                Undo(_clientId);
            }
            if (Input.GetKeyDown(KeyCode.Y) && _ctrl && RedoStack.Count > 0)
            {
                Redo(_clientId);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            _isDrawing = true;
            TryStartPainting();
            RedoStack.Clear(); // Clear redo stack on new paint
            _paintedRedoObjects.Clear();
        }

        if (Input.GetMouseButton(0))
        {
            TryContinuePainting();
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDrawing = false;
            _paintedUndoObjects.Push(LastPaintedObject);
            LastPaintedObject = null;
        }
    }
    #endregion

    #region Painting Methods
    private Vector2? _lastPaintedUV = null; // Add this to Painter class

    private void TryContinuePainting()
    {
        if (RaycastFromMouse(out RaycastHit hit) && hit.collider.gameObject == LastPaintedObject)
        {
            PaintableTexture paintable = hit.collider.GetComponent<PaintableTexture>();
            if (paintable != null)
            {
                Vector2 currentUV = hit.textureCoord;

                if (_lastPaintedUV.HasValue)
                {
                    paintable.DrawLine(_lastPaintedUV.Value, currentUV);
                }
                else
                {
                    paintable.PaintAt(currentUV);
                }

                _lastPaintedUV = currentUV;
            }
        }
    }

    private void TryStartPainting()
    {
        if (RaycastFromMouse(out RaycastHit hit))
        {
            PaintableTexture paintable = hit.collider.GetComponent<PaintableTexture>();
            if (paintable != null)
            {
                SaveTextureState(paintable.PaintTexture, paintable.gameObject.name);
                paintable.PaintAt(hit.textureCoord);
                LastPaintedObject = paintable.gameObject;
                _lastPaintedUV = hit.textureCoord; // Start tracking UV
            }
        }
    }

    private bool RaycastFromMouse(out RaycastHit hit)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit);
    }
    #endregion

    #region Undo/Redo Methods
    public void Undo(int clientId)
    {
        if (_paintedUndoObjects.Count > 0)
        {
            LastPaintedObject = _paintedUndoObjects.Pop();
            _paintedRedoObjects.Push(LastPaintedObject);
            PaintableTexture paintable = LastPaintedObject.GetComponent<PaintableTexture>();
            if (paintable != null && UndoStack.ContainsKey(clientId) && UndoStack[clientId].Count > 0)
            {
                SaveTextureState(paintable.PaintTexture, LastPaintedObject.name, isUndo: false); // Save for redo
                paintable.PaintTexture.SetPixels(UndoStack[clientId].Pop());
                paintable.PaintTexture.Apply();
            }
        }
    }

    public void Redo(int clientId)
    {
        if (_paintedRedoObjects.Count > 0)
        {
            LastPaintedObject = _paintedRedoObjects.Pop();
            _paintedUndoObjects.Push(LastPaintedObject);
            PaintableTexture paintable = LastPaintedObject.GetComponent<PaintableTexture>();
            if (paintable != null && RedoStack.ContainsKey(clientId) && RedoStack[clientId].Count > 0)
            {
                SaveTextureState(paintable.PaintTexture, LastPaintedObject.name, isUndo: true); // Save for undo
                paintable.PaintTexture.SetPixels(RedoStack[clientId].Pop());
                paintable.PaintTexture.Apply();
            }
        }
    }

    public void SaveTextureState(Texture2D paintTexture, string name, bool isUndo = true)
    {
        if (!UndoStack.ContainsKey(_clientId))
        {
            UndoStack.Add(_clientId, new Stack<Color[]>());
        }
        if (!RedoStack.ContainsKey(_clientId))
        {
            RedoStack.Add(_clientId, new Stack<Color[]>());
        }

        if (isUndo)
        {
            if (UndoStack[_clientId].Count == 5)
            {
                TrimStack(_clientId);
            }
            UndoStack[_clientId].Push(paintTexture.GetPixels());
        }
        else
        {
            RedoStack[_clientId].Push(paintTexture.GetPixels());
        }
    }

    private void TrimStack(int clientId)
    {
        var tempList = new List<Color[]>(UndoStack[clientId]);
        tempList.RemoveAt(tempList.Count - 1);
        tempList.Reverse();
        UndoStack[clientId] = new Stack<Color[]>(tempList);
    }
    #endregion

    public void SetBrushColor(Color newColor)
    {
        BrushColor = newColor;
    }

    public void SetThickness(int newThickness)
    {
        BrushSize = newThickness;
    }
}
