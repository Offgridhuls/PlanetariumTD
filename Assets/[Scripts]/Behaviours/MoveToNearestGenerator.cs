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
    [SerializeField] float Altitude = 10f;
    [SerializeField] float targetUpdateInterval = 0.5f;
    [SerializeField] float arrivalDistance = 15f; // Increased to match RangedAttackGenerator's attack range

    [Header("Movement Settings")]
    [SerializeField] float smoothTime = 0.5f;
    [SerializeField] float maxSpeed = 20f;

    [Header("Collision Avoidance")]
    [SerializeField]
    float avoidanceRadius = 5f; // How far to check for other enemies
    [SerializeField]
    float avoidanceForce = 10f; // How strongly to avoid other enemies
    [SerializeField]
    float maxAvoidanceAngle = 45f; // Maximum angle to deviate from path for avoidance

    [Header("Flocking Settings")]
    [SerializeField] private bool useFlocking = true;
    [SerializeField] private float cohesionWeight = 1.0f;
    [SerializeField] private float separationWeight = 2.0f;
    [SerializeField] private float alignmentWeight = 1.0f;
    [SerializeField] private float targetWeight = 1.5f;
    [SerializeField] private float neighborRadius = 5f;
    [SerializeField] private float separationRadius = 3f;

    private float orbitRadius;
    private Vector3 targetPoint;
    private float lastTargetUpdateTime;
    private GeneratorBase currentTarget;
    private Vector3 currentVelocity;
    private Vector3 velocityChange;
    private FlockingHelper flockingHelper;

    public override void StateInit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.StateInit(stateMachine, executer);
    }

    public override void OnStateEnter(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.OnStateEnter(stateMachine, executer);
        
        if (useFlocking)
        {
            flockingHelper = new FlockingHelper
            {
                cohesionWeight = this.cohesionWeight,
                separationWeight = this.separationWeight,
                alignmentWeight = this.alignmentWeight,
                targetWeight = this.targetWeight,
                neighborRadius = this.neighborRadius,
                separationRadius = this.separationRadius,
                maxSpeed = OwningEnemy.GetStats().MoveSpeed,
                maxSteerForce = OwningEnemy.GetStats().MoveSpeed * 0.5f
            };
        }

        orbitRadius = 15;
        currentVelocity = Vector3.zero;
        currentTarget = null;
        UpdateTargetGenerator();
    }

    public override void OnStateUpdate(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        if (!OwningEnemy || !executer) return;

        Vector3 currentPosition = executer.transform.position;
        
        if (currentTarget == null || currentTarget.IsDestroyed)
        {
            UpdateTargetGenerator();
            if (currentTarget == null)
            {
                return;
            }
        }

        
        Vector3 targetPoint = currentTarget.transform.position;
        float distanceToGenerator = Vector3.Distance(currentPosition, targetPoint);

        if (distanceToGenerator <= arrivalDistance)
        {
            // Face the generator before transitioning
            Vector3 d2t = (currentTarget.transform.position - currentPosition).normalized;
            Quaternion targetRot = Quaternion.LookRotation(d2t, executer.transform.up);
            executer.transform.rotation = Quaternion.Slerp(
                executer.transform.rotation,
                targetRot,
                OwningEnemy.GetStats().RotSpeed * Time.deltaTime
            );

            // Only transition if we're facing the target
            float angleToTarget = Vector3.Angle(executer.transform.forward, d2t);
            if (angleToTarget < 30f) // Allow some tolerance
            {
                stateMachine.SetBool("TargetReached", true);
                return;
            }
        }

        /*// Calculate movement direction
        Vector3 moveDirection = (targetPoint - currentPosition).normalized;
        Vector3 targetVelocity = moveDirection * OwningEnemy.GetStats().MoveSpeed;

        if (useFlocking && flockingHelper != null)
        {
            // Add flocking force to the target velocity
            Vector3 flockingForce = GetFlockingForce(targetPoint);
            targetVelocity += flockingForce;
            targetVelocity = Vector3.ClampMagnitude(targetVelocity, OwningEnemy.GetStats().MoveSpeed);
        }

        // Smoothly interpolate current velocity
        currentVelocity = Vector3.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocityChange,
            0.1f,
            OwningEnemy.GetStats().MoveSpeed
        );*/

        // Update position using rigidbody
        if (OwningEnemy.rb != null)
        {
            OwningEnemy.rb.linearVelocity = currentVelocity;
        }

       
            Quaternion targetRotation = Quaternion.LookRotation(
                currentVelocity.normalized,
                -CalculateGravityDirection(currentPosition)
            );
            
            executer.transform.rotation = Quaternion.RotateTowards(
                executer.transform.rotation,
                targetRotation,
                OwningEnemy.GetStats().RotSpeed * Time.deltaTime
            );
        
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

    private Vector3 CalculateGravityDirection(Vector3 position)
    {
        if (OwningEnemy == null) return Vector3.down;
        return (position - OwningEnemy.CurrentPlanet.transform.position).normalized;
    }

    private Vector3 GetFlockingForce(Vector3 targetPoint)
    {
        if (!useFlocking || flockingHelper == null || !(OwningEnemy is FlyingEnemyBase)) return Vector3.zero;
        return flockingHelper.CalculateFlockingForce(OwningEnemy as FlyingEnemyBase, targetPoint);
    }

    private void UpdateTargetGenerator()
    {
        lastTargetUpdateTime = Time.time;
        currentTarget = FindNearestGenerator();
    }

    private GeneratorBase FindNearestGenerator()
    {
        GeneratorBase[] generators = UnityEngine.Object.FindObjectsOfType<GeneratorBase>();
        GeneratorBase nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var generator in generators)
        {
            if (generator.IsDestroyed) continue;

            float distance = Vector3.Distance(OwningEnemy.transform.position, generator.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = generator;
            }
        }

        return nearest;
    }

    public GeneratorBase GetCurrentTarget()
    {
        return currentTarget;
    }

    public override void OnStateExit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        currentTarget = null;
    }
}