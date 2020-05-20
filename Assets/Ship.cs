using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    public Singleton singleton;
    AudioSource audioSource;
    void Start()
    {
        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.Play();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
        else if (Input.GetKeyDown(KeyCode.M) && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (singleton.gameState == Singleton.GameState.InGame)
        {
            if (col.gameObject.CompareTag("MazeWall"))
            {
                singleton.gameState = Singleton.GameState.GameOver;
            }
        }
    }
}
