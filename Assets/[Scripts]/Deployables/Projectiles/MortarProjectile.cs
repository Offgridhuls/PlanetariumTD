using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
public class MortarProjectile : ProjectileBase
{
    PlanetBase Planet;
    public float arcHeight = 5f;
    private Vector3 startPoint;
    private Vector3 endPoint;

    private float journeyLength;
    private float journeyTime;
    public override void ShootProjectile(Vector3 target, GameObject enemyObject)
    {
        Planet = FindFirstObjectByType<PlanetBase>();
        Vector3 randomPoint = GetRandomPointOnSphere(Planet.GetPlanetRadius());
        startPoint = transform.position;
        endPoint = randomPoint;
        journeyLength = Vector3.Distance(startPoint, endPoint);
        journeyTime = 0f;
    }
    public override void Update()
    {
        if (journeyTime <= 1f)
        {
            journeyTime += Time.deltaTime * GetProjectileSpeed() / journeyLength;

            // Interpolate position along the sphere's surface
            Vector3 interpolatedPoint = Vector3.Slerp(startPoint, endPoint, journeyTime);

            // Add arc height
            float height = Mathf.Sin(journeyTime * Mathf.PI) * arcHeight;
            Vector3 direction = (interpolatedPoint - Planet.transform.position).normalized;
            transform.position = interpolatedPoint + direction * height;


            if (journeyTime < 1f)
            {
                Vector3 nextInterpolatedPoint = Vector3.Slerp(startPoint, endPoint, journeyTime + 0.01f); // Small step ahead
                float nextHeight = Mathf.Sin((journeyTime + 0.01f) * Mathf.PI) * arcHeight;
                Vector3 nextDirection = (nextInterpolatedPoint - Planet.transform.position).normalized;
                Vector3 nextPosition = nextInterpolatedPoint + nextDirection * nextHeight;

                Vector3 forwardDirection = (nextPosition - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(forwardDirection, direction); // Face forward
            }
        }
    }
    Vector3 GetRandomPointOnSphere(float radius)
    {
        // Generate random spherical coordinates
        float theta = Random.Range(0f, Mathf.PI * 2); // Angle around the equator
        float phi = Mathf.Acos(Random.Range(-1f, 1f)); // Angle from the pole

        // Convert spherical coordinates to Cartesian coordinates
        float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
        float y = radius * Mathf.Sin(phi) * Mathf.Sin(theta);
        float z = radius * Mathf.Cos(phi);

        return new Vector3(x, y, z);
    }
    public override void OnProjectileHit()
    {

    }
}