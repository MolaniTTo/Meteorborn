using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla la UI d'una targeta de slot individual.
/// 
/// HIERARCHY EXEMPLE:
///   SlotCard_0
///   ├── Panel_Empty          (actiu si slot buit)
///   │   └── TMP_NewGame      "Nova Partida"
///   ├── Panel_Filled         (actiu si slot té dades)
///   │   ├── TMP_SlotTitle    "Partida 1"
///   │   ├── TMP_LastSaved    "12/05/2025  18:30"
///   │   ├── TMP_Particles    "Partícules: 42"
///   │   ├── TMP_Tutorial     "Tutorial completat ✓"
///   │   └── Btn_Delete       (crida SlotSelectMenu.RequestDeleteSlot)
///   └── Image_FocusBorder    (activat quan té focus)
/// </summary>
public class SlotCardUI : MonoBehaviour
{
    [Header("Panells")]
    [SerializeField] private GameObject panelEmpty;
    [SerializeField] private GameObject panelFilled;

    [Header("Texts (slot ple)")]
    [SerializeField] private TMP_Text txtSlotTitle;
    [SerializeField] private TMP_Text txtLastSaved;
    [SerializeField] private TMP_Text txtParticles;
    [SerializeField] private TMP_Text txtTutorial;

    [Header("Focus visual")]
    [SerializeField] private GameObject focusBorder;

    // Guardada per als UnityEvents del botó
    private int slotIndex;
    private SlotSelectMenu menu;

    void Awake()
    {
        menu = FindFirstObjectByType<SlotSelectMenu>();
    }

    /// <summary>Actualitza la targeta amb les dades de preview (null = buit).</summary>
    public void Refresh(int slot, SlotPreviewData preview)
    {
        slotIndex = slot;

        bool hasSave = preview != null;
        panelEmpty.SetActive(!hasSave);
        panelFilled.SetActive(hasSave);

        if (hasSave)
        {
            txtSlotTitle.text = $"Partida {slot + 1}";
            txtLastSaved.text = $"Guardat: {preview.lastSaved}";
            txtParticles.text = $"Partícules: {preview.particles}";
            txtTutorial.text = preview.tutorialDone ? "Tutorial ✓" : "Tutorial pendent";
        }

    }

    /// <summary>Activa o desactiva el ressaltat de focus (navegació per mando).</summary>
    public void SetFocused(bool focused)
    {
        focusBorder?.SetActive(focused);
    }
}