using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum DamageType
{
    Normal
}

[System.Serializable]
public struct DamageData
{
    public float Damage;
    public GameObject Source;
    public DamageType Type;
    public float DamageMultiplier;

    public DamageData(float damage, GameObject source, DamageType type = DamageType.Normal, float multiplier = 1f)
    {
        Damage = damage;
        Source = source;
        Type = type;
        DamageMultiplier = multiplier;
    }

    public float GetFinalDamage()
    {
        return Damage * DamageMultiplier;
    }
}
