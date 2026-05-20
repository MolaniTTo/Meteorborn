// ConfirmDialogUI.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtMessage;
    [SerializeField] private TMP_Text txtConfirmLabel;  // text del botó confirm
    [SerializeField] private TMP_Text txtCancelLabel;   // text del botó cancel
    [SerializeField] private GameObject focusConfirm;
    [SerializeField] private GameObject focusCancel;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;

    private Action onConfirm;
    private Action onCancel;
    private Action onBack; // ← nou
    private bool focusOnConfirm = true;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    void Awake()
    {
        var uiMap = playerInput.actions.FindActionMap("UI", throwIfNotFound: true);
        navigateAction = uiMap.FindAction("Navigate", throwIfNotFound: true);
        submitAction = uiMap.FindAction("Submit", throwIfNotFound: true);
        cancelAction = uiMap.FindAction("Cancel", throwIfNotFound: true);
        gameObject.SetActive(false);
    }

    public void Show(string message, string confirmText, string cancelText,
                     Action onConfirm, Action onCancel, Action onBack = null)
    {
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        this.onBack = onBack ?? onCancel;

        txtMessage.text = message;
        txtConfirmLabel.text = confirmText;
        txtCancelLabel.text = cancelText;

        focusOnConfirm = true;
        UpdateFocusVisual();
        gameObject.SetActive(true);

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
    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        float x = ctx.ReadValue<Vector2>().x;
        if (x > 0.5f) focusOnConfirm = false; // joystick derecha → Cancel (derecha)
        else if (x < -0.5f) focusOnConfirm = true;  // joystick izquierda → Confirm (izquierda)
        UpdateFocusVisual();
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (focusOnConfirm) onConfirm?.Invoke();
        else onCancel?.Invoke();
    }

    private void OnCancel(InputAction.CallbackContext ctx) => onBack?.Invoke();

    private void UpdateFocusVisual()
    {
        focusConfirm?.SetActive(focusOnConfirm);
        focusCancel?.SetActive(!focusOnConfirm);
    }
}