using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SaveMenuUI : MonoBehaviour
{
    public static SaveMenuUI Instance { get; private set; }

    [SerializeField] private GameObject saveMenuUI;
    [SerializeField] private GameObject firstSelectedButton;

    private StatueSavePoint currentStatue;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (saveMenuUI != null) saveMenuUI.SetActive(false);
    }

    public void Open(StatueSavePoint statue)
    {
        currentStatue = statue;
        saveMenuUI.SetActive(true);
        Time.timeScale = 0f;
        EventSystem.current?.SetSelectedGameObject(firstSelectedButton);
    }

    public void Close()
    {
        saveMenuUI.SetActive(false);
        Time.timeScale = 1f;
        currentStatue?.OnMenuClosed();
        currentStatue = null;
    }

    // Aquests són els que vinculen els botons de UI (arrossega aquest GameObject als OnClick)
    public void OnSaveAndContinue()
    {
        SaveManager.Instance?.Save();
        Close();
    }

    public void OnSaveAndExit()
    {
        SaveManager.Instance?.Save();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}