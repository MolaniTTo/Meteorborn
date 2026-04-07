using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BTSequence", menuName = "BehaviourTree/Sequence")]
public class BTSequence : BTNode
{
    public List<BTNode> children = new List<BTNode>();

    public override bool Execute(MinionAI minion)
    {
        foreach (BTNode node in children)
        {
            if (!node.Execute(minion)) return false;
        }
        return true;
    }
}
