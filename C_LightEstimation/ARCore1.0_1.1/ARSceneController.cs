/*=================================================
==Adapted by  : Ritesh Kanjee(Augmented Startups)==
==Date        : 27 March 2018      				 ==
==Revision    : 1.1 				 		  	 ==	
==Description : Modified HelloAR Controller  	 ==
==			    Cleaned Code - ARCore 1.0 & 1.1  ==
==			   			  	                	 ==	
==================================================*/
namespace GoogleARCore.HelloAR
{
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.Rendering;

#if UNITY_EDITOR
    using Input = InstantPreviewInput;
#endif

    public class  ARSceneController : MonoBehaviour
    {

        public Camera FirstPersonCamera;
        public GameObject TrackedPlanePrefab;
        public GameObject MonsterPrefab;
        public GameObject SearchingForPlaneUI;
        public int MoveCloserSteps = 5;
        private List<TrackedPlane> m_NewPlanes = new List<TrackedPlane>();
        private List<TrackedPlane> m_AllPlanes = new List<TrackedPlane>();

        public float LightThreshold = 0.4f;
        private bool IsDark;
        private bool _isDarkPrevious;
        private bool m_IsQuitting = false;
        public static int CurrentNumberOfMon = 0;
        public GameObject MonsterObject;
        Vector3 Position3;

        void Start()
        {
            MonsterPrefab.SetActive(true);
        }

        public void Update()
        {
            _QuitOnConnectionErrors();
            _MotionTracking();
            _PlaneDetection();
            _InstantiateOnTouch();
            _LightEstimation();



        }
        private void _QuitOnConnectionErrors()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }
            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status.IsError())
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
        }
        private void _MotionTracking()
        {
            // Check that motion tracking is tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                Screen.sleepTimeout = lostTrackingSleepTimeout;
                if (!m_IsQuitting && Session.Status.IsValid())
                {
                    SearchingForPlaneUI.SetActive(true);
                }

                return;
            }
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        private void _PlaneDetection()
        {
            // Plane Detection: Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
            Session.GetTrackables<TrackedPlane>(m_NewPlanes, TrackableQueryFilter.New);
            for (int i = 0; i < m_NewPlanes.Count; i++)
            {
                GameObject planeObject = Instantiate(TrackedPlanePrefab, Vector3.zero, Quaternion.identity,
                    transform);
                planeObject.GetComponent<TrackedPlaneVisualizer>().Initialize(m_NewPlanes[i]);
            }
            // Disable the snackbar UI when no planes are valid.
            Session.GetTrackables<TrackedPlane>(m_AllPlanes);
            bool showSearchingUI = true;
            for (int i = 0; i < m_AllPlanes.Count; i++)
            {
                if (m_AllPlanes[i].TrackingState == TrackingState.Tracking)
                {
                    showSearchingUI = false;
                    break;
                }
            }
            SearchingForPlaneUI.SetActive(showSearchingUI);
        }
        private void _LightEstimation()
        {
            IsDark = Frame.LightEstimate.PixelIntensity < LightThreshold;       //Get the pixel intensity estimate from camera
            if (MonsterObject != null) 
            {   
                if (Frame.LightEstimate.PixelIntensity < LightThreshold)        //If light estimate is below our theshold
                {

                    MonsterObject.SetActive(true);                              //Activate monster at same pose
                    if (IsDark && _isDarkPrevious != IsDark)                    //Dark State Event
                    {
                        var tra = MonsterObject.transform;                 
                        var dif = Vector3.ProjectOnPlane(FirstPersonCamera.transform.position, Vector3.up) - Vector3.ProjectOnPlane(tra.position, Vector3.up);
                        tra.position = tra.position + dif / MoveCloserSteps;    //Make monster move closer to camera when event is detected
                    }
                }
                else
                {   
                    MonsterObject.SetActive(false);                             //else when it is light, hide monster

                }
            }
            _isDarkPrevious = IsDark;
        }
        public void _InstantiateOnTouch()
        {
            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            
            int numberOfMonsAllowed = 1;

            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon | TrackableHitFlags.FeaturePointWithSurfaceNormal;

            if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                Debug.Log("Screen Touched");
                if (CurrentNumberOfMon < numberOfMonsAllowed) {
                    Debug.Log("Current Mons " + CurrentNumberOfMon);
                    MonsterObject = Instantiate(MonsterPrefab, hit.Pose.position, hit.Pose.rotation);
                    CurrentNumberOfMon = CurrentNumberOfMon + 1;// Create an anchor to allow ARCore to track the hitpoint as understanding of the physical world evolves.                                
                    var anchor = hit.Trackable.CreateAnchor(hit.Pose);
                    if ((hit.Flags & TrackableHitFlags.PlaneWithinPolygon) != TrackableHitFlags.None)// Andy should look at the camera but still be flush with the plane.
                    {  
                        Vector3 cameraPositionSameY = FirstPersonCamera.transform.position; // Get the camera position and match the y-component with the hit position.
                        cameraPositionSameY.y = hit.Pose.position.y;
                        MonsterObject.transform.LookAt(cameraPositionSameY, MonsterObject.transform.up);// Have Andy look toward the camera respecting his "up" perspective, which may be from ceiling.
                    }  
                MonsterObject.transform.parent = anchor.transform;// Make Monster model a child of the anchor.
                }
         
            }
 
            return;
        }
        private void _DoQuit()
        {
            Application.Quit();
        }
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }
    }
}