using UnityEngine;
using System.Collections.Generic;

public class PaintableTexture : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private int _textureSize = 1024;
    [SerializeField] private Shader _shader;
    #endregion

    #region Private Fields
    private Texture2D _paintTexture;
    public Texture2D PaintTexture => _paintTexture;
    private Renderer _objectRenderer;
    #endregion

    #region Unity Methods
    private void Start()
    {
        _objectRenderer = GetComponent<Renderer>();
        InitializeTexture();
    }
    #endregion

    #region Initialization
    private void InitializeTexture()
    {
        Texture baseTexture = _objectRenderer.material.mainTexture;
        _paintTexture = new Texture2D(_textureSize, _textureSize);
        _paintTexture.filterMode = FilterMode.Bilinear;
        _paintTexture.wrapMode = TextureWrapMode.Clamp;

        Material newMaterial = new Material(_shader);
        newMaterial.SetTexture("_BaseTex", baseTexture);
        newMaterial.SetTexture("_PaintTex", _paintTexture);

        _objectRenderer.material = newMaterial;
        ClearTexture();
    }
    #endregion

    #region Painting Logic
    public void PaintAt(Vector2 uv)
    {
        int centerX = (int)(uv.x * _textureSize);
        int centerY = (int)(uv.y * _textureSize);

        for (int i = -Painter.Instance.BrushSize; i <= Painter.Instance.BrushSize; i++)
        {
            for (int j = -Painter.Instance.BrushSize; j <= Painter.Instance.BrushSize; j++)
            {
                float distance = Mathf.Sqrt(i * i + j * j);
                if (distance <= Painter.Instance.BrushSize)
                {
                    if (Random.value > 0.5f) continue;
                    int px = Mathf.Clamp(centerX + i, 0, _textureSize - 1);
                    int py = Mathf.Clamp(centerY + j, 0, _textureSize - 1);
                    _paintTexture.SetPixel(px, py, Painter.Instance.BrushColor);
                }
            }
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
            for (int i = -Painter.Instance.BrushSize; i <= Painter.Instance.BrushSize; i++)
            {
                for (int j = -Painter.Instance.BrushSize; j <= Painter.Instance.BrushSize; j++)
                {
                    float distance = Mathf.Sqrt(i * i + j * j);
                    if (distance <= Painter.Instance.BrushSize)
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
            _paintTexture.SetPixel(position.x, position.y, Painter.Instance.BrushColor);
        }
        _paintTexture.Apply();
    }

    #endregion

    #region Utility Methods
    private void ClearTexture()
    {
        Color[] clearPixels = new Color[_textureSize * _textureSize];
        for (int i = 0; i < clearPixels.Length; i++)
            clearPixels[i] = new Color(0, 0, 0, 0);

        _paintTexture.SetPixels(clearPixels);
        _paintTexture.Apply();
    }
    #endregion
}
