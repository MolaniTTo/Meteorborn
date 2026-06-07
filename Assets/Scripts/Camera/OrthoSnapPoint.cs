using Unity.AI.Navigation;
using UnityEngine;

public class OrthoSnapPoint : MonoBehaviour
{
    [Header("NavMesh Link associat")]
    public NavMeshLink navMeshLink;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = true;

    //El transform de aquest objecte ja te la rotation de X i Y be per unir les illes

    private void OnDrawGizmos() //Dibuixa un gizmo per mostrar la posició i direcció del punt d'unió
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}