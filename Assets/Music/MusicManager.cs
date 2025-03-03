using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance; // Singleton for easy access

    public AudioSource audioSource; // Main audio source
    public AudioClip mainMenuMusic;
    public AudioClip bossFightMusic;
    public AudioClip deathSoundEffect;

    private bool isBossMusicPlaying = false;

    void Awake()
    {
        // Ensure only one MusicManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayMainMenuMusic();
    }

    public void PlayMainMenuMusic()
    {
        if (audioSource.clip != mainMenuMusic)
        {
            audioSource.clip = mainMenuMusic;
            audioSource.loop = true;
            audioSource.Play();
            isBossMusicPlaying = false;
        }
    }

    public void PlayBossFightMusic()
    {
        if (!isBossMusicPlaying)
        {
            audioSource.clip = bossFightMusic;
            audioSource.loop = true;
            audioSource.Play();
            isBossMusicPlaying = true;
        }
    }

    public void PlayDeathSound()
    {
        audioSource.PlayOneShot(deathSoundEffect);
    }
}
