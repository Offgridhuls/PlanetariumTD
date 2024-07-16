using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;
using System;
using Unity.VisualScripting;

[Serializable]
public class MoveToNearestGenerator : BehaviourBase
{
    [SerializeField]
    float Altitude;
    
    float orbitRadius;
    Vector3 orbitAxis;
    Vector3 targetPoint;
    public override void StateInit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.StateInit(stateMachine, executer);
    }

    public override void OnStateEnter(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        orbitRadius = OwningEnemy.GetCurrentPlanet().GetPlanetRadius() + Altitude;
        Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
        targetPoint = OwningEnemy.GetCurrentPlanet().gameObject.transform.position + randomDirection * orbitRadius;
    }

    public override void OnStateUpdate(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        executer.transform.position = Vector3.MoveTowards(executer.transform.position, targetPoint, OwningEnemy.GetStats().MoveSpeed * Time.deltaTime);

        Vector3 directionToPlanet = (executer.transform.position - OwningEnemy.GetCurrentPlanet().transform.position).normalized;
        executer.transform.position = OwningEnemy.GetCurrentPlanet().transform.position + directionToPlanet * orbitRadius;

        Quaternion targetRotation = Quaternion.LookRotation(-directionToPlanet, executer.transform.up);
        executer.transform.rotation =  Quaternion.Slerp(executer.transform.rotation, targetRotation, OwningEnemy.GetStats().RotSpeed * Time.deltaTime);

        if(Vector3.Distance(targetPoint, OwningEnemy.transform.position) < 5)
        {
            Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
            targetPoint = OwningEnemy.GetCurrentPlanet().gameObject.transform.position + randomDirection * orbitRadius;
        }
    }

    public override void OnStateExit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
    
    }

}