using UnityEngine;

public enum WeakSpotType
{
    Normal,
    Headshot,
    StunSpot
}

public class WeakSpot : MonoBehaviour
{
    [Header("Weak Spot")]
    public WeakSpotType type = WeakSpotType.Normal;

    [Tooltip("Damage multiplier applied to bullet base damage.")]
    public float damageMultiplier = 2.0f;

    [Tooltip("If true, a hit here instantly kills (optional).")]
    public bool instantKill = false;

    [Header("Stun (only used for StunSpot)")]

    public float stunDuration = 0.8f;
}