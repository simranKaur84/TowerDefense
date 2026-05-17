using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI livesText;
    public GameObject gameOverPanel;

    [Header("Game Settings")]
    public int currentLevel = 1;
    public int lives = 3;

    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        isGameOver = false;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI();
    }

    public void LoseLife()
    {
        if (isGameOver) return;

        lives--;

        if (lives < 0)
            lives = 0;

        UpdateUI();

        if (lives <= 0)
        {
            GameOver();
        }
    }

    private void UpdateUI()
    {
        if (levelText != null)
            levelText.text = "Level: " + currentLevel;

        if (livesText != null)
            livesText.text = "Lives: " + lives;
    }

    private void GameOver()
{
    if (isGameOver) return;

    isGameOver = true;

    Debug.Log("GameOver() called");

    if (AudioManager.Instance != null)
    {
        Debug.Log("AudioManager found, playing sound");
        AudioManager.Instance.PlayGameOverSound();
    }
    else
    {
        Debug.Log("AudioManager is NULL!");
    }

    if (gameOverPanel != null)
        gameOverPanel.SetActive(true);

    StartCoroutine(FreezeGameAfterDelay());
}

    private IEnumerator FreezeGameAfterDelay()
{
    yield return new WaitForSecondsRealtime(1.5f); // give sound time to play
    Time.timeScale = 0f;
}

    public bool IsGameOver()
    {
        return isGameOver;
    }
}