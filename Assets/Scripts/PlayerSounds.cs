using Unity.Netcode;
using UnityEngine;

public class PlayerSounds : NetworkBehaviour
{
    [SerializeField] private AudioClip eatSound;
    [SerializeField] private AudioClip slapSound;
    [SerializeField] private AudioClip objectHitSound;
    [SerializeField] private float eatVolume = 1f;
    [SerializeField] private float hitVolume = 1f;
    [SerializeField] private float hitMinDistance = 2f;
    [SerializeField] private float hitMaxDistance = 22f;

    private AudioSource eatSource;

    private void Awake()
    {
        eatSource = gameObject.AddComponent<AudioSource>();
        eatSource.playOnAwake = false;
        eatSource.spatialBlend = 0f;
    }

    public void PlayEatSound()
    {
        PlayEatSoundRpc();
    }

    [Rpc(SendTo.Owner)]
    private void PlayEatSoundRpc()
    {
        eatSource.PlayOneShot(eatSound, eatVolume);
    }

    public void PlayHitSound(Vector3 position, bool isObjectHit)
    {
        AudioClip clip = slapSound;
        if (isObjectHit)
        {
            clip = objectHitSound;
        }

        GameObject soundObject = new GameObject("HitSound");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = hitVolume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = hitMinDistance;
        source.maxDistance = hitMaxDistance;
        source.dopplerLevel = 0f;
        source.Play();

        Destroy(soundObject, clip.length + 0.1f);
    }
}
