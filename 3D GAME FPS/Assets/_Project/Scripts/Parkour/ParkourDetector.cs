using UnityEngine;

/// <summary>
/// Performs raycasts to detect walls, ledges, and vaultable obstacles
/// for the parkour system.
/// </summary>
public class ParkourDetector : MonoBehaviour
{
    [Header("Wall Detection")]
    [SerializeField] private float _wallCheckDistance = 1f;
    [SerializeField] private LayerMask _wallMask;

    [Header("Vault Detection")]
    [SerializeField] private float _vaultCheckDistance = 1.5f;
    [SerializeField] private float _vaultMaxHeight = 1.2f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("Mantle Detection")]
    [SerializeField] private float _mantleCheckDistance = 1f;
    [SerializeField] private float _mantleMaxHeight = 2.5f;

    [Header("Ground Check")]
    [SerializeField] private float _groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask _groundMask;

    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_wallMask == 0) _wallMask = LayerMask.GetMask("Wall");
        if (_obstacleMask == 0) _obstacleMask = LayerMask.GetMask("Obstacle");
        if (_groundMask == 0) _groundMask = LayerMask.GetMask("Ground");
    }

    private float GetFeetY()
    {
        if (_characterController != null)
        {
            return transform.position.y + _characterController.center.y - (_characterController.height * 0.5f);
        }
        return transform.position.y - 1.0f;
    }

    public bool CheckWallLeft(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(origin, -transform.right, out hit, _wallCheckDistance))
        {
            if (((1 << hit.collider.gameObject.layer) & _wallMask) != 0 || hit.collider.CompareTag("WallRunnable"))
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckWallRight(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(origin, transform.right, out hit, _wallCheckDistance))
        {
            if (((1 << hit.collider.gameObject.layer) & _wallMask) != 0 || hit.collider.CompareTag("WallRunnable"))
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckObstacle(out RaycastHit frontHit, out RaycastHit topHit, out float obstacleHeight, out bool isVault, out bool isMantle)
    {
        frontHit = default;
        topHit = default;
        obstacleHeight = 0f;
        isVault = false;
        isMantle = false;

        float feetY = GetFeetY();

        // Check at knee level (0.5m above feet) and chest level (1.3m above feet)
        Vector3 kneeOrigin = new Vector3(transform.position.x, feetY + 0.5f, transform.position.z);
        Vector3 chestOrigin = new Vector3(transform.position.x, feetY + 1.3f, transform.position.z);

        bool hasHit = Physics.Raycast(kneeOrigin, transform.forward, out frontHit, _vaultCheckDistance)
                   || Physics.Raycast(chestOrigin, transform.forward, out frontHit, _vaultCheckDistance);

        if (hasHit)
        {
            bool isObstacle = ((1 << frontHit.collider.gameObject.layer) & _obstacleMask) != 0 
                || frontHit.collider.CompareTag("Vaultable") 
                || frontHit.collider.CompareTag("Mantleable");

            if (isObstacle)
            {
                Vector3 topRayStart = frontHit.point + transform.forward * 0.25f + Vector3.up * (_mantleMaxHeight + 0.2f);
                if (Physics.Raycast(topRayStart, Vector3.down, out topHit, _mantleMaxHeight + 0.5f))
                {
                    obstacleHeight = topHit.point.y - feetY;
                    if (obstacleHeight >= 0.3f && obstacleHeight <= _vaultMaxHeight)
                    {
                        isVault = true;
                        return true;
                    }
                    else if (obstacleHeight > _vaultMaxHeight && obstacleHeight <= _mantleMaxHeight)
                    {
                        isMantle = true;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool CheckVault(out RaycastHit hit)
    {
        return CheckObstacle(out hit, out _, out _, out bool isVault, out _) && isVault;
    }

    public bool CheckMantle(out RaycastHit hit)
    {
        return CheckObstacle(out hit, out _, out _, out _, out bool isMantle) && isMantle;
    }

    public bool IsGrounded()
    {
        return Physics.CheckSphere(transform.position, _groundCheckDistance, _groundMask);
    }
}