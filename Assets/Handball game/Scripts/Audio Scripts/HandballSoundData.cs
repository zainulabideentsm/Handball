using UnityEngine;

namespace Handball.Audio
{
    [CreateAssetMenu(fileName = "Handball Sound Data", menuName = "Handball/Audio/Sound Data")]
    public sealed class HandballSoundData : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private AudioClip gameplayMusic;

        [Header("UI")]
        [SerializeField] private AudioClip buttonSound;

        [Header("Player")]
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip throwSound;

        [Header("Ball")]
        [SerializeField] private AudioClip ballImpactSound;
        [SerializeField] private AudioClip hoopImpactSound;

        [Header("Goal")]
        [SerializeField] private AudioClip goalSound;

        public AudioClip GameplayMusic => gameplayMusic;

        private static void PlayOneShot(AudioSource source, AudioClip clip, float volume = 1f)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void PlayButton(AudioSource source)
        {
            PlayOneShot(source, buttonSound);
        }

        public void PlayJump(AudioSource source)
        {
            PlayOneShot(source, jumpSound);
        }

        public void PlayPickup(AudioSource source)
        {
            PlayOneShot(source, pickupSound);
        }

        public void PlayThrow(AudioSource source)
        {
            PlayOneShot(source, throwSound);
        }

        public void PlayBallImpact(AudioSource source, float volume = 1f)
        {
            PlayOneShot(source, ballImpactSound, volume);
        }

        public void PlayHoopImpact(AudioSource source, float volume = 1f)
        {
            PlayOneShot(source, hoopImpactSound, volume);
        }

        public void PlayGoal(AudioSource source)
        {
            PlayOneShot(source, goalSound);
        }

        public void StartGameplayMusic(AudioSource source)
        {
            if (source == null || gameplayMusic == null)
            {
                return;
            }

            if (source.isPlaying && source.clip == gameplayMusic)
            {
                return;
            }

            source.clip = gameplayMusic;
            source.loop = true;
            source.Play();
        }
    }
}