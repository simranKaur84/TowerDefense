using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Sounds")]
    public AudioClip gameOverSound;

    private void Awake()
    {
        // Singleton - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayGameOverSound()
{
    Debug.Log("PlayGameOverSound called, clip: " + gameOverSound + ", source: " + audioSource);
    if (gameOverSound != null && audioSource != null)
        audioSource.PlayOneShot(gameOverSound);
}
}