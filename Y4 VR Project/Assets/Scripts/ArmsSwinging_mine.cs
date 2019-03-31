
//======= Arms swinging movement ===============
//
// Upon pressing both touchpad faces and swinging your hands you will move 
// in the direction that you are facing
//
//=============================================================================


using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    public class ArmsSwinging_mine : MonoBehaviour
    {
        [SteamVR_DefaultAction("Teleport", "default")]
        public SteamVR_Action_Boolean teleportAction;

        public LayerMask traceLayerMask;
        public Material pointVisibleMaterial;
        public Transform destinationReticleTransform;
        public Transform invalidReticleTransform;
        public GameObject playAreaPreviewCorner;
        public GameObject playAreaPreviewSide;


        [Header("Debug")]
        public Transform offsetReticleTransform;

        private LineRenderer pointerLineRenderer;
        private GameObject teleportPointerObject;
        private Hand pointerHand = null;
        private Player player = null;
        private bool visible = false;

        private TeleportMarkerBase[] teleportMarkers;

        private float pointerHideStartTime = 0.0f;
        private float fullTintAlpha;

        private float invalidReticleMinScale = 0.2f;
        private float invalidReticleMaxScale = 1.0f;

        private Transform playAreaPreviewTransform;
        private Transform[] playAreaPreviewCorners;
        private Transform[] playAreaPreviewSides;

        private bool originalHoverLockState = false;
        private Interactable originalHoveringInteractable = null;
        private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;


        SteamVR_Events.Action chaperoneInfoInitializedAction;

        private bool gotHeadingDirectionMWIL = false;
        private Vector3 headingVecMWIL;

        private int touchpadAmountPressed = 0;
        private bool armSwingingActive = false;
        private bool updateHandInit = true;
        private Vector3 hand1PosInitial;
        private Vector3 hand2PosInitial;

        private Vector3 hand1PosCur;
        private Vector3 hand2PosCur;

        private float walkThreshold = 0.3f;

        private float singleStep = 0.5f;
        private Vector3 destination;
        private bool doLerp = false;
        private bool detectedStep = false;


        /// <summary>
        /// Singleton
        /// </summary>
        private static ArmsSwinging_mine _instance;
        public static ArmsSwinging_mine instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<ArmsSwinging_mine>();
                }

                return _instance;
            }
        }


        /// <summary>
        /// Get chaperone info on initialisation
        /// </summary>
        void Awake()
        {
            _instance = this;

            chaperoneInfoInitializedAction = ChaperoneInfo.InitializedAction(OnChaperoneInfoInitialized);

            pointerLineRenderer = GetComponentInChildren<LineRenderer>();
            teleportPointerObject = pointerLineRenderer.gameObject;

            int tintColorID = Shader.PropertyToID("_TintColor");
            fullTintAlpha = pointVisibleMaterial.GetColor(tintColorID).a;

            playAreaPreviewCorner.SetActive(false);
            playAreaPreviewSide.SetActive(false);

            float invalidReticleStartingScale = invalidReticleTransform.localScale.x;
            invalidReticleMinScale *= invalidReticleStartingScale;
            invalidReticleMaxScale *= invalidReticleStartingScale;
        }


        /// <summary>
        /// Runs after Awake
        /// Hide the tp points and initialise player
        /// </summary>
        void Start()
        {
            teleportMarkers = GameObject.FindObjectsOfType<TeleportMarkerBase>();

            HidePointer();

            player = InteractionSystem.Player.instance;

            if (player == null)
            {
                Debug.LogError("Teleport: No Player instance found in map.");
                Destroy(this.gameObject);
                return;
            }

            destination = player.trackingOriginTransform.position;

        }


        /// <summary>
        /// Runs when GameObject runs an enabled call.
        /// </summary>
        void OnEnable()
        {
            chaperoneInfoInitializedAction.enabled = true;
            OnChaperoneInfoInitialized(); // In case it's already initialized
        }


        /// <summary>
        /// Runs when GameObject runs a disable call.
        /// </summary>
        void OnDisable()
        {
            chaperoneInfoInitializedAction.enabled = false;
            HidePointer();
        }


        /// <summary>
        /// hide the pointers
        /// </summary>
        public void HideTeleportPointer()
        {
            if (pointerHand != null)
            {
                HidePointer();
            }
        }


        /// <summary>
        /// Run update, check hands and update movement accordingly.
        /// </summary>
        void Update()
        {
            if(updateHandInit)
            {
                hand1PosInitial = player.hands[0].transform.position;
                hand2PosInitial = player.hands[1].transform.position;
                updateHandInit = false;
            }
            Hand oldPointerHand = pointerHand;
            Hand newPointerHand = null;

            foreach (Hand hand in player.hands)
            {
                if (visible)
                {
                    if (WasTeleportButtonReleased(hand))
                    {
                        if (pointerHand == hand) //This is the pointer hand
                        {
                            //TryTeleportPlayer();
                            gotHeadingDirectionMWIL = false;
                        }
                    }
                }

                if (WasTeleportButtonPressed(hand)) //increment amount pressed when a touchpad button is pressed
                {
                    newPointerHand = hand;
                    touchpadAmountPressed++;
                }
                if(WasTeleportButtonReleased(hand))//decrement amount pressed when a touchpad button is released
                {
                    touchpadAmountPressed--;
                }

                //when arm swinging mode is off but should be on
                if(touchpadAmountPressed == 2 && !armSwingingActive)
                {
                    armSwingingActive = true;
                    //track initial position of the hands
                    hand1PosInitial = player.hands[0].transform.position;
                    hand2PosInitial = player.hands[1].transform.position;
                }
                if(touchpadAmountPressed < 2)
                {
                    armSwingingActive = false;
                }
            }
            if(armSwingingActive)
            {
                //track current position of the hands
                hand1PosCur = player.hands[0].transform.position;
                hand2PosCur = player.hands[1].transform.position;


                float distBetweenCurrentAndPreviousPosition1 = (hand1PosInitial - hand1PosCur).magnitude;
                float distBetweenCurrentAndPreviousPosition2 = (hand2PosInitial - hand2PosCur).magnitude;


                ///check magnitude of both hands has left their threshold
                if (distBetweenCurrentAndPreviousPosition1 > walkThreshold && distBetweenCurrentAndPreviousPosition2 > walkThreshold && !doLerp)
                {
                    Debug.Log("THRESHOLD PASSED");
                    Vector3 headingVec = player.bodyDirectionGuess.normalized;
                    headingVecMWIL.y = 0;

                    destination = headingVec * singleStep;
                    destination += player.trackingOriginTransform.position;

                    if ((destination.x < 4.5 && destination.x > -4.5) && (destination.z < 4.5 && destination.z > -4.5))
                    {
                        //player.trackingOriginTransform.position = destination;
                        //must allow a single pass so hands are updated
                        //updateHandInit = true;
                        detectedStep = true;
                        doLerp = true;
                    }
                    else
                    {
                        destination -= player.trackingOriginTransform.position;
                        doLerp = false;
                    }


                }
            }
            //if destination point moves at any point, lerp player to the destination
            if ((player.trackingOriginTransform.position.x > destination.x + 0.001f || player.trackingOriginTransform.position.x < destination.x - 0.001f)
                &&
                (player.trackingOriginTransform.position.z > destination.z + 0.001f || player.trackingOriginTransform.position.z < destination.z - 0.001f) && doLerp)
            {
                player.trackingOriginTransform.position = Vector3.Lerp(player.trackingOriginTransform.position, destination, 0.17f);
            }
            else //otherwise destination is the players current position
            {
                //if its the first loop AFTER the step is linearly interpolated, update initial hand position
                if(detectedStep)
                {
                    updateHandInit = true;
                    detectedStep = false;
                }
                destination = player.trackingOriginTransform.position;
                doLerp = false;
            }

        }



        /// <summary>
        /// initialisation of chaperone and the play area preview
        /// </summary>
        private void OnChaperoneInfoInitialized()
        {
            ChaperoneInfo chaperone = ChaperoneInfo.instance;

            if (chaperone.initialized && chaperone.roomscale)
            {
                //Set up the render model for the play area bounds

                if (playAreaPreviewTransform == null)
                {
                    playAreaPreviewTransform = new GameObject("PlayAreaPreviewTransform").transform;
                    playAreaPreviewTransform.parent = transform;
                    Util.ResetTransform(playAreaPreviewTransform);

                    playAreaPreviewCorner.SetActive(true);
                    playAreaPreviewCorners = new Transform[4];
                    playAreaPreviewCorners[0] = playAreaPreviewCorner.transform;
                    playAreaPreviewCorners[1] = Instantiate(playAreaPreviewCorners[0]);
                    playAreaPreviewCorners[2] = Instantiate(playAreaPreviewCorners[0]);
                    playAreaPreviewCorners[3] = Instantiate(playAreaPreviewCorners[0]);

                    playAreaPreviewCorners[0].transform.parent = playAreaPreviewTransform;
                    playAreaPreviewCorners[1].transform.parent = playAreaPreviewTransform;
                    playAreaPreviewCorners[2].transform.parent = playAreaPreviewTransform;
                    playAreaPreviewCorners[3].transform.parent = playAreaPreviewTransform;

                    playAreaPreviewSide.SetActive(true);
                    playAreaPreviewSides = new Transform[4];
                    playAreaPreviewSides[0] = playAreaPreviewSide.transform;
                    playAreaPreviewSides[1] = Instantiate(playAreaPreviewSides[0]);
                    playAreaPreviewSides[2] = Instantiate(playAreaPreviewSides[0]);
                    playAreaPreviewSides[3] = Instantiate(playAreaPreviewSides[0]);

                    playAreaPreviewSides[0].transform.parent = playAreaPreviewTransform;
                    playAreaPreviewSides[1].transform.parent = playAreaPreviewTransform;
                    playAreaPreviewSides[2].transform.parent = playAreaPreviewTransform;
                    playAreaPreviewSides[3].transform.parent = playAreaPreviewTransform;
                }

                float x = chaperone.playAreaSizeX;
                float z = chaperone.playAreaSizeZ;

                playAreaPreviewSides[0].localPosition = new Vector3(0.0f, 0.0f, 0.5f * z - 0.25f);
                playAreaPreviewSides[1].localPosition = new Vector3(0.0f, 0.0f, -0.5f * z + 0.25f);
                playAreaPreviewSides[2].localPosition = new Vector3(0.5f * x - 0.25f, 0.0f, 0.0f);
                playAreaPreviewSides[3].localPosition = new Vector3(-0.5f * x + 0.25f, 0.0f, 0.0f);

                playAreaPreviewSides[0].localScale = new Vector3(x - 0.5f, 1.0f, 1.0f);
                playAreaPreviewSides[1].localScale = new Vector3(x - 0.5f, 1.0f, 1.0f);
                playAreaPreviewSides[2].localScale = new Vector3(z - 0.5f, 1.0f, 1.0f);
                playAreaPreviewSides[3].localScale = new Vector3(z - 0.5f, 1.0f, 1.0f);

                playAreaPreviewSides[0].localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                playAreaPreviewSides[1].localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                playAreaPreviewSides[2].localRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                playAreaPreviewSides[3].localRotation = Quaternion.Euler(0.0f, 270.0f, 0.0f);

                playAreaPreviewCorners[0].localPosition = new Vector3(0.5f * x - 0.25f, 0.0f, 0.5f * z - 0.25f);
                playAreaPreviewCorners[1].localPosition = new Vector3(0.5f * x - 0.25f, 0.0f, -0.5f * z + 0.25f);
                playAreaPreviewCorners[2].localPosition = new Vector3(-0.5f * x + 0.25f, 0.0f, -0.5f * z + 0.25f);
                playAreaPreviewCorners[3].localPosition = new Vector3(-0.5f * x + 0.25f, 0.0f, 0.5f * z - 0.25f);

                playAreaPreviewCorners[0].localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                playAreaPreviewCorners[1].localRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                playAreaPreviewCorners[2].localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                playAreaPreviewCorners[3].localRotation = Quaternion.Euler(0.0f, 270.0f, 0.0f);

                playAreaPreviewTransform.gameObject.SetActive(false);
            }
        }


        /// <summary>
        /// Hide the pointer
        /// </summary>
        private void HidePointer()
        {
            if (visible)
            {
                pointerHideStartTime = Time.time;
            }

            visible = false;
            if (pointerHand)
            {
                if (ShouldOverrideHoverLock())
                {
                    //Restore the original hovering interactable on the hand
                    if (originalHoverLockState == true)
                    {
                        pointerHand.HoverLock(originalHoveringInteractable);
                    }
                    else
                    {
                        pointerHand.HoverUnlock(null);
                    }
                }

            }
            teleportPointerObject.SetActive(false);

            foreach (TeleportMarkerBase teleportMarker in teleportMarkers)
            {
                if (teleportMarker != null && teleportMarker.markerActive && teleportMarker.gameObject != null)
                {
                    teleportMarker.gameObject.SetActive(false);
                }
            }

            destinationReticleTransform.gameObject.SetActive(false);
            invalidReticleTransform.gameObject.SetActive(false);
            offsetReticleTransform.gameObject.SetActive(false);

            if (playAreaPreviewTransform != null)
            {
                playAreaPreviewTransform.gameObject.SetActive(false);
            }


            pointerHand = null;
        }

        /// <summary>
        /// Check if should override when something held
        /// used when we dont want to allow player to teleport with the hand they are holding something in
        /// </summary>
        /// <returns></returns>
        private bool ShouldOverrideHoverLock()
        {
            if (!allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Check if teleport button was released.
        /// 
        /// 
        /// </summary>
        /// <param name="hand">hand object of the player</param>
        /// <returns></returns>
        private bool WasTeleportButtonReleased(Hand hand)
        {
            if (hand.noSteamVRFallbackCamera != null)
            {
                return Input.GetKeyUp(KeyCode.T);
            }
            else
            {
                return teleportAction.GetStateUp(hand.handType);

                //return hand.controller.GetPressUp( SteamVR_Controller.ButtonMask.Touchpad );
            }
        }

        /// <summary>
        /// Check if teleport button is currently being held down.
        /// 
        /// 
        /// </summary>
        /// <param name="hand">hand object of the player</param>
        /// <returns></returns>
        private bool IsTeleportButtonDown(Hand hand)
        {
            if (hand.noSteamVRFallbackCamera != null)
            {
                return Input.GetKey(KeyCode.T);
            }
            else
            {
                return teleportAction.GetState(hand.handType);
            }
        }

        /// <summary>
        /// Check if teleport button was pressed.
        /// 
        /// 
        /// </summary>
        /// <param name="hand">hand object of the player</param>
        /// <returns></returns>
        private bool WasTeleportButtonPressed(Hand hand)
        {
            if (hand.noSteamVRFallbackCamera != null)
            {
                return Input.GetKeyDown(KeyCode.T);
            }
            else
            {
                return teleportAction.GetStateDown(hand.handType);
            }
        }
    }
}

