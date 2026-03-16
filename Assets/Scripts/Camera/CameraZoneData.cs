using UnityEngine;


[CreateAssetMenu(fileName = "CameraZoneData", menuName = "Camera/Zone Data")]
public class CameraZoneData : ScriptableObject
{
    [Header("Ortho Settings")]
    public bool allowOrthoMode = true;      //Si es false, el boto de canvi de camara queda bloquejat 
    public float orthographicSize = 10f;    //La mida de la camara ortografica
}
