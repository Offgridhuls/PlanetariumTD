using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PostProcessing : CustomImageEffect
{
    public AtmosphereSettings atmosphere;
    public Transform sun;
    public float planetRadius;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        bool initMat = false;
        
        if (material == null)
        {
            shader = atmosphere.atmosphereShader;
            material = GetMaterial();
            initMat = true;
        }
        
        if (active && material != null)
        {
            atmosphere.SetProperties(material, planetRadius, Vector3.zero, -sun.forward, initMat);
            Graphics.Blit(source, destination, material);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    public override void Release()
    {
        base.Release();
        if (material != null)
        {
            DestroyImmediate(material);
            material = null;
        }
    }

    private void OnDisable()
    {
        Release();
    }
}
