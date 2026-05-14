using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private TutorialSaveData data = new TutorialSaveData();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Crida això des de qualsevol sistema: TutorialManager.Instance.TriggerIfNew("hasSeenEnemy", MostraPanel);
    public void TriggerIfNew(string flag, Action onTrigger)
    {
        if (GetFlag(flag)) return; // ja vist
        SetFlag(flag, true);
        onTrigger?.Invoke();
        SaveManager.Instance?.Save();
    }

    public TutorialSaveData GetSaveData() => data;
    public void LoadSaveData(TutorialSaveData loaded) => data = loaded;

    private bool GetFlag(string flag)
    {
        return flag switch
        {
            "hasSeenEnemy" => data.hasSeenEnemy,
            "hasSeenMinion" => data.hasSeenMinion,
            "hasUsedOrthographic" => data.hasUsedOrthographic,
            "hasGetParticles" => data.hasGetParticles,
            "hasUsedParticles" => data.hasUsedParticles,
            "hasGetRedParticles" => data.hasGetRedParticles,
            "hasSeenStatue" => data.hasSeenStatue,

            _ => false
        };
    }

    private void SetFlag(string flag, bool value)
    {
        switch (flag)
        {
            case "hasSeenEnemy": data.hasSeenEnemy = value; break;
            case "hasSeenMinion": data.hasSeenMinion = value; break;
            case "hasUsedOrthographic": data.hasUsedOrthographic = value; break;
            case "hasGetParticles": data.hasGetParticles = value; break;
            case "hasUsedParticles": data.hasUsedParticles = value; break;
            case "hasGetRedParticles": data.hasGetRedParticles = value; break;
            case "hasSeenStatue": data.hasSeenStatue = value; break;
        }
    }
}