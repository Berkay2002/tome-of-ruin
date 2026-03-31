using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Shake(float intensity)
    {
        // Stub — will be fully implemented in Task 24
    }
}
