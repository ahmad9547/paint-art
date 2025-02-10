using CodeMonkey.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DrawMeshWithUndoRedo : MonoBehaviour
{
    public static DrawMeshWithUndoRedo Instance { get; private set; }

    [SerializeField] private Material drawMeshMaterial;
    private List<GameObject> undoStack = new List<GameObject>();
    private List<GameObject> redoStack = new List<GameObject>();

    private GameObject currentStroke;
    private Mesh currentMesh;
    private Vector3 lastPaintPosition;
    private float lineThickness = 0.2f;
    private Color lineColor = Color.green;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!UtilsClass.IsPointerOverUI())
        {
            // Perform a raycast to find the 3D object under the mouse pointer
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 hitPosition = hit.point;
                Vector3 hitNormal = hit.normal; // Surface normal for orientation
                Transform hitTransform = hit.transform;

                if (Input.GetMouseButtonDown(0))
                {
                    StartNewStroke(hitTransform); // Pass the hit object's transform
                    AddPointToStroke(hitPosition, hitNormal, hitTransform); // Add the initial point
                }

                if (Input.GetMouseButton(0) && currentStroke != null)
                {
                    AddPointToStroke(hitPosition, hitNormal, hitTransform);
                }

                if (Input.GetMouseButtonUp(0))
                {
                    FinalizeStroke();
                }
            }
        }

        // Undo (Ctrl+Z)
        if (Input.GetKeyDown(KeyCode.Z) && undoStack.Count > 0)
        {
            UndoStroke();
        }

        // Redo (Ctrl+Y)
        if (Input.GetKeyDown(KeyCode.Y) && redoStack.Count > 0)
        {
            RedoStroke();
        }
    }


    int _currentSortingOrder = 0;
    private void StartNewStroke(Transform parentTransform)
    {
        currentStroke = new GameObject("Stroke", typeof(MeshFilter), typeof(MeshRenderer));
        currentMesh = new Mesh();
        currentMesh.MarkDynamic();

        currentStroke.GetComponent<MeshFilter>().mesh = currentMesh;
        Material material = new Material(drawMeshMaterial) { color = lineColor };
        currentStroke.GetComponent<MeshRenderer>().material = material;

        // Attach the stroke to the object's transform
        currentStroke.transform.SetParent(parentTransform, false);

        Vector3 position = parentTransform.position;
        position.z -= 1;
        currentStroke.transform.localPosition = position;
        currentStroke.transform.localRotation = Quaternion.identity;
        currentStroke.transform.localScale = new Vector3(lineThickness, lineThickness, lineThickness * -1);
        currentStroke.AddComponent<SortingGroup>().sortingOrder = _currentSortingOrder++;

        undoStack.Add(currentStroke);
        ClearRedoStack();
    }

    private void AddPointToStroke(Vector3 hitPoint, Vector3 hitNormal, Transform parentTransform)
    {
        if (currentMesh == null) return;

        // Convert hit point to local space relative to the parent
        Vector3 localPoint = parentTransform.InverseTransformPoint(hitPoint);

        // Draw only if the new position is significantly different from the last paint position
        if (Vector3.Distance(lastPaintPosition, localPoint) > 0.1f)
        {
            // Align the circle with the surface normal
            Quaternion orientation = Quaternion.LookRotation(hitNormal, parentTransform.up);

            // Add a circular mesh at the local hit point
            MeshUtils.AddCircleToMesh(currentMesh, localPoint, lineThickness / 2, 20/*, orientation*/); // 20 segments for smoothness

            // Update the last paint position
            lastPaintPosition = localPoint;
        }
    }

    private void ClearRedoStack()
    {
        redoStack.ForEach(x => Destroy(x));
        redoStack.Clear(); // Clear redoStack on new draw
    }

    private void FinalizeStroke()
    {
        currentStroke = null;
    }

    private void UndoStroke()
    {
        GameObject lastStroke = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);
        redoStack.Add(lastStroke);

        lastStroke.SetActive(false); // Hide the stroke
    }

    private void RedoStroke()
    {
        GameObject lastRedo = redoStack[redoStack.Count - 1];
        redoStack.RemoveAt(redoStack.Count - 1);
        undoStack.Add(lastRedo);

        lastRedo.SetActive(true); // Show the stroke
    }

    public void SetThickness(float lineThickness)
    {
        this.lineThickness = lineThickness;
    }

    public void SetColor(Color lineColor)
    {
        this.lineColor = lineColor;
    }
}
