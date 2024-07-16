using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageComponent : MonoBehaviour
{
    [SerializeField]
    private DamageData DamageData;
    // Start is called before the first frame update
    public delegate void ApplyDamage();
}
