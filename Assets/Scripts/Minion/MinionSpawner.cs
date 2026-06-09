using System.Collections;
using UnityEngine;


public class MinionSpawner : MonoBehaviour
{
    [Header("Configuració")]
    [SerializeField] private GameObject minionPrefab;   // prefab del minion
    [SerializeField] private GameObject spawnVFX;       // efecte de fum/pop (opcional)

    // Referència al minion viu en escena (null si no n'hi ha cap)
    [HideInInspector] public MinionAI spawnedMinion;
    public bool minionSpawned = false;
    private float respawnDelay = 0.1f;

    private void Start()
    {
        StartCoroutine(SpawnMinionWithDelay(respawnDelay));
    }

    private IEnumerator SpawnMinionWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (minionSpawned == false)
        {
            Debug.Log($"[MinionSpawner] Generant un nou minion al spawner '{name}' després d'una espera de {delay} segons.");
            SpawnMinion();
        }
        else
        {
            Debug.LogWarning($"[MinionSpawner] No es pot generar un nou minion al spawner '{name}' perquè ja hi ha un minion viu.");
        }

    }

    public void SpawnMinion()
    {
        // Destrueix l'instància anterior si existeix
        if (spawnedMinion != null)
        {
            Debug.LogWarning($"[MinionSpawner] Ja hi ha un minion viu al spawner '{name}', es destruirà abans de crear-ne un de nou.");
            Destroy(spawnedMinion.gameObject);
        }
           

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

        SaveManager.Instance?.ApplyMinionData(this);

    }

    // Dibuixa el punt de spawn a l'editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }
}