using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Planetarium;

[RequireComponent(typeof(Rigidbody))]
public class PlayerSphericalMovementComponent : MonoBehaviour
{
    [SerializeField]
    Transform playerInputSpace = default;

    public Animator playerAnimator;

    [Header("Movement Settings")]
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 40f, maxStairsAngle = 65f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 4f;

    [SerializeField, Range(0, 4)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 100f)]
    float maxSnapSpeed = 100f;

    [SerializeField, Min(0f)]
    float probeDistance = 1f;

    [SerializeField]
    LayerMask probeMask = -1, stairsMask = -1;

    //movement
    Rigidbody rb;
    Vector3 velocity, desiredVelocity, contactNormal, steepNormal;
    float minGroundDotProduct, minStairsDotProduct;
    int jumpPhase, groundContactCount, steepContactCount, framesSinceLastGrounded, framesSinceLastJump;
    bool desiredJump, IsRunning;
    bool IsGrounded => groundContactCount > 0;
    bool OnSteep => steepContactCount > 0;
    //custom axes
    Vector3 upAxis, rightAxis, forwardAxis;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        OnValidate();
    }

    void Update() // use update to get user input
    {
        //movement
        Vector2 playerInput = Vector2.zero;
        playerInput.x = Input.GetAxisRaw("Horizontal");
        playerInput.y = Input.GetAxisRaw("Vertical");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        if (playerInputSpace)
        {
            rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
        }
        else
        {
            rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
            forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
        }
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

        playerAnimator.SetBool("isRunning", IsRunning);

        if (playerInput.x > 0 || playerInput.x < 0 ||  playerInput.y > 0 || playerInput.y < 0)
        {
            IsRunning = true;
        }
        else
        {
            IsRunning = false;
        }

        if (IsRunning)
            transform.rotation = Quaternion.LookRotation((forwardAxis * playerInput.y) + (playerInput.x * rightAxis), upAxis);
        
        //jump
        desiredJump |= Input.GetButtonDown("Jump");
    }

    private void FixedUpdate() // process the physics in fixed update
    {
        //gravity
        Vector3 gravity = CustomGravity.GetGravity(rb.position, out upAxis);
        //movement
        UpdateState();
        AdjustVelocity();
        //jump
        if (desiredJump)
        {
            desiredJump = false;
            Jump(gravity);
        }

        velocity += gravity * Time.deltaTime;
        //Debug.Log("Current Gravity: " + gravity.ToString());
        rb.linearVelocity = velocity; // update rb velocity with local velocity value
        ClearState();
    }

    private void UpdateState()
    {
        framesSinceLastGrounded += 1;
        framesSinceLastJump += 1;
        velocity = rb.linearVelocity; //reset local velocity variable
        if (IsGrounded || SnapToGround())
        {
            framesSinceLastGrounded = 0;
            if (framesSinceLastJump > 1)
            {
                jumpPhase = 0;
            }
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = upAxis;
        }
    }

    private void ClearState()
    {
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }

    private void AdjustVelocity()
    {
        Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
        Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = IsGrounded ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    private void Jump(Vector3 gravity)
    {
        Vector3 jumpDirection;
        if (IsGrounded)
        {
            jumpDirection = contactNormal;
        }
        else if (OnSteep)
        {
            jumpDirection = steepNormal;
            jumpPhase = 0;
        }
        else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
        {
            if (jumpPhase == 0)
            {
                jumpPhase = 1;
            }
            jumpDirection = contactNormal;
        }
        else
        {
            return;
        }
        jumpPhase += 1;
        framesSinceLastJump = 0;
        float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
        jumpDirection = (jumpDirection + upAxis).normalized;
        float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
        if (alignedSpeed > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
        }
        velocity += jumpDirection * jumpSpeed;
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision) // count all ground contacts and update ground normal
    {
        float minDot = GetMinDot(collision.gameObject.layer);
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;
            float upDot = Vector3.Dot(upAxis, normal);
            if (upDot >= minDot)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
            else if (upDot > -0.01f)
            {
                steepContactCount += 1;
                steepNormal += normal;
            }
        }
    }

    bool CheckSteepContacts()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            float upDot = Vector3.Dot(upAxis, steepNormal);
            if (upDot >= minGroundDotProduct)
            {
                groundContactCount = 1;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    private bool SnapToGround()
    {
        if (framesSinceLastGrounded > 1 || framesSinceLastJump <= 2)
        {
            return false;
        }
        float speed = velocity.magnitude;
        if (speed > maxSnapSpeed)
        {
            return false;
        }
        if (!Physics.Raycast(rb.position, -upAxis, out RaycastHit hit, probeDistance, probeMask))
        {
            return false;
        }
        float upDot = Vector3.Dot(upAxis, hit.normal);
        if (upDot < GetMinDot(hit.collider.gameObject.layer))
        {
            return false;
        }
        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0f)
        {
            velocity = (velocity - hit.normal * dot).normalized * speed;
        }
        return true;
    }

    private void OnValidate() // determine valid ground slopes
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
    }

    Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
    {
        return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }

    float GetMinDot(int layer)
    {
        return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
    }
}
