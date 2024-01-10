using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sounds : MonoBehaviour
{
    [SerializeField] private AudioSource rightSound;
    [SerializeField] private AudioSource wrongSound;
    [SerializeField] private AudioSource levelWinSound;
    [SerializeField] private AudioSource levelLoseSound;
    [SerializeField] private AudioSource clickSound;

    public void PlayRightSound()
    {
        if(enabled) rightSound.Play();
    }

    public void PlayWrongSound()
    {
        if(enabled) wrongSound.Play();
    }

    public void PlayLvlWin()
    {
        if (enabled) levelWinSound.Play();
    }

    public void PlayLvlLose()
    {
        if (enabled) levelLoseSound.Play();
    }

    public void PlayClickSound()
    {
        if (enabled) clickSound.Play();
    }
}
