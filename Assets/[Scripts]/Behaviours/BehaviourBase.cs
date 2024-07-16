using FSMC.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourBase : FSMC_Behaviour
{
    protected EnemyBase OwningEnemy;

    public override void StateInit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.StateInit(stateMachine, executer);
        OwningEnemy = executer as EnemyBase;
    }

}
