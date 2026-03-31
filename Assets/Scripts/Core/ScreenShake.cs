using UnityEngine;
using Cinemachine;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    private CinemachineImpulseSource _impulse;

    private void Awake()
    {
        Instance = this;
        _impulse = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake(float force = 1f)
    {
        if (_impulse != null)
            _impulse.GenerateImpulse(force);
    }
}
