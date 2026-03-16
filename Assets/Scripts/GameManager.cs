using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.AI.Navigation;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Player")]
    public PlayerStateMachine player;

    [Header("Path Conditions")]
    public List<PathCondition> pathConditions = new List<PathCondition>();

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
       
    }

    
    //Interaction que es crida desde PlayerStateMachine
    public void HandleInteraction(GameObject obj)
    {
        //Intentem obtenir el component MovablePlatform
        MovablePlatform platform = obj.GetComponent<MovablePlatform>();
        if (platform == null) { platform = obj.GetComponentInParent<MovablePlatform>(); } //

        if (platform != null)
        {
            platform.Interact();
            return;
        }

        //Aqui podrem afegir altres tipus d'interactuables en el futur
        //Ex: Interactuable interactuable = obj.GetComponent<Interactuable>();
        Debug.Log($"Objecte interactuable sense MovablePlatform: {obj.name}");
    }


    //Ara ja no evaluo condicions al Update sino que cada cop que finalitza un moviment o rotacio de plataforma es crida desde MovablePlatform
    public void EvaluateConditions()
    {
        foreach (PathCondition pc in pathConditions)
        {
            int count = 0;

            for (int i = 0; i < pc.conditions.Count; i++)
            {
                bool rotationOk = IsRotationClose(
                    pc.conditions[i].conditionObject.eulerAngles,
                    pc.conditions[i].eulerAngle
                );
                bool positionOk = Vector3.Distance(
                    pc.conditions[i].conditionObject.localPosition,
                    pc.conditions[i].position
                ) < 0.01f;

                if (rotationOk && positionOk) count++;
            }

            bool conditionsMet = count == pc.conditions.Count;

            //Activem o desactivem NavMeshLinks
            foreach (SinglePath sp in pc.paths)
            {
                if (sp.navMeshLink != null)
                {
                    sp.navMeshLink.enabled = conditionsMet;
                }

                else { Debug.LogWarning($"NavMeshLink null a: {pc.pathConditionName}"); }
            }

            //Rebake de plataformes de unió si les condicions es compleixen
            foreach (RebakePlatform rp in pc.rebakePlatforms)
            {
                if (rp.navMeshSurface != null)
                {
                    if (conditionsMet)
                    {
                        rp.navMeshSurface.BuildNavMesh();
                        Debug.Log($"Rebake de unió: {rp.navMeshSurface.gameObject.name}");
                    }
                }
            }
        }
    }


    private bool IsRotationClose(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(a.x, b.x)) < 5f &&
               Mathf.Abs(Mathf.DeltaAngle(a.y, b.y)) < 5f &&
               Mathf.Abs(Mathf.DeltaAngle(a.z, b.z)) < 5f;
    }
}

//Classes per a les condicions de camins

[System.Serializable]
public class PathCondition
{
    public string pathConditionName;
    public List<Condition> conditions;
    public List<SinglePath> paths;
    public List<RebakePlatform> rebakePlatforms;    //Plataformes que cal rebakear quan es compleixen les condicions
}

[System.Serializable]
public class Condition
{
    public Transform conditionObject;
    public Vector3 eulerAngle;
    public Vector3 position;
}

[System.Serializable]
public class SinglePath
{
    public NavMeshLink navMeshLink;
}

[System.Serializable]
public class RebakePlatform
{
    public NavMeshSurface navMeshSurface;   //NavMeshSurface de la plataforma d'unió a rebakear
}