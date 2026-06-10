using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class UniqueID : MonoBehaviour
{
    [SerializeField] private string id;
    public string ID => id;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }
}