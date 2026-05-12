using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverBestText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void UpdateScore(int score, int best)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
        if (bestScoreText != null) bestScoreText.text = best.ToString("N0");
    }

    public void ShowGameOver(int score, int best)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverScoreText != null) gameOverScoreText.text = $"점수: {score:N0}";
        if (gameOverBestText != null) gameOverBestText.text = $"최고 점수: {best:N0}";
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // Called by the Restart button's OnClick
    public void OnRestartButton()
    {
        GameManager.Instance?.RestartGame();
    }
}
