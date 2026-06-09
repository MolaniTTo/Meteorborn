using UnityEngine;
public class PlataformaBalanca : MonoBehaviour
{
    public float pess;
    [SerializeField] Transform plataformaTransform;
    [SerializeField] Balanca balanca;
    [SerializeField] private float snapRadius = 1.5f;
    [SerializeField] private float cursorSurfaceOffset = 0.3f;
    [SerializeField] private float dropHeight = 3f;
    public float SnapRadius => snapRadius;
    public Vector3 objectivePosition;
    public Vector3 initialPosition;
    private AudioSource audioSource;
    private float miniCounter;
    private void Start()
    {
        objectivePosition = plataformaTransform.position;
        initialPosition = objectivePosition;
        audioSource = gameObject.GetComponent<AudioSource>();
    }
    private void FixedUpdate()
    {
        if (objectivePosition != plataformaTransform.position)
        {
            plataformaTransform.position = Vector3.MoveTowards(plataformaTransform.position, objectivePosition, 0.01f);
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            audioSource.Stop();
        }
        if (miniCounter >= 2f)
        {
            if (pess == 0)
            {
                objectivePosition = initialPosition;
            }
            else
            {
                objectivePosition[1] = initialPosition[1] - pess * 0.2f;
            }
            miniCounter = 0f;
        }
        miniCounter += Time.deltaTime;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pesa"))
        {
            Rigidbody temporalRb = other.GetComponent<Rigidbody>();
            pess += temporalRb.mass;
            objectivePosition = new Vector3(plataformaTransform.position.x, initialPosition[1] - temporalRb.mass * 0.2f, plataformaTransform.position.z);
            balanca.Actualitzar();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("pesa"))
        {
            Rigidbody temporalRb = other.GetComponent<Rigidbody>();
            objectivePosition = new Vector3(plataformaTransform.position.x, initialPosition[1] + temporalRb.mass * 0.2f, plataformaTransform.position.z);
            pess -= temporalRb.mass;
            balanca.Actualitzar();
        }
    }
    public bool IsInsideRadius(Vector3 pos)
    {
        Vector2 a = new Vector2(plataformaTransform.position.x, plataformaTransform.position.z);
        Vector2 b = new Vector2(pos.x, pos.z);
        return Vector2.Distance(a, b) < snapRadius;
    }
    private void OnDrawGizmos()
    {
        if (plataformaTransform == null) return;
        // Radi de snap (cercle verd)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        DrawCircle(plataformaTransform.position, snapRadius);
        // Punt on es posicionarà el cursor (esfera groga)
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(plataformaTransform.position + Vector3.up * cursorSurfaceOffset, 0.08f);
    }
    private void DrawCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
    public Vector3 GetSurfacePosition() =>
    plataformaTransform.position + Vector3.up * cursorSurfaceOffset;
    public Vector3 GetDropPosition() =>
        plataformaTransform.position + Vector3.up * dropHeight;
}