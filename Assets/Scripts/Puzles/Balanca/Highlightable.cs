using UnityEngine;

public class Highlightable : MonoBehaviour
{
    private Renderer rend;
    private Color colorOriginal;

    [SerializeField] private Color highlightColor = Color.yellow;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        colorOriginal = rend.material.color;
    }

    public void Highlight()
    {
        rend.material.color = highlightColor;
    }

    public void UnHighlight()
    {
        rend.material.color = colorOriginal;
    }
}