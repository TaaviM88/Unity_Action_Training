using UnityEngine;

public class MusicBootstrap : MonoBehaviour
{
    [SerializeField] private bool playOnStart = true;
    [SerializeField, Range(0f, 1f)] private float volume = 0.75f;

    private void Start()
    {
        if (!playOnStart) return;
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlayRandomMusic(volume);
    }
}