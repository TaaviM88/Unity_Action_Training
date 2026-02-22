using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSfx : MonoBehaviour
{
    [SerializeField] private Transform footPos;
    [SerializeField] private float walkStepInterval = 0.42f;
    [SerializeField] private float runStepInterval = 0.30f;
    [SerializeField] private float minSpeedForSteps = 1.2f;

    [SerializeField] private float maxDistance = 18f;

    private CharacterController _cc;
    private float _nextStepTime;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (!footPos) footPos = transform;
    }

    private void Update()
    {
        if (AudioManager.Instance == null) return;
        if (!_cc.isGrounded) return;

        Vector3 v = _cc.velocity;
        v.y = 0f;

        float speed = v.magnitude;
        if (speed < minSpeedForSteps) return;

        float interval = (speed > 6.5f) ? runStepInterval : walkStepInterval;

        if (Time.time < _nextStepTime) return;
        _nextStepTime = Time.time + interval;

        AudioManager.Instance.PlaySfx3D(AudioManager.Instance.Library.footstep, footPos.position, 0.9f, 1.5f, maxDistance);
    }
}