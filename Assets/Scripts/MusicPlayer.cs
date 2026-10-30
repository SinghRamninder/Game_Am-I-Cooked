using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip startMusic;
    [SerializeField] private AudioClip loopMusic;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float startDelay = 0.1f;

    private AudioSource startSource;
    private AudioSource loopSource;

    private void Start()
    {
        startSource = CreateSource(startMusic, false);
        loopSource = CreateSource(loopMusic, true);

        double startTime = AudioSettings.dspTime + startDelay;

        if (startMusic == null)
        {
            loopSource.PlayScheduled(startTime);
            return;
        }

        double startMusicLength = (double)startMusic.samples / startMusic.frequency;
        startSource.PlayScheduled(startTime);
        loopSource.PlayScheduled(startTime + startMusicLength);
    }

    private AudioSource CreateSource(AudioClip clip, bool isLooping)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = isLooping;
        source.volume = volume;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.priority = 0;

        return source;
    }
}
