using UnityEngine;
using UnityEngine.UI;

public class BlueprintMeteorit : MonoBehaviour
{
    [SerializeField] Image[] imagenes;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < imagenes.Length; i++)
        {
            ChangeColor(new Color(0,0,0,0.2f), i);
        }
        
    }

    public void ChangeColor(Color tempColor, int theObject)
    {
        imagenes[theObject].color = tempColor;
    }

}
