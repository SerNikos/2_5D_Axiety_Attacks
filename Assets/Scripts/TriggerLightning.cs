using UnityEngine;
using UnityEngine.VFX; // Required for Visual Effect Graph

public class TriggerLightning : MonoBehaviour
{
    [Header("Visual Effect")]
    public VisualEffect lightningEffect;

    [Header("Audio")]
    public AudioSource audioSource; // Reference to the Audio Source
    public AudioClip lightningSound;  // The thunder/lightning sound clip

    void Update()
    {
        // Check if the 'L' key is pressed down
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Trigger Visual Effect
            if (lightningEffect != null)
            {
                lightningEffect.SendEvent("OnPlay");
            }
            else
            {
                Debug.LogWarning("Lightning Visual Effect reference is missing!");
            }

            // Play Sound Effect
            if (audioSource != null && lightningSound != null)
            {
                audioSource.PlayOneShot(lightningSound);
            }
            else if (audioSource == null)
            {
                Debug.LogWarning("Audio Source reference is missing!");
            }
        }
    }
}