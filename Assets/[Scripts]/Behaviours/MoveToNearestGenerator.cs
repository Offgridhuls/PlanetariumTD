using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;
using System;
using Unity.VisualScripting;
using Planetarium;

[Serializable]
public class MoveToNearestGenerator : BehaviourBase
{
    [SerializeField]
    float Altitude;
    
    [SerializeField]
    float targetUpdateInterval = 0.5f; // How often to search for new target

    private float orbitRadius;
    private Vector3 targetPoint;
    private float lastTargetUpdateTime;
    private GeneratorBase currentTarget;

    public override void StateInit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.StateInit(stateMachine, executer);
    }

    public override void OnStateEnter(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        orbitRadius = OwningEnemy.GetCurrentPlanet().GetPlanetRadius() + Altitude;
        UpdateTargetGenerator();
        Debug.Log($"MoveToNearestGenerator: Entered state, orbit radius: {orbitRadius}");
    }

    public override void OnStateUpdate(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        // Update target periodically
        if (Time.time - lastTargetUpdateTime >= targetUpdateInterval)
        {
            UpdateTargetGenerator();
        }

        Vector3 currentPosition = executer.transform.position;
        Vector3 planetPosition = OwningEnemy.GetCurrentPlanet().transform.position;

        if (currentTarget == null)
        {
            // If no generator found, orbit randomly like before
            if (Vector3.Distance(targetPoint, currentPosition) < 5)
            {
                Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
                targetPoint = planetPosition + randomDirection * orbitRadius;
                Debug.Log($"No target, new random point: {targetPoint}");
            }
        }
        else
        {
            // Project generator position onto orbital sphere
            Vector3 generatorDirection = (currentTarget.transform.position - planetPosition).normalized;
            targetPoint = planetPosition + generatorDirection * orbitRadius;
            Debug.Log($"Moving to generator at {currentTarget.transform.position}, projected point: {targetPoint}");
        }

        // Calculate direction to target on the orbital sphere
        Vector3 directionToTarget = (targetPoint - currentPosition).normalized;
        
        // Move towards target point
        Vector3 newPosition = currentPosition + directionToTarget * OwningEnemy.GetStats().MoveSpeed * Time.deltaTime;
        
        // Project the new position onto the orbital sphere
        Vector3 directionToPlanet = (newPosition - planetPosition).normalized;
        executer.transform.position = planetPosition + directionToPlanet * orbitRadius;

        // Rotate to face movement direction while staying aligned with planet
        Vector3 forward = -directionToPlanet;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right);
        
        Quaternion targetRotation = Quaternion.LookRotation(forward, up);
        executer.transform.rotation = Quaternion.Slerp(
            executer.transform.rotation,
            targetRotation,
            OwningEnemy.GetStats().RotSpeed * Time.deltaTime
        );

        Debug.DrawLine(currentPosition, targetPoint, Color.red);
        Debug.DrawLine(planetPosition, executer.transform.position, Color.blue);
    }

    private void UpdateTargetGenerator()
    {
        lastTargetUpdateTime = Time.time;

        // Find all generators using FindObjectsByType
        GeneratorBase[] generators = UnityEngine.Object.FindObjectsByType<GeneratorBase>(FindObjectsSortMode.None);
        
        if (generators.Length == 0)
        {
            Debug.Log("No generators found!");
            currentTarget = null;
            return;
        }

        // Find nearest non-destroyed generator
        float nearestDistance = float.MaxValue;
        GeneratorBase nearestGenerator = null;

        foreach (var generator in generators)
        {
            if (generator.IsDestroyed) continue;

            float distance = Vector3.Distance(
                OwningEnemy.transform.position,
                generator.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestGenerator = generator;
            }
        }

        if (nearestGenerator != currentTarget)
        {
            Debug.Log($"New target found at distance {nearestDistance}");
            currentTarget = nearestGenerator;
        }

        // If we found a target, update target point
        if (currentTarget != null)
        {
            Vector3 directionToGenerator = (currentTarget.transform.position - OwningEnemy.GetCurrentPlanet().transform.position).normalized;
            targetPoint = OwningEnemy.GetCurrentPlanet().transform.position + directionToGenerator * orbitRadius;
        }
    }

    public override void OnStateExit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        currentTarget = null;
        Debug.Log("Exiting MoveToNearestGenerator state");
    }
}