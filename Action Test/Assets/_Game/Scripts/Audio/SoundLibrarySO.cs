using UnityEngine;
using UnityEngine.Audio;

public enum SfxId
{
    RifleShot,
    RifleBolt,
    RifleReload,

    EnemyHurt,
    EnemyDeath,

    Footstep
}

[CreateAssetMenu(menuName = "Arena/Audio/Sound Library", fileName = "SoundLibrary_")]
public class SoundLibrarySO : ScriptableObject
{
    [Header("Mixer")]
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup uiGroup;

    [Header("SFX Clips")]
    public ClipSet rifleShot;
    public ClipSet rifleBolt;
    public ClipSet rifleReload;

    public ClipSet enemyHurt;
    public ClipSet enemyDeath;

    public ClipSet footstep;

    [Header("Music")]
    public AudioClip[] musicTracks;

    [System.Serializable]
    public class ClipSet
    {
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitchMin = 0.95f;
        [Range(0.5f, 2f)] public float pitchMax = 1.05f;

        public AudioClip PickRandom()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float PickPitch() => Random.Range(pitchMin, pitchMax);
    }
}