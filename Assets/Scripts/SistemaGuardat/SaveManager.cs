using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    //Slot actiu durant la sessio de joc (0 o 1)
    public int ActiveSlot { get; private set; } = 0; // per defecte, slot 0 actiu. 
    private string GetSavePath(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json"); // ruta per a cada slot de guardat

    private SaveData currentData = new SaveData(); // mantenim una instància actual de les dades per facilitar la recollida i aplicació

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; } //Singleton bàsic, es pot millorar si cal
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetActiveSlot(int slot) => ActiveSlot = slot; //metode per canviar el slot actiu des de les opcions de menú, etc.

    public void Save() => SaveSlot(ActiveSlot);
    public bool Load() => LoadSlot(ActiveSlot);

    public void SaveSlot(int slot)
    {
        currentData.player = CollectPlayerData();
        currentData.minions = CollectMinionsData();
        currentData.world = CollectWorldData();
        currentData.tutorial = TutorialManager.Instance?.GetSaveData() ?? currentData.tutorial;

        string json = JsonUtility.ToJson(currentData, prettyPrint: true);  //converteix les dades a JSON per escriure-les a disc
        File.WriteAllText(GetSavePath(slot), json); //escriu el fitxer de guardat a disc
        Debug.Log($"[SaveManager] Partida guardada al slot {slot}.");
    }

    public bool LoadSlot(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return false; //si no hi ha fitxer de guardat, retorna fals per indicar que no s'ha pogut carregar

        string json = File.ReadAllText(path); //llegeix el fitxer de guardat des de disc
        currentData = JsonUtility.FromJson<SaveData>(json); //parseja el JSON per obtenir les dades de la partida guardada

        ApplyPlayerData(currentData.player); 
        ApplyMinionsData(currentData.minions);
        ApplyWorldData(currentData.world);
        TutorialManager.Instance?.LoadSaveData(currentData.tutorial);

        Debug.Log("[SaveManager] Partida carregada.");
        return true;
    }

    public bool HasSave() => HasSaveSlot(ActiveSlot);
    public bool HasSaveSlot(int slot) => File.Exists(GetSavePath(slot));
    public void DeleteSave() => DeleteSlot(ActiveSlot);
    public void DeleteSlot(int slot) //mètode per eliminar la partida guardada, útil per a opcions de menú o per reiniciar la partida des de zero
    {
        string path = GetSavePath(slot);
        if (File.Exists(path)) File.Delete(path);
        Debug.Log($"[SaveManager] Slot {slot} eliminat.");

    }
    public SlotPreviewData GetSlotPreview(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            var info = new FileInfo(path);
            return new SlotPreviewData
            {
                slot = slot,
                lastSaved = info.LastWriteTime.ToString("dd/MM/yyyy  HH:mm"),
                particles = data.player.particles,
                tutorialDone = data.tutorial.hasUsedParticles // pots canviar per qualsevol indicador de progrés
            };
        }
        catch
        {
            return null;
        }
    }

    // ── Recollida de dades ────────────────────────────────────────────────

    private PlayerSaveData CollectPlayerData() 
    {
        PlayerStateMachine player = FindFirstObjectByType<PlayerStateMachine>();
        if (player == null) return new PlayerSaveData();
        return new PlayerSaveData
        {
            posX = player.transform.position.x,
            posY = player.transform.position.y,
            posZ = player.transform.position.z,
            particles = player.playerParticles.Current
        };
    }

    private List<MinionSaveData> CollectMinionsData()
    {
        var list = new List<MinionSaveData>();
        foreach (MinionAI minion in FindObjectsByType<MinionAI>(FindObjectsSortMode.None))
        {
            UniqueID spawnerID = minion.spawner?.GetComponent<UniqueID>();
            list.Add(new MinionSaveData
            {
                spawnerID = spawnerID != null ? spawnerID.ID : "",
                state = minion.currentState.ToString(),
                posX = minion.transform.position.x,
                posY = minion.transform.position.y,
                posZ = minion.transform.position.z,
            });
        }
        return list;
    }

    private WorldSaveData CollectWorldData() //Funcio per recollir les dades globals del món, com zones completades, roques transportades, enemics morts, etc. Per ara només recull les zones completades, però es pot ampliar fàcilment si cal afegir més informació. Aquesta funció depèn de com implementem el WorldManager i quines dades necessitem guardar del món. Per ara assumeix que el WorldManager té un mètode GetSaveData() que retorna un WorldSaveData amb la informació necessària. Si no tenim encara aquesta funcionalitat al WorldManager, podem deixar aquesta funció buida o retornar un WorldSaveData per defecte fins que ho implementem. Quan implementem el WorldManager, haurem d'assegurar-nos que el GetSaveData() reculli tota la informació rellevant del món i la retorni en un format que puguem guardar i carregar correctament.
    {
        return WorldManager.Instance?.GetSaveData() ?? new WorldSaveData();
    }

    // ── Aplicació de dades ────────────────────────────────────────────────

    private void ApplyPlayerData(PlayerSaveData data)
    {
        PlayerStateMachine player = FindFirstObjectByType<PlayerStateMachine>();
        if (player == null) return;
        player.transform.position = new Vector3(data.posX, data.posY, data.posZ);
        player.playerParticles.numberOfParticles = data.particles;
    }

    private void ApplyMinionsData(List<MinionSaveData> list)
    {
        foreach (MinionSaveData data in list)
        {
            // Troba el spawner amb aquest ID
            foreach (UniqueID uid in FindObjectsByType<UniqueID>(FindObjectsSortMode.None))
            {
                if (uid.ID != data.spawnerID) continue;

                MinionSpawner spawner = uid.GetComponent<MinionSpawner>();
                if (spawner == null || spawner.spawnedMinion == null) continue;

                MinionAI minion = spawner.spawnedMinion;

                // Aplica posició
                minion.transform.position = new Vector3(data.posX, data.posY, data.posZ);

                // Aplica estat
                if (System.Enum.TryParse(data.state, out MinionAI.MinionState state))
                {
                    if (state == MinionAI.MinionState.Activat)
                        minion.Activate();
                    else if (state == MinionAI.MinionState.Debilitat)
                        minion.ChangeState(MinionAI.MinionState.Debilitat);
                    else
                        minion.ChangeState(MinionAI.MinionState.Desactivat);
                }
                break;
            }
        }
    }

    private void ApplyWorldData(WorldSaveData data)
    {
        WorldManager.Instance?.LoadSaveData(data);
        WorldManager.Instance?.ApplyToWorld();
    }
}

[System.Serializable]
public class SlotPreviewData
{
    public int slot;
    public string lastSaved;
    public int particles;
    public bool tutorialDone;
}