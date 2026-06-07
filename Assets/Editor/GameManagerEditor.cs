#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            GameManager gm = (GameManager)target;
            foreach (PathCondition pc in gm.pathConditions)
            {
                foreach (Condition c in pc.conditions)
                {
                    // Si hi ha referència, autocompleta els camps
                    if (c.referenceTransform != null)
                    {
                        c.eulerAngle = c.referenceTransform.eulerAngles;
                        c.position = c.referenceTransform.localPosition;
                    }
                }
            }
            EditorUtility.SetDirty(gm);
        }
    }
}
#endif