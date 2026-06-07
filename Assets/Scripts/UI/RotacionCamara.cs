using UnityEngine;

public class RotacionCamara : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    [SerializeField] private float velocidadRotacion = 30f;
    [SerializeField] private Vector3 ejeRotacion = Vector3.up;

    void Update()
    {
        transform.Rotate(ejeRotacion * velocidadRotacion * Time.deltaTime);
    }
}