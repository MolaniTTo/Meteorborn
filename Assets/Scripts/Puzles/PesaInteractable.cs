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
        UniqueID uid = GetComponent<UniqueID>();
        if (uid != null) { WorldManager.Instance?.RegisterMovedObject(uid.ID, transform.position, transform.rotation); }
    }
    // Crida aquesta quan la pesa s'assigna a una plataforma
    public void OnPlacedOnPlataforma(PlataformaBalanca plataforma)
    {
        PlataformaActual = plataforma;
        State = PesaState.EnPlataforma;
        UniqueID uid = GetComponent<UniqueID>();
        if (uid != null) { WorldManager.Instance?.RegisterMovedObject(uid.ID, transform.position, transform.rotation); }
    }
}