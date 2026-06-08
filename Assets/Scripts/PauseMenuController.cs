using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] GameObject[] allCanvas;
    [SerializeField] GameObject[] mainPanel;
    [SerializeField] GameObject[] controlPanel;

    private void Update() {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) {
            Activar(allCanvas);
            Activar(mainPanel);
            Time.timeScale = 0f;
        }
    }

    public void ResumeButton() {
        Desactivar(mainPanel);
        Desactivar(controlPanel);
        Desactivar(allCanvas);
        Time.timeScale = 1f;
        Debug.Log("Jugar");
    }

    public void VolverButton() {
        Activar(mainPanel);
        Desactivar(controlPanel);
    }

    public void ControlsButton() {
        Desactivar(mainPanel);
        Activar(controlPanel);
        Debug.Log("Controls");
    }

    public void ExitButton() {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
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
