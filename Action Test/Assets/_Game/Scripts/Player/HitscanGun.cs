using UnityEngine;
using UnityEngine.InputSystem;

public class HitscanGun : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private DoomFpsController player;

    [Header("Gun")]
    [SerializeField] private float range = 60f;
    [SerializeField] private int damage = 3;
    [SerializeField] private float fireRate = 8f; // shots / second
    [SerializeField] private LayerMask hitMask = ~0;
    
    private ArenaInput _input;
    private bool _fireHeld;
    private float _nextShotTime;

    private void Awake()
    {
        if (!player) player = GetComponentInParent<DoomFpsController>();
        if (!cam) cam = GetComponentInChildren<Camera>();

        _input = new ArenaInput();
    }

    private void OnEnable()
    {
        _input.Enable();
        _input.Player.Fire.performed += OnFire;
        _input.Player.Fire.canceled += OnFire;
    }

    private void OnDisable()
    {
        _input.Player.Fire.performed -= OnFire;
        _input.Player.Fire.canceled -= OnFire;
        _input.Disable();
    }

    private void OnFire(InputAction.CallbackContext ctx) => _fireHeld = ctx.ReadValueAsButton();

    private void Update()
    {
        if (!_fireHeld) return;
        if (Time.time < _nextShotTime) return;

        _nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        Shoot();
    }

    private void Shoot()
    {
        if (!cam || player == null) return;

        Ray r = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(r, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            return;

        var h = hit.collider.GetComponentInParent<Health>();
        if (h == null) return;

        h.TakeDamage(damage, player.PlayerId);
    }
}