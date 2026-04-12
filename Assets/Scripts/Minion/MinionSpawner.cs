using UnityEngine;


public class MinionSpawner : MonoBehaviour
{
    [Header("Configuració")]
    [SerializeField] private GameObject minionPrefab;   // prefab del minion
    [SerializeField] private GameObject spawnVFX;       // efecte de fum/pop (opcional)

    // Referència al minion viu en escena (null si no n'hi ha cap)
    [HideInInspector] public MinionAI spawnedMinion;

    void Start()
    {
        // Spawnejar el minion al inici de la partida en estat Desactivat
        SpawnMinion();
    }

    public void SpawnMinion()
    {
        // Destrueix l'instància anterior si existeix
        if (spawnedMinion != null)
            Destroy(spawnedMinion.gameObject);

        // Efecte visual de pop (opcional)
        if (spawnVFX != null)
            Instantiate(spawnVFX, transform.position, Quaternion.identity);

        // Instancia el minion al punt de spawn
        GameObject go = Instantiate(minionPrefab, transform.position, transform.rotation);
        spawnedMinion = go.GetComponent<MinionAI>();

        // Assigna la referència al spawner perquè el minion pugui trucar-lo
        spawnedMinion.spawner = this;
        spawnedMinion.spawnPosition = transform.position;

        // Assegurem que comença desactivat
        spawnedMinion.ChangeState(MinionAI.MinionState.Desactivat);

        // Registra el minion al MinionManager
        MinionManager.Instance?.RegisterMinion(spawnedMinion);
    }

    // Dibuixa el punt de spawn a l'editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }
}