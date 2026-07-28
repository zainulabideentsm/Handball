using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GoalCelebrationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private CanvasGroup goalCanvasGroup;
    [SerializeField] private RectTransform goalUITransform;

    [Header("UI Timing")]
    [SerializeField, Min(0.01f)] private float popDuration = 0.18f;
    [SerializeField, Min(0f)] private float visibleDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.3f;

    [Header("UI Scale")]
    [SerializeField, Range(0.1f, 1f)] private float startingScale = 0.65f;
    [SerializeField, Range(1f, 2f)] private float endingScale = 1.15f;

    private Coroutine uiRoutine;

    private void Awake()
    {
        HideGoalUIImmediately();
    }

    public void PlayGoalCelebration(ParticleSystem confettiParticles)
    {
        PlayConfetti(confettiParticles);

        if (followCamera != null)
        {
            followCamera.PlayGoalShake();
        }

        if (uiRoutine != null)
        {
            StopCoroutine(uiRoutine);
        }

        uiRoutine = StartCoroutine(AnimateGoalUI());
    }

    private void PlayConfetti(ParticleSystem confettiParticles)
    {
        if (confettiParticles == null)
        {
            return;
        }

        confettiParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        confettiParticles.Play(true);
    }

    private IEnumerator AnimateGoalUI()
    {
        if (goalCanvasGroup == null || goalUITransform == null)
        {
            yield break;
        }

        goalCanvasGroup.gameObject.SetActive(true);
        goalCanvasGroup.blocksRaycasts = false;
        goalCanvasGroup.interactable = false;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / popDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            goalCanvasGroup.alpha = easedProgress;
            goalUITransform.localScale = Vector3.one * Mathf.Lerp(startingScale, 1f, easedProgress);

            yield return null;
        }

        goalCanvasGroup.alpha = 1f;
        goalUITransform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(visibleDuration);

        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);

            goalCanvasGroup.alpha = 1f - progress;
            goalUITransform.localScale = Vector3.one * Mathf.Lerp(1f, endingScale, progress);

            yield return null;
        }

        HideGoalUIImmediately();
        uiRoutine = null;
    }

    private void HideGoalUIImmediately()
    {
        if (goalCanvasGroup == null)
        {
            return;
        }

        goalCanvasGroup.alpha = 0f;
        goalCanvasGroup.blocksRaycasts = false;
        goalCanvasGroup.interactable = false;
        goalCanvasGroup.gameObject.SetActive(false);

        if (goalUITransform != null)
        {
            goalUITransform.localScale = Vector3.one * startingScale;
        }
    }

    private void OnDisable()
    {
        if (uiRoutine != null)
        {
            StopCoroutine(uiRoutine);
            uiRoutine = null;
        }

        HideGoalUIImmediately();
    }
}