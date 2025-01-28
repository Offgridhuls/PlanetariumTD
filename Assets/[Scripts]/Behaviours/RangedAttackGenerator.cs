using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;
using System;
using Unity.VisualScripting;
using Planetarium;

[Serializable]
public class RangedAttackGenerator : BehaviourBase
{
    [SerializeField]
    float Altitude;
    
    [SerializeField]
    float targetUpdateInterval = 0.5f; // How often to search for new target

    [Header("Collision Avoidance")]
    [SerializeField]
    float avoidanceRadius = 5f; // How far to check for other enemies
    [SerializeField]
    float avoidanceForce = 10f; // How strongly to avoid other enemies
    [SerializeField]
    float maxAvoidanceAngle = 45f; // Maximum angle to deviate from path for avoidance

    [Header("Target Settings")]
    [SerializeField]
    float arrivalDistance = 2f; // How close we need to be to count as "arrived"

    
   
    [Header("Attack Settings")]
    private Transform firePoint;
    private ProjectileBase projectilePrefab;
    private float attackTimer;

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
        attackTimer = 0f;
        Debug.Log($"MoveToNearestGenerator: Entered state, orbit radius: {orbitRadius}");
        
        // Get references from the FlyingEnemyBase
        if (OwningEnemy is FlyingEnemyBase flyingEnemy)
        {
            firePoint = flyingEnemy.GetFirePoint();
            projectilePrefab = flyingEnemy.GetProjectilePrefab();
        }
        else
        {
            Debug.LogError("RangedAttackGenerator requires a FlyingEnemyBase!");
        }
    }

    public override void OnStateUpdate(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        if (Time.time - lastTargetUpdateTime >= targetUpdateInterval)
        {
            UpdateTargetGenerator();
        }

        Vector3 currentPosition = executer.transform.position;
        Vector3 planetPosition = OwningEnemy.GetCurrentPlanet().transform.position;

        // Check if we've reached the target
        if (currentTarget != null && Vector3.Distance(currentPosition, targetPoint) < arrivalDistance)
        {
            // Face the generator
            Vector3 directionToGenerator = (currentTarget.transform.position - currentPosition).normalized;
            Quaternion targetRot = Quaternion.LookRotation(directionToGenerator, executer.transform.up);
            executer.transform.rotation = Quaternion.Slerp(
                executer.transform.rotation,
                targetRot,
                OwningEnemy.GetStats().RotSpeed * Time.deltaTime
            );

            // Attack logic
            attackTimer += Time.deltaTime;
            if (attackTimer >= OwningEnemy.GetStats().attackSpeed)
            {
                attackTimer = 0f;
                FireAtGenerator();
            }

            // Signal state machine to transition to attack state
            stateMachine.SetTrigger("TargetReached");
            return;
        }

        if (currentTarget == null)
        {
            if (Vector3.Distance(targetPoint, currentPosition) < 5)
            {
                Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
                targetPoint = planetPosition + randomDirection * orbitRadius;
                Debug.Log($"No target, new random point: {targetPoint}");
            }
        }
        else
        {
            Vector3 generatorDirection = (currentTarget.transform.position - planetPosition).normalized;
            targetPoint = planetPosition + generatorDirection * orbitRadius;
            Debug.Log($"Moving to generator at {currentTarget.transform.position}, projected point: {targetPoint}");
        }

        // Calculate base movement direction
        Vector3 directionToTarget = (targetPoint - currentPosition).normalized;
        
        // Apply collision avoidance
        Vector3 avoidanceDirection = CalculateAvoidanceDirection(currentPosition, planetPosition);
        
        // Blend target direction with avoidance
        Vector3 finalDirection = Vector3.Slerp(directionToTarget, avoidanceDirection, 
            Vector3.Dot(directionToTarget, avoidanceDirection) < 0 ? 0.8f : 0.5f);
        
        // Move in the final direction
        Vector3 newPosition = currentPosition + finalDirection * OwningEnemy.GetStats().MoveSpeed * Time.deltaTime;
        
        // Project onto orbital sphere
        Vector3 directionToPlanet = (newPosition - planetPosition).normalized;
        executer.transform.position = planetPosition + directionToPlanet * orbitRadius;

        // Rotate to face movement direction
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

    private Vector3 CalculateAvoidanceDirection(Vector3 currentPosition, Vector3 planetPosition)
    {
        // Find nearby enemies
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        Vector3 avoidanceSum = Vector3.zero;
        int nearbyCount = 0;

        foreach (var enemy in enemies)
        {
            if (enemy.gameObject == OwningEnemy.gameObject) continue;

            Vector3 toEnemy = enemy.transform.position - currentPosition;
            float distance = toEnemy.magnitude;

            if (distance < avoidanceRadius && distance > 0)
            {
                // Project the enemy position onto our orbital plane
                Vector3 dirToPlanet = (currentPosition - planetPosition).normalized;
                Vector3 projectedToEnemy = Vector3.ProjectOnPlane(toEnemy, dirToPlanet);
                
                // Calculate avoidance vector (stronger when closer)
                float weight = 1.0f - (distance / avoidanceRadius);
                avoidanceSum -= projectedToEnemy.normalized * weight * avoidanceForce;
                nearbyCount++;
            }
        }

        if (nearbyCount > 0)
        {
            // Average the avoidance vectors
            Vector3 averageAvoidance = avoidanceSum / nearbyCount;
            
            // Project the avoidance direction onto the orbital sphere
            Vector3 dirToPlanet = (currentPosition - planetPosition).normalized;
            Vector3 projectedAvoidance = Vector3.ProjectOnPlane(averageAvoidance, dirToPlanet).normalized;
            
            // Limit the maximum deviation angle
            Vector3 currentDir = (targetPoint - currentPosition).normalized;
            float angle = Vector3.Angle(currentDir, projectedAvoidance);
            if (angle > maxAvoidanceAngle)
            {
                projectedAvoidance = Vector3.RotateTowards(currentDir, projectedAvoidance, 
                    Mathf.Deg2Rad * maxAvoidanceAngle, 0.0f);
            }
            
            return projectedAvoidance;
        }

        // If no nearby enemies, return direction to target
        return (targetPoint - currentPosition).normalized;
    }

    private void UpdateTargetGenerator()
    {
        lastTargetUpdateTime = Time.time;

        GeneratorBase[] generators = UnityEngine.Object.FindObjectsByType<GeneratorBase>(FindObjectsSortMode.None);
        
        if (generators.Length == 0)
        {
            currentTarget = null;
            return;
        }

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
            currentTarget = nearestGenerator;
        }

        // If we found a target, update target point
        if (currentTarget != null)
        {
            Vector3 directionToGenerator = (currentTarget.transform.position - OwningEnemy.GetCurrentPlanet().transform.position).normalized;
            targetPoint = OwningEnemy.GetCurrentPlanet().transform.position + directionToGenerator * orbitRadius;
        }
    }

    private void FireAtGenerator()
    {
        if (currentTarget == null || !firePoint || !projectilePrefab) return;

        // Calculate direction to target with prediction
        Vector3 directionToTarget = (currentTarget.transform.position - firePoint.position).normalized;
        
        // Spawn and initialize projectile
        ProjectileBase projectile = UnityEngine.Object.Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(directionToTarget));
        projectile.Initialize(OwningEnemy.GetStats().attackDamage, directionToTarget, OwningEnemy.GetStats().ProjectileSpeed);

    }

    public override void OnStateExit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        currentTarget = null;
    }
}