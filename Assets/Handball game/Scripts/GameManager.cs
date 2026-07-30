using Handball.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Shared Sound Data")]
    [SerializeField] private HandballSoundData soundData;

    [Header("Global Audio Sources")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource uiAudioSource;

    [Header("Feedback")]
    [SerializeField] private GameFeedbackController feedback;

    public HandballSoundData SoundData => soundData;
    public AudioSource MusicAudioSource => musicAudioSource;
    public AudioSource UiAudioSource => uiAudioSource;
    public GameFeedbackController Feedback => feedback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (feedback == null)
        {
            feedback = GetComponent<GameFeedbackController>();
        }
    }

    private void Start()
    {
        StartGameplayMusic();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Restart()
    {
        PlayButtonSound();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartGameplayMusic()
    {
        soundData?.StartGameplayMusic(musicAudioSource);
    }

    public void PlayButtonSound()
    {
        soundData?.PlayButton(uiAudioSource);
    }
}