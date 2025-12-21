using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    public void TriggerDialogue()
    {
        // Try T1
        var t1 = FindObjectOfType<DialogueManager_T1>();
        if (t1 != null)
        {
            t1.StartDialogue(dialogue);
            gameObject.SetActive(false);
            return;
        }

        // Try T2
        var t2 = FindObjectOfType<DialogueManager_T2>();
        if (t2 != null)
        {
            t2.StartDialogue(dialogue);
            gameObject.SetActive(false);
            return;
        }

        // Try T3
        var t3 = FindObjectOfType<DialogueManager_T3>();
        if (t3 != null)
        {
            t3.StartDialogue(dialogue);
            gameObject.SetActive(false);
            return;
        }

        Debug.LogError("No DialogueManager found in this scene!");
    }
}
