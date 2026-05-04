using UnityEngine;

public class HighlightObject : MonoBehaviour
{
    private Material mat;
    public Color baseEmission = Color.black;
    public Color highlightEmission = Color.white;
    public float intensity = 2f;

    void Start()
    {
        mat = GetComponent<Renderer>().material;

        // Asegurarse de que la emisión esté activada
        mat.EnableKeyword("_EMISSION");
    }

    public void Highlight(bool active)
    {
        if (active)
        {
            mat.SetColor("_EmissionColor", highlightEmission * intensity);
        }
        else
        {
            mat.SetColor("_EmissionColor", baseEmission);
        }
    }
}