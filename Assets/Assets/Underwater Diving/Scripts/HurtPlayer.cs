using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HurtPlayer : MonoBehaviour {

	private PlayerController thePlayer;

	// Use this for initialization
	void Start () {
		thePlayer = FindAnyObjectByType<PlayerController> ();	
	}

	void OnTriggerEnter2D(Collider2D other){
		if(other.tag == "Player"){
			// Use PlayerController if it exists (original asset)
			if (thePlayer != null) {
				thePlayer.hurt();
			} else {
				// Fallback to PlayerManager (used in this project)
				var pm = other.GetComponent<PlayerManager>();
				if (pm != null) pm.TakeMeleeDamage(4);
			}
		}

	}
}
