using UnityEngine;
using DG.Tweening;

public class Stunnable : MonoBehaviour
{
    [SerializeField] private float stunScalePunch = 0.10f;
    [SerializeField] private float stunScalePunchDuration = 0.15f;

    public bool IsStunned => Time.time < _stunEndTime;

    private float _stunEndTime;
    private Tween _tween;
    private Vector3 _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    public void Stun(float duration)
    {
        _stunEndTime = Mathf.Max(_stunEndTime, Time.time + Mathf.Max(0f, duration));

        // tiny juice
        _tween?.Kill();
        transform.localScale = _baseScale;
        _tween = transform.DOPunchScale(Vector3.one * stunScalePunch, stunScalePunchDuration, 8, 0.6f);
    }
}