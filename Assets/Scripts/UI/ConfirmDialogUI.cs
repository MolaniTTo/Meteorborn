// ConfirmDialogUI.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ConfirmDialogUI : MonoBehaviour
{
    [SerializeField] private TMP_Text txtMessage;
    [SerializeField] private TMP_Text txtConfirmLabel;
    [SerializeField] private TMP_Text txtCancelLabel;
    [SerializeField] private GameObject focusConfirm;
    [SerializeField] private GameObject focusCancel;

    [SerializeField] private float inputCooldownTime = 0.2f;
    private float inputCooldown = 0f;

    private Action onConfirm;
    private Action onCancel;
    private Action onBack;
    private bool focusOnConfirm = true;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    public void Init(InputAction navigate, InputAction submit, InputAction cancel)
    {
        navigateAction = navigate;
        submitAction = submit;
        cancelAction = cancel;
    }

    void Update()
    {
        if (inputCooldown > 0f) inputCooldown -= Time.deltaTime;
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

        navigateAction.performed -= OnNavigate;
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;
        navigateAction.performed += OnNavigate;
        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;

        inputCooldown = inputCooldownTime;
        gameObject.SetActive(true);
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
        if (x > 0.5f) focusOnConfirm = false;
        else if (x < -0.5f) focusOnConfirm = true;
        UpdateFocusVisual();
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (inputCooldown > 0f) return;
        if (focusOnConfirm) onConfirm?.Invoke();
        else onCancel?.Invoke();
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        if (inputCooldown > 0f) return;
        onBack?.Invoke();
    }

    private void UpdateFocusVisual()
    {
        focusConfirm?.SetActive(focusOnConfirm);
        focusCancel?.SetActive(!focusOnConfirm);
    }
}