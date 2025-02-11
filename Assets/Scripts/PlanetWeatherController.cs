using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PlanetWeatherController : MonoBehaviour
{
    [Header("Planet Settings")]
    public Transform planetCenter;
    public float planetRadius = 100f;

    [Header("Weather Settings")]
    [Range(0f, 1f)]
    public float weatherIntensity = 0.5f;
    public float rotationSpeed = 1f;
    [Range(1.0f, 1.5f)]
    public float weatherHeight = 1.1f;
    [Range(0f, 1f)]
    public float weatherBandWidth = 0.1f;
    public Color weatherColor = new Color(1, 1, 1, 0.5f);
    public Texture2D weatherPattern;

    private Material weatherMaterial;
    private MeshRenderer meshRenderer;
    private static readonly int WeatherIntensityID = Shader.PropertyToID("_WeatherIntensity");
    private static readonly int RotationSpeedID = Shader.PropertyToID("_RotationSpeed");
    private static readonly int WeatherHeightID = Shader.PropertyToID("_WeatherHeight");
    private static readonly int WeatherBandWidthID = Shader.PropertyToID("_WeatherBandWidth");
    private static readonly int WeatherColorID = Shader.PropertyToID("_WeatherColor");
    private static readonly int WeatherTexID = Shader.PropertyToID("_WeatherTex");
    private static readonly int PlanetCenterID = Shader.PropertyToID("_PlanetCenter");
    private static readonly int PlanetRadiusID = Shader.PropertyToID("_PlanetRadius");

    void OnEnable()
    {
        // Get the renderer
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Create a new material instance
        if (weatherMaterial == null)
        {
            var shader = Shader.Find("Custom/PlanetWeather");
            if (shader == null)
            {
                Debug.LogError("Could not find Custom/PlanetWeather shader!");
                return;
            }
            weatherMaterial = new Material(shader);
            meshRenderer.material = weatherMaterial;
        }

        if (!planetCenter)
        {
            planetCenter = transform;
            Debug.LogWarning("No planet center assigned, using this object's transform.");
        }

        // Initial setup of material properties
        UpdateMaterialProperties();
    }   

    void Update()
    {
        UpdateMaterialProperties();
    }

    void UpdateMaterialProperties()
    {
        if (weatherMaterial != null)
        {
            weatherMaterial.SetFloat(WeatherIntensityID, weatherIntensity);
            weatherMaterial.SetFloat(RotationSpeedID, rotationSpeed);
            weatherMaterial.SetFloat(WeatherHeightID, weatherHeight);
            weatherMaterial.SetFloat(WeatherBandWidthID, weatherBandWidth);
            weatherMaterial.SetColor(WeatherColorID, weatherColor);
            
            if (weatherPattern != null)
            {
                weatherMaterial.SetTexture(WeatherTexID, weatherPattern);
            }
            
            if (planetCenter != null)
            {
                weatherMaterial.SetVector(PlanetCenterID, planetCenter.position);
                weatherMaterial.SetFloat(PlanetRadiusID, planetRadius);
            }
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && weatherMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }

    void OnDisable()
    {
        if (weatherMaterial != null)
        {
            DestroyImmediate(weatherMaterial);
        }
    }
}
