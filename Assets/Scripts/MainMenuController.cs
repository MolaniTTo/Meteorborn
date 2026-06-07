using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject[] mainPanel;
    [SerializeField] GameObject[] loadPanel;
    [SerializeField] GameObject[] controlPanel;

    private void Start() {
        ShowMain();
    }

    public void NewGame() {
        Debug.Log("NuevaPartida");
    }

    public void PlayButton() {
        Desactivar(mainPanel);
        Desactivar(controlPanel);
        Activar(loadPanel);
        Debug.Log("Jugar");
    }

    public void ControlsButton() {
        Desactivar(mainPanel);
        Activar(controlPanel);
        Desactivar(loadPanel);
        Debug.Log("Controls");
    }

    public void ShowMain() {
        Activar(mainPanel);
        Desactivar(controlPanel);
        Desactivar(loadPanel);
    }

    public void ExitButton() {
        Debug.Log("Salir del juego");
    }

    private void Desactivar(GameObject[] temp) {
        for (int i = 0; i < temp.Length; i++) {
            temp[i].SetActive(false);
        }
    }

    private void Activar(GameObject[] temp) {
        for (int i = 0; i < temp.Length; i++) {
            temp[i].SetActive(true);
        }
    }
}
