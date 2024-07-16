using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
struct TurretStats
{
    [Header("Turret Stats")]
    private string Name;
    [SerializeField]
    private float HealthPoints;
    [SerializeField]
    private int Damage;
    [SerializeField]
    private float AgroRadius;
    [SerializeField]
    private float FireInterval;

    [Header("Resources")]
    [SerializeField]
    private int ScrapCost;
    [SerializeField]
    private int CoinCost;
    public float GetHealth()
    {
        return HealthPoints;
    }
    public int GetDamage()
    {
        return Damage;
    }
    public float GetAgroRadius()
    {
        return AgroRadius;
    }
    public float GetFireInterval()
    {
        return FireInterval;
    }
    public string GetName()
    {
        return Name; 
    }
}