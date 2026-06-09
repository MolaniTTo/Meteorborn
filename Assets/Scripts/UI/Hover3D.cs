using UnityEngine;
using UnityEngine.InputSystem;

public class Hover3D : MonoBehaviour
{
    private Vector3 escalaInicial;
    private Vector3 escalaObjetivo;

    private void Start() {
        escalaInicial = transform.localScale;
        escalaObjetivo = transform.localScale;
    }

    void Update()
    {
        // Suavizar el cambio de escala
        if (Vector3.Distance(escalaObjetivo, transform.localScale) > 0.001f) {
            transform.localScale = Vector3.MoveTowards(transform.localScale, escalaObjetivo, 100f * Time.deltaTime);
        }

        // Raycast
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                //Debug.Log("Mouse sobre: " + gameObject.name);
                // Escala objetivo: 1.5 veces más grande
                escalaObjetivo = escalaInicial * 1.5f;
            }
            else
            {
                escalaObjetivo = escalaInicial;
            }
        }
        else
        {
            escalaObjetivo = escalaInicial;
        }
    }
}