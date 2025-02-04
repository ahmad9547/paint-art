using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private Painter _painter => Painter.Instance;
    public FlexibleColorPicker ColorPickerPanel;

    public string ClientID { get; private set; } = "1001";

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            ColorPickerPanel.gameObject.SetActive(true);
        }
    }

    public void SetBrushColor()
    {
        _painter.SetBrushColor(ColorPickerPanel.color);
        ColorPickerPanel.gameObject.SetActive(false);
    }



}
