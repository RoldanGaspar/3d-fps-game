using UnityEngine;

/// <summary>
/// Puts the character in the main menu into a relaxed rifle-ready idle and slowly turns it, so the menu
/// has a living 3D backdrop. Uses the same animator controller as the gameplay character.
/// </summary>
public class MenuCharacterPose : MonoBehaviour
{
    [SerializeField] private float _turnSpeed = 6f;
    [SerializeField] private float _swayDegrees = 14f;

    private Animator _animator;
    private float _baseYaw;

    private void Start()
    {
        var character = GameObject.Find("MenuCharacter");
        if (character == null) return;

        _animator = character.GetComponent<Animator>();
        _baseYaw = character.transform.eulerAngles.y;
        if (_animator == null) return;

        _animator.SetBool("IsGrounded", true);
        _animator.SetBool("IsAirborne", false);
        _animator.SetInteger("WeaponType", 1);
        _animator.SetFloat("Speed", 0f);
    }

    private void Update()
    {
        if (_animator == null) return;
        // gentle left-right sway of the model for some life
        float yaw = _baseYaw + Mathf.Sin(Time.unscaledTime * 0.35f) * _swayDegrees;
        _animator.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
