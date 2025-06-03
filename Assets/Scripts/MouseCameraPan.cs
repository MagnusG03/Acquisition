using UnityEngine;
using UnityEngine.InputSystem;

public class MouseCameraPan : MonoBehaviour
{
    private float maxYawAngle = 3f;
    private float maxPitchAngle = 2f;
    private float smoothTime = 0.2f;
    private Vector3 _initialEuler;
    private float _currentYawOffset;
    private float _currentPitchOffset;
    private float _velocityYawOffset;
    private float _velocityPitchOffset;

    void Start()
    {
        _initialEuler = transform.localEulerAngles;
        _currentYawOffset = 0f;
        _currentPitchOffset = 0f;
    }

    void Update()
    {
        Vector2 rawMousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            rawMousePos = Mouse.current.position.ReadValue();
        }

        float normX = (rawMousePos.x / Screen.width) * 2f - 1f;
        float normY = (rawMousePos.y / Screen.height) * 2f - 1f;

        normX = Mathf.Clamp(normX, -1f, 1f);
        normY = Mathf.Clamp(normY, -1f, 1f);

        float targetYawOffset = normX * maxYawAngle;
        float targetPitchOffset = -normY * maxPitchAngle;

        _currentYawOffset = Mathf.SmoothDamp(
            _currentYawOffset,
            targetYawOffset,
            ref _velocityYawOffset,
            smoothTime
        );

        _currentPitchOffset = Mathf.SmoothDamp(
            _currentPitchOffset,
            targetPitchOffset,
            ref _velocityPitchOffset,
            smoothTime
        );

        transform.localEulerAngles = new Vector3(
            _initialEuler.x + _currentPitchOffset,
            _initialEuler.y + _currentYawOffset,
            _initialEuler.z
        );
    }
}
