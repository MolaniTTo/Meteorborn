using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public PlayerSaveData player = new PlayerSaveData();
    public List<MinionSaveData> minions = new List<MinionSaveData>();
    public WorldSaveData world = new WorldSaveData();
    public TutorialSaveData tutorial = new TutorialSaveData();
}

[Serializable]
public class PlayerSaveData
{
    public float posX, posY, posZ;
    public int particles;
}

[Serializable]
public class MinionSaveData
{
    public string spawnerID;
    public string state;
    public float posX, posY, posZ;
    public float scaleX, scaleY, scaleZ;
    public float health;
}

[Serializable]
public class WorldSaveData
{
    public List<MovableObjectSaveData> movedObjects = new List<MovableObjectSaveData>();
    public List<string> deadEnemyIDs = new List<string>();
}

[Serializable]
public class MovableObjectSaveData
{
    public string id;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
}

[Serializable]
public class TutorialSaveData
{
    public bool hasSeenEnemy;
    public bool hasSeenMinion;
    public bool hasGetParticles;
    public bool hasSeenStatue;
    public bool hasSeenCohet;
    public bool tutorialCompleted;
    public bool hasActivatedMinion;
    public bool hasThrownMinion;
    public bool hasPlacedPiece;
    public bool hasPlacedAllPieces;
    public bool hasSeenBalanca;

}