namespace GoogleARCore.Examples.HelloAR
{
    using System.Collections.Generic;
    using GoogleARCore;
    using GoogleARCore.Examples.Common;
    using UnityEngine;
    using UnityEngine.EventSystems;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    using Input = InstantPreviewInput;
#endif

    /// <summary>
    /// Controls the HelloAR example.
    /// </summary>
    public class HelloARController : MonoBehaviour
    {
        public Camera FirstPersonCamera;

        public GameObject DRBackground;

        private string test01 = "chair";
        private string test02 = "laptop";
        public GameObject ChairModel;
        public GameObject LaptopModel;

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            // Enable ARCore to target 60fps camera capture frame rate on supported devices.
            // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
            Application.targetFrameRate = 60;
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                //タップしていなければ何もしない
                return;
            }

            
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;
            if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
            {
                //タップしたら、アンカーを配置
                if ((hit.Trackable is DetectedPlane))
                {
                    if (DRBackground.GetComponent<DRController02>().label.Equals(test01))
                    {
                        var gameObject = Instantiate(ChairModel, hit.Pose.position, hit.Pose.rotation);
                        var anchor = hit.Trackable.CreateAnchor(hit.Pose);
                        gameObject.transform.parent = anchor.transform;
                    }
                    else if (DRBackground.GetComponent<DRController02>().label.Equals(test02))
                    {
                        var gameObject = Instantiate(LaptopModel, hit.Pose.position, hit.Pose.rotation);
                        gameObject.transform.Rotate(0, 180.0f, 0, Space.Self);
                        var anchor = hit.Trackable.CreateAnchor(hit.Pose);
                        gameObject.transform.parent = anchor.transform;
                    }

                }
            }


        }

        
    }
}
