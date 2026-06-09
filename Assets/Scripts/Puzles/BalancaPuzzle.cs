using DG.Tweening;
using UnityEngine;
// Col·loca aquest script en el GameObject que té el trigger d'entrada al puzzle.
// Gestiona l'activació del mode puzzle al MinionCursor i exposa les posicions snap.
public class BalancaPuzzle : MonoBehaviour
{
    [Header("Referència al cursor del jugador")]
    [SerializeField] private MinionCursor minionCursor;
    [Header("Posicions snap de la balança")]
    [SerializeField] public Transform snapLeft;
    [SerializeField] public Transform snapRight;
    [Header("Radi de detecció de peses")]
    [SerializeField] private float pesaDetectionRadius = 2f;
    [Header("Balança")]
    [SerializeField] private Balanca balanca;
    [SerializeField] private PlataformaBalanca plataformaLeft;
    [SerializeField] private PlataformaBalanca plataformaRight;
    // ── Propietats públiques ──────────────────────────────────────────────────
    public bool IsActive { get; private set; } = false;
    // ── Trigger d'entrada/sortida ─────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        IsActive = true;
        minionCursor?.SetPuzzleMode(this);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        IsActive = false;
        minionCursor?.ClearPuzzleMode();
    }
    // ── Utilitats públiques ───────────────────────────────────────────────────
    // Retorna la pesa més propera al cursor (dins del radi), o null
    public PesaInteractable GetPesaUnderCursor(Vector3 cursorPos)
    {
        Collider[] hits = Physics.OverlapSphere(cursorPos, pesaDetectionRadius);
        PesaInteractable closest = null;
        float closestDist = float.MaxValue;
        foreach (Collider col in hits)
        {
            PesaInteractable pesa = col.GetComponent<PesaInteractable>();
            if (pesa == null) continue;
            if (pesa.State == PesaInteractable.PesaState.Agarrada) continue;
            float dist = Vector3.Distance(cursorPos, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pesa;
            }
        }
        return closest;
    }
    public PlataformaBalanca GetPlataformaUnderCursor(Vector3 cursorPos)
    {
        var b = GetBalanca();
        // Accedim a les plataformes via BalancaPuzzle
        if (plataformaLeft != null && plataformaLeft.IsInsideRadius(cursorPos)) return plataformaLeft;
        if (plataformaRight != null && plataformaRight.IsInsideRadius(cursorPos)) return plataformaRight;
        return null;
    }
    public Balanca GetBalanca() => balanca;
}