using UnityEngine;

public class AutoDestroyParticle : MonoBehaviour
{
    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (_ps == null) { Destroy(gameObject); return; }
        if (!_ps.IsAlive(true)) Destroy(gameObject);
    }
}