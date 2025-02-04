using CodeMonkey.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DrawSingleMesh : MonoBehaviour
{
    public static DrawSingleMesh Instance { get; private set; }

    [SerializeField] private Material drawMeshMaterial;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh combinedMesh;

    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private Stack<Mesh> undoStack = new Stack<Mesh>();
    private Stack<Mesh> redoStack = new Stack<Mesh>();

    [SerializeField]
    private float lineThickness = 1f;
    private Color lineColor = Color.green;
    private Vector3 lastPaintPosition;

    private void Awake()
    {
        Instance = this;
        //SetupMesh();
    }

    private void SetupMesh()
    {
        GameObject drawSurface = new GameObject("DrawSurface", typeof(MeshFilter), typeof(MeshRenderer));
        meshFilter = drawSurface.GetComponent<MeshFilter>();
        meshRenderer = drawSurface.GetComponent<MeshRenderer>();

        Vector3 position = drawSurface.transform.localPosition;
        position.z -= 5;
        drawSurface.transform.localPosition = position;
        drawSurface.transform.localScale = new Vector3(lineThickness, lineThickness, lineThickness * -1);

        meshRenderer.material = new Material(drawMeshMaterial) { color = lineColor };
        combinedMesh = new Mesh();
        combinedMesh.MarkDynamic();
        meshFilter.mesh = combinedMesh;
    }

    private void Update()
    {
        if (!UtilsClass.IsPointerOverUI())
        {
            if (Input.GetMouseButtonDown(0))
            {
                SaveUndoState(); // Save for undo before starting a new stroke
                Vector3 worldPosition = GetWorldPosition();
                lastPaintPosition = worldPosition;
                AddPointToMesh(worldPosition);
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 worldPosition = GetWorldPosition();
                if (Vector3.Distance(lastPaintPosition, worldPosition) > 0.05f) // Ensure smooth drawing
                {
                    AddPointToMesh(worldPosition);
                    lastPaintPosition = worldPosition;
                }
            }

            if (Input.GetKeyDown(KeyCode.Z)) UndoStroke();
            if (Input.GetKeyDown(KeyCode.Y)) RedoStroke();
        }
    }

    private Vector3 GetWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point; // Draw on object surface
        }
        return Vector3.zero;
    }

    private void AddPointToMesh(Vector3 position)
    {
        MeshUtils.AddCircleToSingleMesh(ref vertices, ref triangles, ref uvs, position, lineThickness / 2, 20);
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        combinedMesh.Clear();
        combinedMesh.SetVertices(vertices);
        combinedMesh.SetTriangles(triangles, 0);
        combinedMesh.SetUVs(0, uvs);
        combinedMesh.RecalculateNormals();
    }

    private void SaveUndoState()
    {
        Mesh backup = new Mesh();
        backup.vertices = combinedMesh.vertices;
        backup.triangles = combinedMesh.triangles;
        backup.uv = combinedMesh.uv;
        backup.normals = combinedMesh.normals;
        undoStack.Push(backup);
        redoStack.Clear(); // Clear redo on new action
    }

    private void UndoStroke()
    {
        if (undoStack.Count > 0)
        {
            redoStack.Push(CopyMesh(combinedMesh));
            combinedMesh = undoStack.Pop();
            meshFilter.mesh = combinedMesh;
        }
    }

    private void RedoStroke()
    {
        if (redoStack.Count > 0)
        {
            undoStack.Push(CopyMesh(combinedMesh));
            combinedMesh = redoStack.Pop();
            meshFilter.mesh = combinedMesh;
        }
    }

    private Mesh CopyMesh(Mesh original)
    {
        Mesh copy = new Mesh();
        copy.vertices = original.vertices;
        copy.triangles = original.triangles;
        copy.uv = original.uv;
        copy.normals = original.normals;
        return copy;
    }

    public void SetThickness(float newThickness) => lineThickness = newThickness;
    public void SetColor(Color newColor)
    {
        lineColor = newColor;
        meshRenderer.material.color = newColor;
    }
}
