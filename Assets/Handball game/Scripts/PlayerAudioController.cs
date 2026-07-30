using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAudioController : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private PlayerMovementController playerMovement;

    [Tooltip("Assign the AudioSource from the PlayerAudio child.")]
    [SerializeField] private AudioSource playerAudioSource;

    private void Awake()
    {
        if (playerMovement == null)
        {
            Debug.LogError("PlayerAudioController: Player Movement is not assigned.", this);
        }

        if (playerAudioSource == null)
        {
            Debug.LogError("PlayerAudioController: Player Audio Source is not assigned.", this);
        }
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.Jumped += PlayJump;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.Jumped -= PlayJump;
        }
    }

    public void PlayJump()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.SoundData == null || playerAudioSource == null)
        {
            return;
        }

        gameManager.SoundData.PlayJump(playerAudioSource);
    }

    public void PlayPickup()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.SoundData == null || playerAudioSource == null)
        {
            return;
        }

        gameManager.SoundData.PlayPickup(playerAudioSource);
    }

    public void PlayThrow()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || gameManager.SoundData == null || playerAudioSource == null)
        {
            return;
        }

        gameManager.SoundData.PlayThrow(playerAudioSource);
    }
}