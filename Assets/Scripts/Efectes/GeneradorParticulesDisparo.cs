using UnityEngine;

public class GeneradorParticulesDisparo : MonoBehaviour
{
    [Header("Punts")]
    public Transform origen;
    public Transform objectiu;

    [Header("Configuració")]
    [SerializeField] private float velocityScale = 1.5f; // multiplica la velocitat segons distància
    [SerializeField] private bool followPoints = true;   // actualitza posició/rotació si els punts es mouen

    private ParticleSystem ps;
    private bool hasPlayed = false;

    private void Awake()
    {
        ps = GetComponentInChildren<ParticleSystem>();
    }

    public void Init(Transform from, Transform to)
    {
        origen = from;
        objectiu = to;

        transform.position = origen.position;
        transform.LookAt(objectiu);

        // Ajusta la velocitat inicial de les partícules segons la distància
        float dist = Vector3.Distance(origen.position, objectiu.position);
        var main = ps.main;
        main.startSpeed = dist * velocityScale;

        ps.Play();
        hasPlayed = true;
    }

    private void Update()
    {
        if (!hasPlayed) return;

        // Actualitza posició i rotació si els punts es mouen
        if (followPoints && origen != null && objectiu != null)
        {
            transform.position = origen.position;
            transform.LookAt(objectiu);

            // Reajusta velocitat si la distància canvia
            float dist = Vector3.Distance(origen.position, objectiu.position);
            var main = ps.main;
            main.startSpeed = dist * velocityScale;
        }

        // Destrueix quan el PS hagi acabat (no per temps fix)
        if (!ps.IsAlive())
            Destroy(gameObject);
    }
}