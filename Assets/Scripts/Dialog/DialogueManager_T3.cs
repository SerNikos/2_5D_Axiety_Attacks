using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager_T3 : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public Animator dialogBoxAnimator;
    public Animator gnomeTeacherAnimator;

    [Header("Typing Settings")]
    [Tooltip("Time between each typed character.")]
    public float typingSpeed = 0.02f;

    private Queue<string> sentences;
    private int counter = 0;

    void Awake()
    {
        sentences = new Queue<string>();
    }

    // Interface implementation
    public void StartDialogue(Dialogue dialogue)
    {
        dialogBoxAnimator.SetBool("IsOpen", true);

        nameText.text = dialogue.name;
        sentences.Clear();
        counter = 0;

        foreach (string sentence in dialogue.sentences)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();
        StopAllCoroutines();
        StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";

        SetTalking(true);

        foreach (char letter in sentence)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        counter++;
        yield return new WaitForSeconds(0.1f);

        HandleAnimations();
    }

    void HandleAnimations()
    {
        switch (counter)
        {
            case 4:
                SetTalking(false);
                //gnomeTeacherAnimator.SetTrigger("BreathIn");
                break;

            case 6:
                SetTalking(false);
                //gnomeTeacherAnimator.SetTrigger("BreathOut");
                break;

            case 7:
                SetTalking(false);
                break;

            default:
                SetTalking(false);
                break;
        }
    }

    void SetTalking(bool state)
    {
        //gnomeTeacherAnimator.SetBool("IsTalking", state);
    }

    void EndDialogue()
    {
        dialogBoxAnimator.SetBool("IsOpen", false);
        counter = 0;
    }
}