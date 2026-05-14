using UnityEngine;
using UnityEditor;


public class UniqueID : MonoBehaviour
{
    [SerializeField] private string id;
    public string ID => id;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
    }
}