using CodeMonkey.Utils;
using UnityEngine;
using UnityEngine.UI;

public class DrawMeshUI : MonoBehaviour {

    private void Awake() {
        transform.Find("Thickness1Btn").GetComponent<Button>().onClick.AddListener(() => { SetThickness(2); });
        transform.Find("Thickness2Btn").GetComponent<Button>().onClick.AddListener(() => { SetThickness(5); });
        transform.Find("Thickness3Btn").GetComponent<Button>().onClick.AddListener(() => { SetThickness(10); });
        transform.Find("Thickness4Btn").GetComponent<Button>().onClick.AddListener(() => { SetThickness(20); });

        transform.Find("Color1Btn").GetComponent<Button>().onClick.AddListener(() => { SetColor(UtilsClass.GetColorFromString("000000")); });
        transform.Find("Color2Btn").GetComponent<Button>().onClick.AddListener(() => { SetColor(UtilsClass.GetColorFromString("FFFFFF")); });
        transform.Find("Color3Btn").GetComponent<Button>().onClick.AddListener(() => { SetColor(UtilsClass.GetColorFromString("22FF00")); });
        transform.Find("Color4Btn").GetComponent<Button>().onClick.AddListener(() => { SetColor(UtilsClass.GetColorFromString("0077FF")); });
    }

    private void SetThickness(int thickness) {
        Painter.Instance.SetThickness(thickness);
    }

    private void SetColor(Color color) {
        Painter.Instance.SetBrushColor(color);
    }

}