using UnityEngine;

/// <summary>
/// Camera and weapon kickback for procedural weapon recoil.
/// Safely blends positional kickback and angular pitch/yaw without drifting.
/// </summary>
public class WeaponRecoil : MonoBehaviour
{
    [Header("Recoil Angles")]
    [SerializeField] private float _recoilX = -2.5f; // Upward pitch
    [SerializeField] private float _recoilY = 1.0f;  // Yaw wobble
    [SerializeField] private float _recoilZ = 0.5f;  // Roll wobble

    [Header("Kickback Settings")]
    [SerializeField] private float _kickbackZ = -0.06f; // Backwards punch
    [SerializeField] private float _snapSpeed = 18f;
    [SerializeField] private float _returnSpeed = 12f;

    private Vector3 _currentRotation;
    private Vector3 _targetRotation;
    private Vector3 _currentPosition;
    private Vector3 _targetPosition;

    private Vector3 _initialLocalPos;
    private Quaternion _initialLocalRot;

    private void Start()
    {
        _initialLocalPos = transform.localPosition;
        _initialLocalRot = transform.localRotation;
    }

    private void Update()
    {
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, _returnSpeed * Time.deltaTime);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, _snapSpeed * Time.deltaTime);
        transform.localRotation = _initialLocalRot * Quaternion.Euler(_currentRotation);

        _targetPosition = Vector3.Lerp(_targetPosition, Vector3.zero, _returnSpeed * Time.deltaTime);
        _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, _snapSpeed * Time.deltaTime);
        transform.localPosition = _initialLocalPos + _currentPosition;
    }

    public void ApplyRecoil(float multiplier = 1f)
    {
        _targetRotation += new Vector3(_recoilX, Random.Range(-_recoilY, _recoilY), Random.Range(-_recoilZ, _recoilZ)) * multiplier;
        _targetPosition += new Vector3(0f, 0f, _kickbackZ) * multiplier;
    }

    public void SetBaseTransform(Vector3 pos, Quaternion rot)
    {
        _initialLocalPos = pos;
        _initialLocalRot = rot;
    }
}
