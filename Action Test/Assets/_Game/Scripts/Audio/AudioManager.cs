using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Library")]
    [SerializeField] private SoundLibrarySO library;

    [Header("Sources")]
    [SerializeField] private int sfxPoolSize = 16;

    private AudioSource _musicSource;
    private AudioSource[] _sfxPool;
    private int _sfxIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Music source
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.playOnAwake = false;
        _musicSource.spatialBlend = 0f;
        if (library && library.musicGroup) _musicSource.outputAudioMixerGroup = library.musicGroup;

        // SFX pool (2D/3D one-shots)
        _sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 1f; // default 3D; we set to 0 for UI/2D calls
            if (library && library.sfxGroup) s.outputAudioMixerGroup = library.sfxGroup;
            _sfxPool[i] = s;
        }
    }

    // -------- Music --------
    public void PlayMusic(AudioClip clip, float volume = 0.8f)
    {
        if (!clip) return;
        _musicSource.clip = clip;
        _musicSource.volume = volume;
        _musicSource.Play();
    }

    public void PlayRandomMusic(float volume = 0.8f)
    {
        if (!library || library.musicTracks == null || library.musicTracks.Length == 0) return;
        PlayMusic(library.musicTracks[Random.Range(0, library.musicTracks.Length)], volume);
    }

    public void StopMusic() => _musicSource.Stop();

    // -------- SFX --------
    private AudioSource NextSfx()
    {
        _sfxIndex = (_sfxIndex + 1) % _sfxPool.Length;
        return _sfxPool[_sfxIndex];
    }

    public void PlaySfx3D(SoundLibrarySO.ClipSet set, Vector3 pos, float volumeMult = 1f, float minDist = 2f, float maxDist = 25f)
    {
        if (set == null) return;
        var clip = set.PickRandom();
        if (!clip) return;

        var s = NextSfx();
        s.transform.position = pos;

        s.spatialBlend = 1f;
        s.minDistance = minDist;
        s.maxDistance = maxDist;
        s.rolloffMode = AudioRolloffMode.Linear;

        s.pitch = set.PickPitch();
        s.volume = Mathf.Clamp01(set.volume * volumeMult);

        s.PlayOneShot(clip);
    }

    public void PlaySfx2D(SoundLibrarySO.ClipSet set, float volumeMult = 1f)
    {
        if (set == null) return;
        var clip = set.PickRandom();
        if (!clip) return;

        var s = NextSfx();
        s.transform.position = Vector3.zero;

        s.spatialBlend = 0f;
        s.pitch = set.PickPitch();
        s.volume = Mathf.Clamp01(set.volume * volumeMult);

        s.PlayOneShot(clip);
    }

    public SoundLibrarySO Library => library;
}