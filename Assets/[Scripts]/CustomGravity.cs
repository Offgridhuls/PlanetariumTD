using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomGravity
{
    public static Vector3 GetUpAxis(Vector3 position)
    {
        return position;
    }

    public static Vector3 GetGravity(Vector3 position)
    {
        return position * Physics.gravity.y;
    }

    public static Vector3 GetGravity(Vector3 position, out Vector3 upAxis)
    {
        upAxis = position.normalized;

        return upAxis * Physics.gravity.y;
    }
}
