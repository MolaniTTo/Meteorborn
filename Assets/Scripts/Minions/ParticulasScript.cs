// ParticulaScript.cs
using UnityEngine;

public class ParticulaScript : MonoBehaviour
{
    [Header("Moviment")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float attractRadius = 4f;
    [SerializeField] private float attractForce = 3f;
    [SerializeField] private float collectRadius = 0.6f;

    private Transform playerTransform;
    private FontParticulesScript source;
    private Rigidbody rb;
    private float timeOffset;
    private Vector3 basePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 3f;
    }

    public void Init(FontParticulesScript src)
    {
        source = src;
        playerTransform = GameObject.FindWithTag("Player")?.transform;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        basePosition = transform.position;
        rb.linearVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // Recollida
        if (distToPlayer < collectRadius)
        {
            PlayerParticles.Instance?.Add(1);
            source.ReturnToPool(this);
            return;
        }

        // Atracció cap al player segons distància
        if (distToPlayer < attractRadius)
        {
            float strength = 1f - (distToPlayer / attractRadius); // 0 lluny, 1 a prop
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            rb.AddForce(dir * attractForce * strength, ForceMode.Force);
        }
        else
        {
            // Flotació suau quan el player és lluny
            float newY = basePosition.y + Mathf.Sin(Time.time * floatSpeed + timeOffset) * floatAmplitude;
            Vector3 target = new Vector3(basePosition.x, newY, basePosition.z);
            rb.AddForce((target - transform.position) * 2f, ForceMode.Force);
        }
    }
}