using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Diàleg de confirmació reutilitzable per a accions destructives (eliminar slot, etc.).
/// 
/// HIERARCHY EXEMPLE:
///   ConfirmDialog             (desactivat per defecte)
///   ├── TMP_Message           "Eliminar la partida del slot 1?"
///   ├── Btn_Confirm           "Sí, eliminar"
///   └── Btn_Cancel            "No, tornar"
///
/// El diàleg té la seva pròpia navegació esquerra/dreta entre els dos botons.
/// </summary>
public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtMessage;
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;
    [SerializeField] private GameObject focusConfirm; // ressaltat visual del botó confirm
    [SerializeField] private GameObject focusCancel;  // ressaltat visual del botó cancel

    [Header("Input (mateix PlayerInput que el menú)")]
    [SerializeField] private PlayerInput playerInput;

    private Action onConfirm;
    private Action onCancel;
    private bool focusOnConfirm = false; // per defecte, focus a "Cancel" (més segur)

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    void Awake()
    {
        var uiMap = playerInput.actions.FindActionMap("UI", throwIfNotFound: true);
        navigateAction = uiMap.FindAction("Navigate", throwIfNotFound: true);
        submitAction = uiMap.FindAction("Submit", throwIfNotFound: true);
        cancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);

        btnConfirm.onClick.AddListener(() => onConfirm?.Invoke());
        btnCancel.onClick.AddListener(() => onCancel?.Invoke());

        gameObject.SetActive(false);
    }

    // ── API pública ────────────────────────────────────────────────────────

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;

        txtMessage.text = message;
        gameObject.SetActive(true);

        focusOnConfirm = false; // focus per defecte a Cancel (evita esborrats accidentals)
        UpdateFocusVisual();

        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;
    }

    public void Hide()
    {
        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;

        gameObject.SetActive(false);
    }

    // ── Input ──────────────────────────────────────────────────────────────

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        float x = ctx.ReadValue<Vector2>().x;
        if (x > 0.5f) focusOnConfirm = true;
        else if (x < -0.5f) focusOnConfirm = false;
        UpdateFocusVisual();
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (focusOnConfirm) onConfirm?.Invoke();
        else onCancel?.Invoke();
    }

    private void OnCancel(InputAction.CallbackContext ctx) => onCancel?.Invoke();

    // ── Visual ─────────────────────────────────────────────────────────────

    private void UpdateFocusVisual()
    {
        focusConfirm?.SetActive(focusOnConfirm);
        focusCancel?.SetActive(!focusOnConfirm);
    }
}