using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class Throwable : MonoBehaviour
{
    private float throwForceMultiplier = 7.0f;
    private float manipulationStartTimeStamp = 0.0f;

    private Rigidbody rb = null;
    private Collider dartCollider = null;

    private GestureRecognizer manipulationRecognizer = null;

    private Vector3 manipulatedObjectOriginalPos = Vector3.zero;
    private Vector3 accumulativeVelocity = Vector3.zero;

    private bool isFlying = false;

    public Vector3 FingertipsOffset = new Vector3(0, 0.053f, 0.01f);

    public bool HasLanded { get; private set; } = false;

    void Start()
    {
        // Get rigidbody of the Dart
        rb = gameObject.GetComponent<Rigidbody>();

        // Get collider of the Dart
        dartCollider = GetComponentInChildren<Collider>();
        dartCollider.isTrigger = true;

        // Register for manipulation gestures
        RegisterManipulationRecognizer();
    }
    
    void FixedUpdate()
    {
        // Orient dart to face its velocity
        if (isFlying)
            rb.rotation = Quaternion.LookRotation(rb.velocity);
            //transform.forward = Vector3.Slerp(transform.forward, rb.velocity.normalized, Time.deltaTime);
    }

    private void RegisterManipulationRecognizer()
    {
        manipulationRecognizer = new GestureRecognizer();
        manipulationRecognizer.SetRecognizableGestures(GestureSettings.ManipulationTranslate);

        manipulationRecognizer.ManipulationStarted += (ManipulationStartedEventArgs) =>
        {
            if (!ManipulationStartedEventArgs.sourcePose.TryGetPosition(out Vector3 handPosition))
                return;

            rb.useGravity = false;
            gameObject.transform.position = handPosition + Camera.main.transform.forward * 0.25f + Camera.main.transform.TransformVector(FingertipsOffset);
            gameObject.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            manipulatedObjectOriginalPos = gameObject.transform.position;
            manipulationStartTimeStamp = Time.time;

            Debug.Log("Dart picked!");
        };

        manipulationRecognizer.ManipulationUpdated += (ManipulationUpdatedEventArgs) =>
        {
            if (!ManipulationUpdatedEventArgs.sourcePose.TryGetPosition(out Vector3 handPosition))
                return;

            accumulativeVelocity = ManipulationUpdatedEventArgs.cumulativeDelta;
            gameObject.transform.position = handPosition + accumulativeVelocity + Camera.main.transform.forward * 0.25f + Camera.main.transform.TransformVector(FingertipsOffset);
            gameObject.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
        };

        manipulationRecognizer.ManipulationCompleted += (ManipulationCompletedEventArgs) =>
        {
            if (!ManipulationCompletedEventArgs.sourcePose.TryGetPosition(out Vector3 handPosition))
                return;

            const float minAcceleration = 0.0009f;
            const float maxAcceleration = 0.3f;

            Vector3 acceleration = accumulativeVelocity / (Time.time - manipulationStartTimeStamp);

            if (acceleration.magnitude > minAcceleration)
                acceleration = acceleration.normalized * ((acceleration.magnitude / maxAcceleration) * throwForceMultiplier);

            rb.useGravity = true;
            rb.velocity = acceleration;
            isFlying = true;

            if (manipulationRecognizer != null)
                manipulationRecognizer.StopCapturingGestures();

            StartCoroutine(DelayedDestroyIfMissed());
            Debug.Log("Dart thrown!");
        };

        manipulationRecognizer.ManipulationCanceled += (ManipulationCanceledEventArgs) =>
        {
            gameObject.transform.position = manipulatedObjectOriginalPos;
            Debug.Log("Oops!");
        };

        manipulationRecognizer.StartCapturingGestures();
    }

    void OnTriggerEnter(Collider other)
    {
        // Placeholder player stat system
        if (other.tag == "Board")
        {
            isFlying = false;
            rb.useGravity = false;
            rb.isKinematic = true;
            HasLanded = true;
            GameManager.Instance.PlayerScore += 10;
            GameManager.Instance.PlayerDarts -= 1;
        }
    }

    IEnumerator DelayedDestroyIfMissed()
    {
        // Destroy darts that miss the board after 5 seconds
        yield return new WaitForSeconds(5);
        if (!HasLanded)
        {
            GameManager.Instance.PlayerDarts -= 1;
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (manipulationRecognizer != null && manipulationRecognizer.IsCapturingGestures())
            manipulationRecognizer.StopCapturingGestures();    
    }
}