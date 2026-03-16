using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]            //M'asseguro de que tingui un collider per detectar la entrada i sortida de la zona de la camara
public class CameraZone : MonoBehaviour
{
    [Header("Camera Zone Data")]
    [SerializeField] private CameraZoneData zoneData;   //ScriptableObject que conté les dades de la zona de la camara

    [Header("Ortho Positions")]
    [SerializeField] private Transform[] orthoPositions;    //Array de posicions on la camara es colocara

    public CameraZoneData ZoneData => zoneData;    //Propietat pública per accedir a les dades de la zona de la camara des del CameraSwitcher o altres scripts que ho necessitin
    public Transform[] OrthoPositions => orthoPositions;  //Propietat pública per accedir a les posicions ortho des del CameraSwitcher o altres scripts que ho necessitin

    private CameraSwitcher cameraSwitcher;
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;                  //Asseguro que el collider sigui un trigger per detectar les entrades i sortides sense col·lisions físiques

        cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();   //Busco el CameraSwitcher a la escena per poder comunicar-me amb ell i canviar les dades de la camara quan entri o surti de la zona
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player")) return;    //Si el que entra no és el jugador, no entra
        cameraSwitcher.EnterZone(this);        //Si el jugador entra en zona, setejem les dades de la camara al CameraSwitcher per que les apliqui
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;   //Si el que surt no és el jugador, no surt
        cameraSwitcher.ExitZone(this);         //Si el jugador surt de la zona, resetejem les dades de la camara al CameraSwitcher per que torni a les dades per defecte
    }

}
