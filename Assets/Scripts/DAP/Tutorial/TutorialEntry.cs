using UnityEngine;

[CreateAssetMenu(fileName = "TutorialEntry", menuName = "Tutorial/TutorialEntry")]
public class TutorialEntry : ScriptableObject
{
    [TextArea(2, 6)] 
    public string[] lines;
}