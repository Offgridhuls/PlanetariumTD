using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemyBase : EnemyBase
{
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
    }

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
        heightTimer += Time.deltaTime;
        if (heightTimer >= heightChangeSpeed)
        {
            targetHeight = currentHeight + Random.Range(-heightVariation, heightVariation);
            heightTimer = 0f;
        }

        // Smoothly interpolate to target height
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * heightChangeSpeed);
        Vector3 position = transform.position;
        position.y = currentHeight;
        transform.position = position;
    }
}
