using System.Collections;
using System;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    /*public TextMeshProUGUI textDisplay;
    [TextArea] public string fullText;

    public float typingSpeed = 0.05f;

    public AudioSource audioSource;
    public AudioClip beepSound;

    public int soundFrequency = 2; // play sound every X letters

    void Start()
    {
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        textDisplay.text = "";

        int counter = 0;

        foreach (char letter in fullText.ToCharArray())
        {
            textDisplay.text += letter;
            counter++;

            // Play sound every few letters (not EVERY letter = less annoying)
            if (counter % soundFrequency == 0 && beepSound != null)
            {
                audioSource.PlayOneShot(beepSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    [Header("Text Settings")]
    public TextMeshProUGUI textDisplay;
    [TextArea(3, 10)] public string[] pages;

    public float typingSpeed = 0.03f;

    [Header("Cursor Settings")]
    public string cursor = "_";
    public float cursorBlinkSpeed = 0.5f;
    private bool showCursor = true;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip beepSound;
    public int soundFrequency = 2;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Page Settings")]
    public float delayBetweenPages = 1.5f;
    public bool requireInputForNextPage = false;
    public KeyCode nextPageKey = KeyCode.Space;

    private int currentPage = 0;

    public float soundInterval = 0.03f; // time between beeps
    private float soundTimer = 0f;

    private bool isFinished = false;

    public System.Action<int> onPageStart;

    void Start()
    {
        StartCoroutine(CursorBlink());
        StartCoroutine(TypeAllPages());
    }

    public void StartTyping()
    {
        StartCoroutine(TypeAllPages());
    }

    IEnumerator CursorBlink()
    {
        while (true)
        {
            showCursor = !showCursor;
            yield return new WaitForSeconds(cursorBlinkSpeed);
        }
    }

    IEnumerator TypeAllPages()
    {
        for (currentPage = 0; currentPage < pages.Length; currentPage++)
        {
            yield return StartCoroutine(TypeText(pages[currentPage]));

            // Wait for input or delay
            if (requireInputForNextPage)
            {
                yield return new WaitUntil(() => Input.GetKeyDown(nextPageKey));
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenPages);
            }

            textDisplay.text = "";
        }

        // Keep cursor at end after final page
        textDisplay.text = textDisplay.text.Replace(cursor, "") + cursor;

        isFinished = true;
    }

    public bool IsFinished()
    {
        return isFinished;
    }

    IEnumerator TypeText(string pageText)
    {
        textDisplay.text = "";
        int counter = 0;

        foreach (char letter in pageText.ToCharArray())
        {
            string currentText = textDisplay.text.Replace(cursor, "");
            currentText += letter;

            textDisplay.text = showCursor ? currentText + cursor : currentText;

            counter++;

            // Play sound (skip spaces)
            /*if (!char.IsWhiteSpace(letter) && counter % soundFrequency == 0 && beepSound != null)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(beepSound);
            }
            soundTimer += typingSpeed;

            if (!char.IsWhiteSpace(letter) && soundTimer >= soundInterval && beepSound != null)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(beepSound);

                soundTimer = 0f;
            }

            yield return new WaitForSeconds(typingSpeed);
        }
        // after loop ends
        audioSource.Stop();
    }*/

    [Header("Text")]
    public TextMeshProUGUI textDisplay;
    [TextArea(3, 10)] public string[] pages;

    public float typingSpeed = 0.03f;

    [Header("Cursor")]
    public string cursor = "_";
    public float cursorBlinkSpeed = 0.5f;
    private bool showCursor = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip beepSound;
    public float soundInterval = 0.03f;
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    private float soundTimer = 0f;

    [Header("Page Settings")]
    public float delayBetweenPages = 1.5f;
    public bool requireInput = false;
    public KeyCode nextKey = KeyCode.Space;

    private bool isFinished = false;

    //Event for page triggers
    public Action<int> onPageStart;

    public void StartTyping()
    {
        isFinished = false;
        StartCoroutine(CursorBlink());
        StartCoroutine(TypeAllPages());
    }

    public bool IsFinished() => isFinished;

    IEnumerator CursorBlink()
    {
        while (true)
        {
            showCursor = !showCursor;
            yield return new WaitForSeconds(cursorBlinkSpeed);
        }
    }

    IEnumerator TypeAllPages()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            onPageStart?.Invoke(i);

            yield return StartCoroutine(TypeText(pages[i]));

            if (requireInput)
                yield return new WaitUntil(() => Input.GetKeyDown(nextKey));
            else
                yield return new WaitForSeconds(delayBetweenPages);

            textDisplay.text = "";
        }

        isFinished = true;
        audioSource.Stop();
    }

    IEnumerator TypeText(string page)
    {
        textDisplay.text = "";
        soundTimer = 0f;

        foreach (char letter in page)
        {
            string current = textDisplay.text.Replace(cursor, "");
            current += letter;

            textDisplay.text = showCursor ? current + cursor : current;

            soundTimer += typingSpeed;

            if (!char.IsWhiteSpace(letter) && soundTimer >= soundInterval)
            {
                audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(beepSound);
                soundTimer = 0f;
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        audioSource.Stop();
    }
}