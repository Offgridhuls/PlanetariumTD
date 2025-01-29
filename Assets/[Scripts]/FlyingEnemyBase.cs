using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemyBase : EnemyBase
{
    [Header("Flying Enemy Components")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected ProjectileBase projectilePrefab;

    private float heightVariation = 2f;
    private float heightChangeSpeed = 1f;
    private float currentHeight;
    private float targetHeight;
    private float heightTimer;

    protected override void Start()
    {
        base.Start();
        currentHeight = transform.position.y;
        targetHeight = currentHeight + Random.Range(-heightVariation, heightVariation);

        // Ensure we have a fire point
        if (!firePoint)
        {
            // Create a fire point if it doesn't exist
            GameObject firePointObj = new GameObject("FirePoint");
            firePoint = firePointObj.transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = Vector3.forward; // Adjust position as needed
        }
    }

    public Transform GetFirePoint() => firePoint;
    public ProjectileBase GetProjectilePrefab() => projectilePrefab;

    public void SetFlightParameters(float variation, float speed)
    {
        heightVariation = variation;
        heightChangeSpeed = speed;
    }

    public Vector3 GetCurrentTargetPosition()
    {
        // Try to get target from current behavior state
        var moveState = StateMachine.GetState("MoveToGenerator")?.GetBehaviour() as MoveToNearestGenerator;
        if (moveState != null && StateMachine.GetState("MoveToGenerator").IsActive)
        {
            var target = moveState.GetCurrentTarget();
            return target != null ? target.transform.position : Vector3.zero;
        }

        var attackState = StateMachine.GetState("RangedAttackGenerator")?.GetBehaviour() as RangedAttackGenerator;
        if (attackState != null && StateMachine.GetState("RangedAttackGenerator").IsActive)
        {
            var target = attackState.GetCurrentTarget();
           
        }

        return target != null ? target.transform.position : Vector3.zero;
    }

    protected override void Update()
    {
        if (!IsAlive) return;

        base.Update();

        // Update height
        heightTimer += Time.deltaTime;
        if (heightTimer >= heightChangeSpeed)
        {
            targetHeight = currentHeight + Random.Range(-heightVariation, heightVariation);
            heightTimer = 0f;
        }

        // Smoothly interpolate to target height
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightChangeSpeed);
        //transform.position = position;
    }
}
