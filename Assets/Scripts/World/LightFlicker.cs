using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    public float baseIntensity = 2f;
    public float flickerAmount = 0.3f;
    public float flickerSpeed = 5f;

    private Light2D _light;
    private float _seed;

    private void Awake()
    {
        _light = GetComponent<Light2D>();
        _seed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(_seed, Time.time * flickerSpeed);
        _light.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmount;
    }
}
