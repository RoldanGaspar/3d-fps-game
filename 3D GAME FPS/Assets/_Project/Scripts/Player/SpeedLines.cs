using UnityEngine;

/// <summary>
/// Screen-edge speed streaks while moving fast (sprint, slide, wall-run, falling).
/// A single ParticleSystem is parented to the main camera; the emission rate follows the player's speed.
/// </summary>
public class SpeedLines : MonoBehaviour
{
    [SerializeField] private float _startSpeedFactor = 0.65f;
    [SerializeField] private float _maxRate = 70f;
    [SerializeField] private Color _color = new Color(1f, 1f, 1f, 0.22f);

    private PlayerMotor _motor;
    private ParkourStateMachine _parkour;
    private ParticleSystem _particles;
    private ParticleSystem.EmissionModule _emission;
    private Transform _attachedTo;

    private void Start()
    {
        _motor = GetComponent<PlayerMotor>();
        _parkour = GetComponent<ParkourStateMachine>();
    }

    private void OnDestroy()
    {
        if (_particles != null) Destroy(_particles.gameObject);
    }

    private void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || _motor == null) return;

        if (_particles == null || _attachedTo != cam.transform)
            Build(cam.transform);

        float speedFactor = Mathf.InverseLerp(_startSpeedFactor, 1f, _motor.GetNormalizedSpeed());
        string state = _parkour != null ? _parkour.CurrentStateName : "";
        if (state == "SLIDING" || state == "WALL RUN") speedFactor = Mathf.Max(speedFactor, 0.8f);
        if (_motor.Velocity.y < -12f) speedFactor = Mathf.Max(speedFactor, Mathf.InverseLerp(12f, 25f, -_motor.Velocity.y));

        _emission.rateOverTime = _maxRate * speedFactor;
    }

    private void Build(Transform camTransform)
    {
        if (_particles != null) Destroy(_particles.gameObject);

        var go = new GameObject("SpeedLines");
        go.transform.SetParent(camTransform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 14f);
        go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // emit back towards the camera
        _attachedTo = camTransform;

        _particles = go.AddComponent<ParticleSystem>();
        _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = 0.45f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = _color;
        main.maxParticles = 120;

        _emission = _particles.emission;
        _emission.rateOverTime = 0f;

        // a ring around the view axis so the streaks sit at the screen edges
        var shape = _particles.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 5.5f;
        shape.donutRadius = 2.5f;
        shape.rotation = Vector3.zero;
        shape.angle = 0f;

        // fly along the system's local +Z, which points back at the camera
        var velocity = _particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // all three axes must use the same curve mode, or Unity logs an error every frame
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(32f, 44f);

        var renderer = _particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 6f;
        renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var fade = _particles.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        fade.color = grad;

        _particles.Play();
    }
}
