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