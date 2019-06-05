using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class GestureManager : MonoBehaviour
{
    public class HandState
    {
        public uint id;

        private bool _isPressed = false;
        public bool IsPressed
        {
            get { return _isPressed; }
            set
            {
                _isPressed = value;

                if (_isPressed)
                {
                    PressedTimestamp = Time.time;
                }
            }
        }

        public float PressedTimestamp { get; private set; }

        public Vector3 Position = Vector3.zero;
        public Vector3 AccumulativeVelocity = Vector3.zero;

        public void UpdatePosition(Vector3 position)
        {
            if (IsPressed)
            {
                Vector3 displacement = position - Position;

                AccumulativeVelocity += displacement;

                Position = position;
            }
            else
            {
                Position = position;
                AccumulativeVelocity = Vector3.zero;
            }
        }
    }

    public static GestureManager Instance { get; private set; }

    public Vector3 FingertipsOffset = new Vector3(0, 0.053f, 0.01f);

    private GameObject trackedGameObject;

    private Dictionary<uint, HandState> trackedHands = new Dictionary<uint, HandState>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    public void RegisterInteractionManager()
    {
        InteractionManager.InteractionSourceDetected += (args) =>
        {
            GetHandState(args.state);
        };

        InteractionManager.InteractionSourcePressed += (args) =>
        {
            HandState handState = GetHandState(args.state);
            handState.IsPressed = true;

            if (!args.state.sourcePose.TryGetPosition(out Vector3 handPosition))
            {
                RemoveHandState(args.state);
                return;
            }

            handState.UpdatePosition(handPosition);

            if (trackedGameObject == null)
            {
                //trackedGameObject = Instantiate(GameManager.Instance.dartPrefab);
                trackedGameObject = GameManager.Instance.Dart;
                trackedGameObject.GetComponent<Rigidbody>().useGravity = false;
                trackedGameObject.GetComponentInChildren<Collider>().enabled = false;
                trackedGameObject.transform.position = handState.Position + Camera.main.transform.forward * 0.25f + Camera.main.transform.TransformVector(FingertipsOffset);
                trackedGameObject.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            }
        };

        InteractionManager.InteractionSourceUpdated += (args) =>
        {
            if (!args.state.sourcePose.TryGetPosition(out Vector3 handPosition))
            {
                RemoveHandState(args.state);
                return;
            }

            HandState handState = GetHandState(args.state);
            handState.UpdatePosition(handPosition);

            if (trackedGameObject != null)
            {
                trackedGameObject.transform.position = handState.Position + Camera.main.transform.forward * 0.25f + Camera.main.transform.TransformVector(FingertipsOffset);
                trackedGameObject.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            }
        };

        InteractionManager.InteractionSourceReleased += (args) =>
        {
            HandState handState = GetHandState(args.state);

            if (handState == null || !handState.IsPressed)
            {
                return;
            }

            if (!args.state.sourcePose.TryGetPosition(out Vector3 handPosition))
            {
                RemoveHandState(args.state);
                return;
            }

            handState.UpdatePosition(handPosition);

            handState.IsPressed = false;

            const float minAcceleration = 0.009f;
            const float maxAcceleration = 0.3f;

            Vector3 acceleration = handState.AccumulativeVelocity / (Time.time - handState.PressedTimestamp);

            if (acceleration.magnitude > minAcceleration)
            {
                acceleration = acceleration.normalized * ((acceleration.magnitude / maxAcceleration) * 10.0f);
            }

            if (trackedGameObject != null)
            {
                trackedGameObject.GetComponent<Rigidbody>().useGravity = true;
                trackedGameObject.GetComponentInChildren<Collider>().enabled = true;
                trackedGameObject.GetComponent<Rigidbody>().velocity = acceleration;
                trackedGameObject = null;
            }
        };

        InteractionManager.InteractionSourceLost += (args) =>
        {
            RemoveHandState(args.state);

            if (trackedGameObject != null)
            {
                Destroy(trackedGameObject);
                trackedGameObject = null;
            }
        };
    }

    HandState GetHandState(InteractionSourceState state)
    {
        if (state.source.kind != InteractionSourceKind.Hand)
        {
            return null;
        }

        if (!trackedHands.ContainsKey(state.source.id))
        {
            if (!state.sourcePose.TryGetPosition(out Vector3 handPosition))
            {
                return null;
            }

            trackedHands.Add(state.source.id, new HandState
            {
                id = state.source.id,
                Position = handPosition,
            });
        }

        return trackedHands[state.source.id];
    }

    void RemoveHandState(InteractionSourceState state)
    {
        if (trackedHands.ContainsKey(state.source.id))
        {
            trackedHands.Remove(state.source.id);
        }

        if (trackedGameObject != null)
        {
            Destroy(trackedGameObject);
            trackedGameObject = null;
        }
    }
}

// --------------------- TEST CODE --------------------------

// Possible solution by using GestureManager's Hold events
//recognizer.HoldStarted += (HoldStartedEventArgs) =>
//{
//    // Move the dart along the player hand movement
//    if (FocusedObject != null)
//    {
//        Vector3 handPos;

//        HoldStartedEventArgs.sourcePose.TryGetPosition(out handPos);

//        focusedObjectPreviousPos = FocusedObject.transform.position;
//        Debug.Log("Hold gesture captured. Starting hold!");
//        FocusedObject.transform.position = handPos + Camera.main.transform.forward * 2.0f;
//        //FocusedObject.SendMessage("OnManipulationStart");
//    }
//};

//recognizer.HoldCompleted += (HoldCompletedEventArgs) =>
//{
//    Vector3 vel;

//    if (HoldCompletedEventArgs.sourcePose.TryGetVelocity(out vel))
//    {
//        // Call dart "throw" function and pass the velocity as argument
//    }
//    Debug.Log("Hold completed!");
//};

//recognizer.HoldCanceled += (HoldCanceledEventArgs) =>
//{
//    // Return dart to hand
//    Debug.Log("Hold canceled!");
//};


// Possible solution by using Unity's InteractionManager class
//InteractionManager.InteractionSourceDetected += (args) =>
//{
//    if (args.state.source.kind == InteractionSourceKind.Hand)
//    {
//        Debug.Log("Hand detected!");
//    }
//};

//InteractionManager.InteractionSourceLost += (args) =>
//{
//    if (args.state.source.kind == InteractionSourceKind.Hand)
//    {
//        Debug.Log("Hand lost!");
//    }
//};

//InteractionManager.InteractionSourceUpdated += (args) =>
//{
//    if (args.state.source.kind == InteractionSourceKind.Hand)
//    {
//        if (args.state.anyPressed)
//        {
//            Debug.Log("Tap registered!");
//            Vector3 handPosition;

//            args.state.sourcePose.TryGetPosition(out handPosition);

//            if (FocusedObject != null)
//            {


//                FocusedObject.transform.position = Vector3.Lerp(FocusedObject.transform.position, handPosition + Camera.main.transform.forward * 3.0f, fracJourney * 0.5f);
//            }
//        }
//    }
//};