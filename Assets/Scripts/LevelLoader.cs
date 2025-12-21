using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 2f;
    public int sceneToLoad;

    public void ChangeSceneWithFade(int sceneToLoad)
    {
        StartCoroutine(LoadLevel(sceneToLoad));
    }


    IEnumerator LoadLevel(int sceneToLoad)
    {
        //Play animation
        transition.SetTrigger("Start");

        //Wait
        yield return new WaitForSeconds(transitionTime);

        //Load scene
        GameManager.Instance.LoadScene(sceneToLoad);
    }
}
