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
    [SerializeField] float Altitude = 10f;
    [SerializeField] float targetUpdateInterval = 0.5f;
    
    [Header("Attack Settings")]
    [SerializeField] float attackRange = 15f;
    [SerializeField] float optimalAttackRange = 12f; // Optimal distance to maintain
    [SerializeField] float smoothTime = 0.5f;
    [SerializeField] float maxSpeed = 15f;

    [Header("Flocking Settings")]
    [SerializeField] private bool useFlocking = true;
    [SerializeField] private float cohesionWeight = 0.8f;  // Less cohesion during attack
    [SerializeField] private float separationWeight = 2.5f; // More separation during attack
    [SerializeField] private float alignmentWeight = 0.8f;  // Less alignment during attack
    [SerializeField] private float targetWeight = 2.0f;     // More focus on target during attack
    [SerializeField] private float neighborRadius = 5f;
    [SerializeField] private float separationRadius = 3f;

    private Transform firePoint;
    private ProjectileBase projectilePrefab;
    private float attackTimer;
    private Vector3 currentVelocity;
    private float orbitRadius;
    private GeneratorBase currentTarget;
    private float lastTargetUpdateTime;
    private FlockingHelper flockingHelper;
    private Vector3 velocityChange;

    public override void StateInit(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.StateInit(stateMachine, executer);
    }

    public override void OnStateEnter(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        base.OnStateEnter(stateMachine, executer);
        
        orbitRadius = OwningEnemy.CurrentPlanet.GetPlanetRadius() + Altitude;
        
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
        
        currentVelocity = Vector3.zero;
        attackTimer = 0f;
        
        OwningEnemy.rb.linearVelocity = Vector3.zero;
        
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

        UpdateTargetGenerator();
    }

    public override void OnStateUpdate(FSMC_Controller stateMachine, FSMC_Executer executer)
    {
        if (!OwningEnemy || !executer) return;

        Vector3 currentPosition = executer.transform.position;
        
        if (Time.time - lastTargetUpdateTime >= targetUpdateInterval)
        {
            UpdateTargetGenerator();
        }
        
        if (currentTarget == null || currentTarget.IsDestroyed)
        {
            // Return to movement state if target is lost
            stateMachine.SetBool("TargetLost", true);
            return;
        }

        Vector3 targetPoint = currentTarget.transform.position;
        float distanceToTarget = Vector3.Distance(currentPosition, targetPoint);

        // Calculate optimal attack position (at attack range)
        Vector3 directionToTarget = (targetPoint - currentPosition).normalized;
        Vector3 optimalPosition = targetPoint - directionToTarget * attackRange;

        // Calculate movement
        Vector3 moveDirection = (optimalPosition - currentPosition).normalized;
        Vector3 targetVelocity = moveDirection * OwningEnemy.GetStats().MoveSpeed;

        if (useFlocking && flockingHelper != null)
        {
            // Add flocking force to the target velocity
            Vector3 flockingForce = GetFlockingForce(optimalPosition);
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
        );

        // Update position using rigidbody
        if (OwningEnemy.rb != null)
        {
            OwningEnemy.rb.linearVelocity = currentVelocity;
        }

        // Update rotation to face target
        Quaternion targetRotation = Quaternion.LookRotation(
            directionToTarget,
            -CalculateGravityDirection(currentPosition)
        );
        
        executer.transform.rotation = Quaternion.RotateTowards(
            executer.transform.rotation,
            targetRotation,
            OwningEnemy.GetStats().RotSpeed * Time.deltaTime
        );

        // Attack if in range and facing target
        if (distanceToTarget <= attackRange)
        {
            float angleToTarget = Vector3.Angle(executer.transform.forward, directionToTarget);
            if (angleToTarget < 30f) // Allow some tolerance for attack
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= OwningEnemy.GetStats().attackSpeed)
                {
                    FireAtGenerator();
                    attackTimer = 0f;
                }
            }
        }
    }

    private Vector3 CalculateGravityDirection(Vector3 position)
    {
        if (OwningEnemy == null || OwningEnemy.CurrentPlanet == null) return Vector3.down;
        return -(position - OwningEnemy.CurrentPlanet.transform.position).normalized;
    }

    private Vector3 GetFlockingForce(Vector3 targetPoint)
    {
        if (!useFlocking || flockingHelper == null || !(OwningEnemy is FlyingEnemyBase)) return Vector3.zero;
        return flockingHelper.CalculateFlockingForce(OwningEnemy as FlyingEnemyBase, targetPoint);
    }

    private void FireAtGenerator()
    {
        if (currentTarget == null || firePoint == null || projectilePrefab == null) return;

        Vector3 targetPos = currentTarget.transform.position;
        
        // Spawn and initialize projectile
        ProjectileBase projectile = UnityEngine.Object.Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation
        );

        projectile.Initialize(
            OwningEnemy.GetStats().attackDamage,
            targetPos,
            OwningEnemy.GetStats().ProjectileSpeed
        );
        
        projectile.ShootProjectile(targetPos, currentTarget.gameObject);
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