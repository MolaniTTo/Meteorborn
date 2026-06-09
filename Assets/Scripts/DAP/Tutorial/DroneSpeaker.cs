using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DroneSpeaker : MonoBehaviour
{
    public static DroneSpeaker Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI textUI;
    [SerializeField] private GameObject continuePrompt; // "Prem A per continuar" (opcional)

    [Header("Settings")]
    [SerializeField] private float textSpeed = 0.03f;
    private bool isFirstLine = true;

    // Input
    private InputSystem_Actions inputActions;

    // Estat intern
    private Queue<string> lineQueue = new Queue<string>();
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool isSpeaking = false;

    public bool IsTyping => isTyping;
    public int CurrentLineIndex { get; private set; } = 0;


    public void ForceClose()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        Close();
    }
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Enable();
        inputActions.Player.Confirm.performed += OnConfirm;
    }

    void OnDisable()
    {
        inputActions.Player.Confirm.performed -= OnConfirm;
        inputActions.Player.Disable();
    }

    void Start()
    {
        canvas.enabled = false;
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void Speak(TutorialEntry entry)
    {
        if (entry == null || entry.lines.Length == 0) return;
        Speak(entry.lines);
    }

    public void Speak(string[] lines)
    {
        lineQueue.Clear();
        CurrentLineIndex = 0;
        isFirstLine = true;
        foreach (string line in lines)
            lineQueue.Enqueue(line);

        isSpeaking = true;
        canvas.enabled = true;
        ShowNextLine();
    }


    public bool IsSpeaking => isSpeaking;

    // ── Input ─────────────────────────────────────────────────────────────────

    private void OnConfirm(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!isSpeaking) return;

        if (isTyping)
        {
            // Si està escrivint, mostra tot el text immediatament
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            textUI.text = currentLine;
            isTyping = false;
            if (continuePrompt != null) continuePrompt.SetActive(true);
        }
        else
        {
            // Passa a la següent línia
            ShowNextLine();
        }
    }

    // ── Lògica interna ────────────────────────────────────────────────────────

    private string currentLine = "";

    private void ShowNextLine()
    {
        if (lineQueue.Count == 0) { Close(); return; }

        if (!isFirstLine) CurrentLineIndex++;
        isFirstLine = false;

        currentLine = lineQueue.Dequeue();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(currentLine));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if (continuePrompt != null) continuePrompt.SetActive(false);
        textUI.text = "";

        foreach (char c in line)
        {
            textUI.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    private void Close()
    {
        isSpeaking = false;
        canvas.enabled = false;
        if (continuePrompt != null) continuePrompt.SetActive(false);
        textUI.text = "";
    }
}