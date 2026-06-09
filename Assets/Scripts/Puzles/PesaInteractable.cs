using UnityEngine;
public class PesaInteractable : MonoBehaviour
{
    public enum PesaState { Suelo, Agarrada, EnPlataforma }
    [HideInInspector] public PesaState State = PesaState.Suelo;
    [HideInInspector] public Vector3 OriginalPosition;
    [HideInInspector] public PlataformaBalanca PlataformaActual = null; // null si no està en cap
    private Highlightable highlightable;
    void Start()
    {
        OriginalPosition = transform.position;
        highlightable = GetComponent<Highlightable>();
    }
    public void Highlight() => highlightable?.Highlight();
    public void UnHighlight() => highlightable?.UnHighlight();
    // Crida aquesta quan la pesa surt d'una plataforma (la plataforma la crida)
    public void OnRemovedFromPlataforma()
    {
        PlataformaActual = null;
        State = PesaState.Suelo;
    }
    // Crida aquesta quan la pesa s'assigna a una plataforma
    public void OnPlacedOnPlataforma(PlataformaBalanca plataforma)
    {
        PlataformaActual = plataforma;
        State = PesaState.EnPlataforma;
    }
}