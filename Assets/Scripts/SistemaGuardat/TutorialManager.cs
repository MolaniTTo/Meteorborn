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


        if (SaveManager.Instance != null && SaveManager.Instance.HasPendingTutorialData())
        {
            data = SaveManager.Instance.ConsumePendingTutorialData();
            Debug.Log("[TutorialManager] Dades de tutorial recuperades del SaveManager.");
        }
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

    public bool IsTutorialCompleted() => data.tutorialCompleted;

    public void CompleteTutorial()
    {
        if (data.tutorialCompleted) return;
        data.tutorialCompleted = true;
        SaveManager.Instance?.Save();
    }

    private bool GetFlag(string flag)
    {
        return flag switch
        {
            "hasSeenEnemy" => data.hasSeenEnemy,
            "hasSeenMinion" => data.hasSeenMinion,
            "hasGetParticles" => data.hasGetParticles,
            "hasSeenStatue" => data.hasSeenStatue,
            "hasSeenCohet" => data.hasSeenCohet,
            "hasActivatedMinion" => data.hasActivatedMinion,
            "hasThrownMinion" => data.hasThrownMinion,
            "hasPlacedPiece" => data.hasPlacedPiece,
            "hasPlacedAllPieces" => data.hasPlacedAllPieces,
            "hasSeenBalanca" => data.hasSeenBalanca,


            _ => false
        };
    }

    private void SetFlag(string flag, bool value)
    {
        switch (flag)
        {
            case "hasSeenEnemy": data.hasSeenEnemy = value; break;
            case "hasSeenMinion": data.hasSeenMinion = value; break;
            case "hasGetParticles": data.hasGetParticles = value; break;
            case "hasSeenStatue": data.hasSeenStatue = value; break;
            case "hasSeenCohet": data.hasSeenCohet = value; break;
            case "hasActivatedMinion" : data.hasActivatedMinion = value; break;
            case "hasThrownMinion" : data.hasThrownMinion = value; break;
            case "hasPlacedPiece" : data.hasPlacedPiece = value; break;
            case "hasPlacedAllPieces" : data.hasPlacedAllPieces = value; break;
            case "hasSeenBalanca" : data.hasSeenBalanca = value; break;
        }
    }
}