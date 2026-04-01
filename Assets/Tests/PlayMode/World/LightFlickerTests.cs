using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering.Universal;

public class LightFlickerTests
{
    [UnityTest]
    public IEnumerator LightFlicker_ChangesIntensityOverTime()
    {
        var go = new GameObject("TestLight");
        var light = go.AddComponent<Light2D>();
        light.intensity = 2f;

        var flicker = go.AddComponent<LightFlicker>();
        flicker.baseIntensity = 2f;
        flicker.flickerAmount = 0.5f;
        flicker.flickerSpeed = 10f;

        yield return new WaitForSeconds(0.2f);

        Assert.AreNotEqual(2f, light.intensity, 0.01f);

        Object.DestroyImmediate(go);
    }
}
