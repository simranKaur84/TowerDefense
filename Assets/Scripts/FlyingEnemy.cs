using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemy : Enemy
{
    [Tooltip("Units moved per second.")]
    public float movespeed;

    [Header("Flying Enemy Audio")]
    public AudioClip flySound;

    private Vector3 targetPosition;

    protected override void Start()
    {
        base.Start();

        targetPosition = GroundEnemy.path.corners[GroundEnemy.path.corners.Length - 1];
        targetPosition.y = trans.position.y;

        // Start looping fly sound
        if (flySound != null && audioSource != null)
        {
            audioSource.clip = flySound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        trans.position = Vector3.MoveTowards(
            trans.position,
            targetPosition,
            movespeed * Time.deltaTime
        );

        if (trans.position == targetPosition)
            Leak();
    }
}