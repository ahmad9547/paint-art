using UnityEngine;
using System.Collections.Generic;
using CodeMonkey.Utils;

public class PaintableTexture : MonoBehaviour
{
    [SerializeField] private int _textureSize = 1024;
    [SerializeField] private Shader _shader;
    private Texture2D _paintTexture;
    public Texture2D PaintTexture => _paintTexture;
    private Renderer _objectRenderer;

    private Vector2? _lastPaintedUV = null; // Store last painted position

    private int _brushSize => Painter.Instance.BrushSize;
    private Color _brushColor => Painter.Instance.BrushColor;
    private Painter _painter => Painter.Instance;

    private void Start()
    {
        _objectRenderer = GetComponent<Renderer>();
        InitializeTexture();
    }

    bool _isDrawing = false;
    bool _ctrl = true;

    private void Update()
    {
        if (!_isDrawing)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _ctrl = Input.GetKey(KeyCode.LeftControl);
#endif
            if (Input.GetKeyDown(KeyCode.Z) && _ctrl && _painter.UndoStack.Count > 0)
            {
                _painter.Undo(_paintTexture, gameObject.name);
            }
            if (Input.GetKeyDown(KeyCode.Y) && _ctrl && _painter.RedoStack.Count > 0)
            {
                _painter.Redo(_paintTexture, gameObject.name);
            }
        }

        if (UtilsClass.IsPointerOverUI()) return;
        if (Input.GetMouseButtonDown(0)) // On first press, save the state
        {
            _isDrawing = true;
            _painter.SaveTextureState(_paintTexture, gameObject.name);
            _lastPaintedUV = null; // Reset last position
            _painter.RedoStack.Clear();
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject.Equals(gameObject))
                {
                    StartPaint(hit.textureCoord);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDrawing = false;
            _lastPaintedUV = null; // Reset when mouse is released
        }
    }

    private void InitializeTexture()
    {
        // Wall ka original texture get karo
        Texture baseTexture = _objectRenderer.material.mainTexture;

        // Paint ke liye ek new texture banao
        _paintTexture = new Texture2D(_textureSize, _textureSize);
        _paintTexture.filterMode = FilterMode.Bilinear;
        _paintTexture.wrapMode = TextureWrapMode.Clamp;

        // Clone material taake original material effect na ho
        Material newMaterial = new Material(_shader);
        newMaterial.SetTexture("_BaseTex", baseTexture); // Wall ka texture assign karo
        newMaterial.SetTexture("_PaintTex", _paintTexture); // Paint ke liye texture assign karo

        _objectRenderer.material = newMaterial;
        ClearTexture();
    }

    void StartPaint(Vector2 coordinates)
    {
        Vector2 currentUV = coordinates;

        Debug.Log(coordinates);

        if (_lastPaintedUV.HasValue)
        {
            DrawLine(_lastPaintedUV.Value, currentUV);
            //Painter.Instance.DrawLine(gameObject.name, _lastPaintedUV.Value, currentUV, _brushSize, _brushColor);
        }
        else
        {
            Paint(currentUV);
            //Painter.Instance.Paint(gameObject.name, currentUV, _brushSize, _brushColor);
        }

        _lastPaintedUV = currentUV;
    }

    public void Paint(Vector2 uv)
    {
        int centerX = (int)(uv.x * _textureSize);
        int centerY = (int)(uv.y * _textureSize);

        for (int i = -_brushSize; i <= _brushSize; i++)
        {
            for (int j = -_brushSize; j <= _brushSize; j++)
            {
                // Calculate the distance from the center
                float distance = Mathf.Sqrt(i * i + j * j);

                // Check if the pixel lies within the circle radius
                if (distance <= _brushSize)
                {
                    // Apply randomness for spray effect
                    if (Random.value > 0.5f) continue; // Randomly skip some pixels

                    int px = Mathf.Clamp(centerX + i, 0, _textureSize - 1);
                    int py = Mathf.Clamp(centerY + j, 0, _textureSize - 1);
                    _paintTexture.SetPixel(px, py, _brushColor);
                }
            }
        }
        _paintTexture.Apply();
    }

    private void ApplyPixels(List<Vector2Int> positions, List<Color> colors)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            _paintTexture.SetPixel(positions[i].x, positions[i].y, colors[i]);
        }
        _paintTexture.Apply();
    }

    public void DrawLine(Vector2 startUV, Vector2 endUV)
    {
        int startX = (int)(startUV.x * _textureSize);
        int startY = (int)(startUV.y * _textureSize);
        int endX = (int)(endUV.x * _textureSize);
        int endY = (int)(endUV.y * _textureSize);

        int dx = Mathf.Abs(endX - startX);
        int dy = Mathf.Abs(endY - startY);
        int sx = startX < endX ? 1 : -1;
        int sy = startY < endY ? 1 : -1;
        int err = dx - dy;

        List<Vector2Int> pixelPositions = new List<Vector2Int>();

        while (true)
        {
            // Add brush-sized pixels at each point (checking for circular region)
            for (int i = -_brushSize; i <= _brushSize; i++)
            {
                for (int j = -_brushSize; j <= _brushSize; j++)
                {
                    // Calculate the distance from the center of the brush
                    float distance = Mathf.Sqrt(i * i + j * j);

                    // Only add pixels that are within the circular radius
                    if (distance <= _brushSize)
                    {
                        int px = Mathf.Clamp(startX + i, 0, _textureSize - 1);
                        int py = Mathf.Clamp(startY + j, 0, _textureSize - 1);
                        pixelPositions.Add(new Vector2Int(px, py));
                    }
                }
            }

            if (startX == endX && startY == endY) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; startX += sx; }
            if (e2 < dx) { err += dx; startY += sy; }
        }

        ApplyBrushToTexture(pixelPositions);
    }


    private void ApplyBrushToTexture(List<Vector2Int> pixelPositions)
    {
        foreach (var position in pixelPositions)
        {
            _paintTexture.SetPixel(position.x, position.y, _brushColor);
        }
        _paintTexture.Apply();
    }

    private void ClearTexture()
    {
        Color[] clearPixels = new Color[_textureSize * _textureSize];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color(0, 0, 0, 0); // Fully transparent pixels

        _paintTexture.SetPixels(clearPixels);
        _paintTexture.Apply();
    }

}
