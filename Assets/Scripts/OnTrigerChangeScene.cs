using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnTrigerChangeScene : MonoBehaviour
{
    public int scene;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.LoadScene(scene);
        }
       
    }
}
