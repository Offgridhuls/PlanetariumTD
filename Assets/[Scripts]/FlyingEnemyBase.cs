using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Planetarium.AI;
using Planetarium.Stats;

public class FlyingEnemyBase : EnemyBase
{
    [Header("Flying Enemy Components")]
    [SerializeField] protected Transform firePoint;

    private float heightVariation = 2f;
    private float heightChangeSpeed = 1f;
    private float currentHeight;
    private float targetHeight;

    protected override void Awake()
    {
        base.Awake();
        
        taggedComponent = GetComponent<TaggedComponent>();
        if (taggedComponent == null)
        {
            taggedComponent = gameObject.AddComponent<TaggedComponent>();
        }
        
        taggedComponent.AddTag(CachedTags.EnemyFlying);
    }

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


    protected override void Update()
    {
        if (!IsAlive) return;

        base.Update();

        // Update height
        if (currentHeight == targetHeight)
        {
            targetHeight = transform.position.y + Random.Range(-heightVariation, heightVariation);
        }

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightChangeSpeed);
        //transform.position = position;
    }

    // Removed manual state registration
}
