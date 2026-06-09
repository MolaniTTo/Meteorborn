using UnityEngine;

[CreateAssetMenu(fileName = "TutorialEntry", menuName = "Tutorial/TutorialEntry")]
public class TutorialEntry : ScriptableObject
{
    [TextArea(2, 6)]
    public string[] lines;

    [Tooltip("A partir de quina línia (0-based) s'activa la condició d'espera. -1 = mai (només informatiu)")]
    public int inputUnlocksAtLine = 0;
}