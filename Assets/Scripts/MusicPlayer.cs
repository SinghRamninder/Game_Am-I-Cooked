using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip startMusic;
    [SerializeField] private AudioClip loopMusic;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float startDelay = 0.1f;
    [SerializeField] private bool playOnStart = true;

    private AudioSource startSource;
    private AudioSource loopSource;
    private float volumeMultiplier = 1f;
    private float targetMultiplier = 1f;
    private float fadeSpeed = 1f;
    private bool hasStarted = false;

    private void Awake()
    {
        startSource = CreateSource(startMusic, false);
        loopSource = CreateSource(loopMusic, true);
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play(0f);
        }
    }

    public void Play(float skipTime)
    {
        if (hasStarted) return;
        hasStarted = true;

        double startTime = AudioSettings.dspTime + startDelay;

        if (startMusic == null)
        {
            loopSource.PlayScheduled(startTime);
            return;
        }

        double startMusicLength = (double)startMusic.samples / startMusic.frequency;
        skipTime = Mathf.Clamp(skipTime, 0f, (float)startMusicLength - 0.1f);

        if (skipTime > startDelay)
        {
            startSource.Play();
            startSource.time = skipTime;
            loopSource.PlayScheduled(AudioSettings.dspTime + startMusicLength - skipTime);
            return;
        }

        startSource.PlayScheduled(startTime);
        loopSource.PlayScheduled(startTime + startMusicLength);
    }

    public void LowerVolume(float multiplier, float fadeTime)
    {
        targetMultiplier = multiplier;
        fadeSpeed = Mathf.Abs(volumeMultiplier - targetMultiplier) / Mathf.Max(fadeTime, 0.01f);
    }

    private void Update()
    {
        if (volumeMultiplier == targetMultiplier) return;

        volumeMultiplier = Mathf.MoveTowards(volumeMultiplier, targetMultiplier, fadeSpeed * Time.unscaledDeltaTime);
        startSource.volume = volume * volumeMultiplier;
        loopSource.volume = volume * volumeMultiplier;
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
