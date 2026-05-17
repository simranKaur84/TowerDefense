using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GroundEnemy : Enemy
{
    public static NavMeshPath path;

    public float movespeed = 22;

    [Header("Ground Enemy Audio")]
    public AudioClip walkSound;

    private int currentCornerIndex = 0;
    private Vector3 currentCorner;

    private bool CurrentCornerIsFinal
    {
        get { return currentCornerIndex == (path.corners.Length - 1); }
    }

    protected override void Start()
    {
        base.Start();
        currentCorner = path.corners[0];

        // Start looping walk sound
        if (walkSound != null && audioSource != null)
        {
            audioSource.clip = walkSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void GetNextCorner()
    {
        currentCornerIndex += 1;
        currentCorner = path.corners[currentCornerIndex];
    }

    void Update()
    {
        if (currentCornerIndex != 0)
            trans.forward = (currentCorner - trans.position).normalized;

        trans.position = Vector3.MoveTowards(
            trans.position,
            currentCorner,
            movespeed * Time.deltaTime
        );

        if (trans.position == currentCorner)
        {
            if (CurrentCornerIsFinal)
                Leak();
            else
                GetNextCorner();
        }
    }
}