using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    private Dictionary<string, MovableObjectSaveData> movedObjects = new Dictionary<string, MovableObjectSaveData>();
    private HashSet<string> deadEnemies = new HashSet<string>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterMovedObject(string id, Vector3 position, Quaternion rotation)
    {
        movedObjects[id] = new MovableObjectSaveData
        {
            id = id,
            posX = position.x,
            posY = position.y,
            posZ = position.z,
            rotX = rotation.eulerAngles.x,
            rotY = rotation.eulerAngles.y,
            rotZ = rotation.eulerAngles.z
        };
    }

    public void RegisterEnemyDead(string id) => deadEnemies.Add(id);

    public bool IsEnemyDead(string id) => deadEnemies.Contains(id);

    // ── Serialització ─────────────────────────────────────────────────────────

    public WorldSaveData GetSaveData()
    {
        var data = new WorldSaveData();
        foreach (var kvp in movedObjects)
            data.movedObjects.Add(kvp.Value);
        data.deadEnemyIDs = new List<string>(deadEnemies);
        return data;
    }

    public void LoadSaveData(WorldSaveData data)
    {
        movedObjects.Clear();
        foreach (var obj in data.movedObjects)
            movedObjects[obj.id] = obj;

        deadEnemies = new HashSet<string>(data.deadEnemyIDs);
    }

    // ── Aplicació al món ──────────────────────────────────────────────────────

    public void ApplyToWorld()
    {
        // Pedres i plataformes — mou els objectes a la posició guardada
        foreach (UniqueID uid in FindObjectsByType<UniqueID>(FindObjectsSortMode.None))
        {
            if (movedObjects.TryGetValue(uid.ID, out MovableObjectSaveData data))
            {
                uid.transform.position = new Vector3(data.posX, data.posY, data.posZ);
                uid.transform.rotation = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
            }
        }

        // Enemics — destrueix els que ja han mort
        foreach (UniqueID uid in FindObjectsByType<UniqueID>(FindObjectsSortMode.None))
        {
            if (deadEnemies.Contains(uid.ID))
                Destroy(uid.gameObject);
        }
    }

}