using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Score")]
    [SerializeField, Min(0)] private int startingScore;

    public int Score { get; private set; }

    private void Awake()
    {
        Score = startingScore;
        RefreshUI();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Score += amount;
        RefreshUI();
    }

    public void ResetScore()
    {
        Score = startingScore;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.SetText("Score: {0}", Score);
    }
}