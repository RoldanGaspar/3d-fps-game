using UnityEngine;

/// <summary>
/// Projectile behavior: moves forward, deals damage on impact, auto-destroys.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private GameObject _impactVFX;

    private void Start()
    {
        Destroy(gameObject, _lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // TODO: Apply damage if target has health component
        // TODO: Spawn impact VFX
        Destroy(gameObject);
    }
}
