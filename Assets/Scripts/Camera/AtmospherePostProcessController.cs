using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(PostProcessVolume))]
public class AtmospherePostProcessController : MonoBehaviour
{
    [Header("References")]
    public AtmosphereSettings atmosphereSettings;
    public Transform sun;
    public float planetRadius = 100f;

    [Header("Integration")]
    public PostProcessVolume postProcessVolume;
    private PostProcessing atmospherePostProcess;
    private Camera mainCamera;

    private void OnEnable()
    {
        if (!postProcessVolume)
            postProcessVolume = GetComponent<PostProcessVolume>();

        mainCamera = GetComponent<Camera>();
        if (!mainCamera)
            mainCamera = Camera.main;

        // Ensure we have the atmosphere post process component
        if (!atmospherePostProcess)
        {
            var go = new GameObject("Atmosphere Post Process");
            go.transform.parent = transform;
            atmospherePostProcess = go.AddComponent<PostProcessing>();
        }

        // Setup the atmosphere post process
        atmospherePostProcess.atmosphere = atmosphereSettings;
        atmospherePostProcess.sun = sun;
        atmospherePostProcess.planetRadius = planetRadius;

        // Enable depth texture for proper atmosphere rendering
        mainCamera.depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnValidate()
    {
        if (!postProcessVolume)
            postProcessVolume = GetComponent<PostProcessVolume>();
    }

    private void Update()
    {
        // Update any runtime parameters here if needed
        if (atmospherePostProcess && atmosphereSettings)
        {
            atmospherePostProcess.planetRadius = planetRadius;
        }
    }
}
