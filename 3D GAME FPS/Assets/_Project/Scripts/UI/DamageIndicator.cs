using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Directional damage indicator that shows where damage came from.
/// </summary>
public class DamageIndicator : MonoBehaviour
{
    [SerializeField] private Image _indicatorImage;
    [SerializeField] private float _displayDuration = 1.5f;
    [SerializeField] private float _fadeSpeed = 2f;

    private Transform _playerTransform;
    private Vector3 _damageSource;
    private float _timer;

    public void ShowDamage(Vector3 sourcePosition, Transform player)
    {
        _damageSource = sourcePosition;
        _playerTransform = player;
        _timer = _displayDuration;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_timer <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }
        _timer -= Time.deltaTime;
        // TODO: Rotate indicator to face damage source, fade out
    }
}
