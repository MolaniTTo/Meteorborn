using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BTSelectorEnemic", menuName = "BehaviourTree/Enemic/Selector")]
public class BTSelectorEnemic : BTNodeEnemic
{
    public List<BTNodeEnemic> children = new List<BTNodeEnemic>();

    public override bool Execute(EnemicAI enemic)
    {
        foreach (BTNodeEnemic node in children)
        {
            if (node.Execute(enemic)) return true;
        }
        return false;
    }
}