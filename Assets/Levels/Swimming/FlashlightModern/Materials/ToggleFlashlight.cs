using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleFlashlight : MonoBehaviour
{
    int flashlightState = 1;
    public GameObject lightOnPrefab;
    public GameObject lightOffPrefab;
    public AudioClip switchSound;
    
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.F)) // press F to toggle
        {
            flashlightState = 1-flashlightState; // toggle 1-0=1 , 1-1=0 
            AudioSource audio = GetComponent<AudioSource>(); // get audiosource

            audio.Play(); // play the clip
        }
        if (flashlightState==1) // change depending on state
        {
            lightOnPrefab.SetActive(true);
            lightOffPrefab.SetActive(false);
        }
        if (flashlightState == 0)
        {
            lightOnPrefab.SetActive(false);
            lightOffPrefab.SetActive(true);
        }
	}
}
