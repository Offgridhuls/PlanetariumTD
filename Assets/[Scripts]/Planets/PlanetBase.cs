using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetBase : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public float GetPlanetRadius()
    {
        return transform.localScale.x / 2;
    }
}
