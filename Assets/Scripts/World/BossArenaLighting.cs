using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BossArenaLighting : MonoBehaviour
{
    [Header("Global Light")]
    public Light2D globalLight;
    public float startIntensity = 0.2f;
    public float endIntensity = 0.4f;
    public Color endColor = new Color(0.3f, 0.05f, 0.05f);

    [Header("Perimeter Lights")]
    public Light2D[] perimeterLights;
    public float perimeterTargetIntensity = 4f;

    [Header("Transition")]
    public float transitionDuration = 2f;

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;
        StartCoroutine(TransitionLighting());
    }

    private IEnumerator TransitionLighting()
    {
        Color startColor = globalLight.color;
        float elapsed = 0f;

        foreach (var light in perimeterLights)
        {
            if (light != null)
            {
                light.enabled = true;
                light.intensity = 0f;
            }
        }

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            globalLight.intensity = Mathf.Lerp(startIntensity, endIntensity, smooth);
            globalLight.color = Color.Lerp(startColor, endColor, smooth);

            foreach (var light in perimeterLights)
            {
                if (light != null)
                    light.intensity = Mathf.Lerp(0f, perimeterTargetIntensity, smooth);
            }

            yield return null;
        }

        globalLight.intensity = endIntensity;
        globalLight.color = endColor;
        foreach (var light in perimeterLights)
        {
            if (light != null)
                light.intensity = perimeterTargetIntensity;
        }
    }
}
