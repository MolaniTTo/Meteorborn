using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BTSelector", menuName = "BehaviourTree/Selector")]
public class BTSelector : BTNode
{
    public List<BTNode> children = new List<BTNode>();

    public override bool Execute(MinionAI minion)
    {
        foreach (BTNode node in children)
        {
            if (node.Execute(minion)) return true;
        }
        return false;
    }
}