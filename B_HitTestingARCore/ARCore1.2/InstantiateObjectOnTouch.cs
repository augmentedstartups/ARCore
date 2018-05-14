/*================================================
==Adapted by  : Ritesh Kanjee(Arduino Startups)	==
==Date        : 12 March 2018      				==
==Revision    : 1.2 				 		  	==	
==Description : The gameobject to place when	==
==			    tapping the screen.				==
==			   							  		==	
==================================================*/
using UnityEngine;
using GoogleARCore;
using GoogleARCore.HelloAR;

public class InstantiateObjectOnTouch : MonoBehaviour { 
	public Camera FirstPersonCamera; // The first-person camera being used to render the passthrough camera
    public GameObject PlaceGameObject;
	// Update is called once per frame
	void Update ()
	{
		// Get the touch position from the screen to see if we have at least one touch event currently active
		Touch touch;
		if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
		{
			return;
		}
		// Now that we know that we have an active touch point, do a raycast to see if it hits
		// a plane where we can instantiate the object on.
		TrackableHit hit;
		var raycastFilter = TrackableHitFlags.PlaneWithinBounds | TrackableHitFlags.PlaneWithinPolygon;

		if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit) && PlaceGameObject != null)
		{
			// Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
			// world evolves.
			var anchor = hit.Trackable.CreateAnchor(hit.Pose);
            // Instantiate a game object as a child of the anchor; its transform will now benefit
            // from the anchor's tracking.
            var placedObject = Instantiate(PlaceGameObject, hit.Pose.position, hit.Pose.rotation);
            //Each new cube will get a new color upon instantiation.
			placedObject.GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
			// Make the newly placed object a child of the parent
			placedObject.transform.parent = anchor.transform;
		}
	}
}
