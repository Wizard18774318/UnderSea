using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

	private PlayerController thePlayer;
	public GameObject death;

	public float speed = 0.3f;

	private float turnTimer;
	public float timeTrigger;

	private Rigidbody2D myRigidbody;



 

	// Use this for initialization
	void Start () {
		thePlayer = FindAnyObjectByType<PlayerController> ();	
		myRigidbody = GetComponent<Rigidbody2D> ();

		turnTimer = 0;
		timeTrigger = 3f;
		 
	}

	// Update is called once per frame
	void Update (){
		if (myRigidbody != null) {
			// Use Mathf.Sign so scale magnitude doesn't affect speed
			myRigidbody.linearVelocity = new Vector3 (Mathf.Sign(myRigidbody.transform.localScale.x) * speed, myRigidbody.linearVelocity.y, 0f);
		}

		turnTimer += Time.deltaTime;
		if(turnTimer >= timeTrigger){
			turnAround ();
			turnTimer = 0;
		}



	}


	void OnTriggerEnter2D(Collider2D other){

		if(other.tag == "Player"){
			// PlayerController might not exist in this project
			if (thePlayer != null && thePlayer.rushing){
				Instantiate (death, gameObject.transform.position, gameObject.transform.rotation);
				Destroy (gameObject);
			}
			// Otherwise just do nothing (player not rushing or PlayerController doesn't exist)
		}

	}

	void turnAround(){
		// Preserve the current scale magnitude, only flip X for direction
		Vector3 s = transform.localScale;
		transform.localScale = new Vector3(-s.x, s.y, s.z);
	}
}
