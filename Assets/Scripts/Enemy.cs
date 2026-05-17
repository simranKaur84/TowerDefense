using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("References")]
    public Transform trans;
    public Transform projectileSeekPoint;

    [Header("Stats")]
    public float maxHealth;
    [HideInInspector] public float health;
    [HideInInspector] public bool alive = true;
    public float healthGainPerLevel;

    [Header("Audio")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public AudioClip leakSound;

    [HideInInspector] public AudioSource audioSource;

    public void TakeDamage(float amount)
    {
        if (amount > 0)
        {
            health = Mathf.Max(health - amount, 0);

            if (health == 0)
                Die();
            else
                PlaySound(hurtSound); // play hurt only if still alive
        }
    }

    public void Die()
    {
        if (alive)
        {
            alive = false;
            PlaySound(deathSound);
            Destroy(gameObject);
        }
    }

    public void Leak()
{
    if (Player.remainingLives > 0)
    {
        Player.remainingLives -= 1;
    }
    Destroy(gameObject);
}

    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    protected virtual void Start()
    {
        maxHealth = maxHealth + (healthGainPerLevel * (Player.level - 1));
        health = maxHealth;
        audioSource = GetComponent<AudioSource>();
    }
}