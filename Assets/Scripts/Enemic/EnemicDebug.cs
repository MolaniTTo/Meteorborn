using UnityEngine;

/// <summary>
/// Afegeix aquest component al mateix GameObject que EnemicAI.
/// Mostra un overlay en pantalla amb el node actiu i l'estat intern.
/// Només actiu en l'Editor i en builds de Development.
/// </summary>
public class EnemicAIDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    [Tooltip("Offset en pantalla respecte la posició world de l'enemic")]
    public Vector2 screenOffset = new Vector2(0f, 40f);

    private EnemicAI enemic;
    private string currentNode = "---";
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;

    // Colors per node
    private static readonly Color COL_CURAR = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color COL_ATACAR = new Color(0.9f, 0.1f, 0.1f);
    private static readonly Color COL_ALERTA = new Color(0.9f, 0.5f, 0.0f);
    private static readonly Color COL_PERSEGUIR = new Color(0.9f, 0.9f, 0.0f);
    private static readonly Color COL_BUSCAR = new Color(0.3f, 0.6f, 1.0f);
    private static readonly Color COL_PATRULLAR = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color COL_IDLE = new Color(0.4f, 0.4f, 0.4f);

    void Awake()
    {
        enemic = GetComponent<EnemicAI>();
    }

    /// <summary>
    /// Crida aquest mètode des de cada node BT al principi del seu Execute().
    /// </summary>
    public void SetActiveNode(string nodeName)
    {
        currentNode = nodeName;
    }

    void OnGUI()
    {
        if (!showDebug || enemic == null) return;
        if (Camera.main == null) return;

        // Inicialitza estils una vegada
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 11;
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.padding = new RectOffset(6, 6, 4, 4);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.fontStyle = FontStyle.Bold;
        }

        // Posició en pantalla
        Vector3 worldPos = transform.position + Vector3.up * 2.5f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0) return; // darrere la càmera

        // Converteix a coordenades GUI (Y invertida)
        float x = screenPos.x + screenOffset.x - 80f;
        float y = Screen.height - screenPos.y + screenOffset.y;

        // Color del node actiu
        Color nodeColor = currentNode switch
        {
            "CURAR" => COL_CURAR,
            "ATACAR" => COL_ATACAR,
            "ALERTA" => COL_ALERTA,
            "PERSEGUIR" => COL_PERSEGUIR,
            "BUSCAR" => COL_BUSCAR,
            "PATRULLAR" => COL_PATRULLAR,
            _ => COL_IDLE
        };

        // Construeix el text
        string targetName = enemic.targetHealth != null
            ? enemic.targetHealth.gameObject.name
            : "null";

        float distToTarget = enemic.targetHealth != null
            ? Vector3.Distance(enemic.transform.position, enemic.targetHealth.transform.position)
            : -1f;

        bool canSee = enemic.targetHealth != null
            ? enemic.CanSeeTarget(enemic.targetHealth.transform)
            : false;

        float distToGuard = Vector3.Distance(enemic.transform.position, enemic.guardPoint != null ? enemic.guardPoint.position : enemic.transform.position);

        string info =
            $"NODE: {currentNode}\n" +
            $"Target: {targetName}\n" +
            $"DistTarget: {distToTarget:F1}  CanSee: {canSee}\n" +
            $"DistGuard: {distToGuard:F1}  Energia: {enemic.energia:F0}\n" +
            $"PreScream:{enemic.isPreScream}  Screaming:{enemic.isScreaming}\n" +
            $"LookAround:{enemic.isLookingAround}  SearchTimer:{enemic.searchTimer:F1}\n" +
            $"GhostDist:{Vector3.Distance(enemic.transform.position, enemic.ghostAgent.transform.position):F1}";

        // Dibuixa la caixa
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(nodeColor.r * 0.3f, nodeColor.g * 0.3f, nodeColor.b * 0.3f, 0.85f);
        Rect rect = new Rect(x, y, 220f, 120f);
        GUI.Box(rect, "", boxStyle);
        GUI.backgroundColor = prevColor;

        // Dibuixa el text amb color del node
        labelStyle.normal.textColor = nodeColor;
        GUI.Label(new Rect(x + 4f, y + 2f, 214f, 116f), info, labelStyle);
    }

    // ?? Gizmos addicionals ????????????????????????????????????????????????????
    void OnDrawGizmos()
    {
        if (!showDebug || enemic == null) return;

        // Línia cap al target
        if (enemic.targetHealth != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, enemic.targetHealth.transform.position + Vector3.up);
        }

        // Punt lastSeenPosition
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(enemic.lastSeenPosition, 0.2f);

        // Ghost position
        if (enemic.ghostAgent != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(enemic.ghostAgent.transform.position, 0.3f);
        }
    }
}