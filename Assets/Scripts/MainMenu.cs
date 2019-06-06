using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA.Input;

public class MainMenu : MonoBehaviour
{
    private Vector3 targetPosition;
    private Collider tagAlongCollider;
    private GestureRecognizer tapRecognizer;

    public Button BtnStartNewGame;

    private Plane[] frustumPlanes;
    private const int frustumLeft = 0;
    private const int frustumRight = 1;
    private const int frustumBottom = 2;
    private const int frustumTop = 3;

    private float TagalongDistance = 4.0f;

    Camera mainCamera;

    void Start()
    {
        targetPosition = gameObject.transform.position;
        tagAlongCollider = gameObject.GetComponent<Collider>();

        mainCamera = Camera.main;

        BtnStartNewGame.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }

    void Update()
    {
        // Calculate the frustrum planes from the camera
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        // Move the tagalong object based on whether its BoxCollider is in or out of the camera's view frustrum.
        if (UpdatePosition(transform.position, out targetPosition))
        {
            Move(targetPosition);

            Ray ray = new Ray(GazeManager.Instance.HeadPosition, transform.position - GazeManager.Instance.HeadPosition);
            transform.position = ray.GetPoint(TagalongDistance);
        }
        gameObject.transform.LookAt(2 * transform.position - GazeManager.Instance.HeadPosition);
    }

    bool UpdatePosition(Vector3 fromPosition, out Vector3 toPosition)
    {
        bool needsToMove = !GeometryUtility.TestPlanesAABB(frustumPlanes, tagAlongCollider.bounds);
        
        if (!needsToMove)
        {
            toPosition = fromPosition;
            return false;
        }

        // Calculate a default position where the Tagalong should go. In this
        // case TagalongDistance from the camera along the gaze vector.
        toPosition = GazeManager.Instance.HeadPosition + GazeManager.Instance.GazeDirection * TagalongDistance;

        // Create a Ray and set it's origin to be the default toPosition that
        // was calculated above.
        Ray ray = new Ray(toPosition, Vector3.zero);
        Plane plane = new Plane();
        float distanceOffset = 0.0f;

        // Determine if the Tagalong needs to move to the right or the left
        // to get back inside the camera's view frustum. The normals of the
        // planes that make up the camera's view frustum point inward.
        bool moveRight = frustumPlanes[frustumLeft].GetDistanceToPoint(fromPosition) < 0;
        bool moveLeft = frustumPlanes[frustumRight].GetDistanceToPoint(fromPosition) < 0;
        if (moveRight)
        {
            // If the Tagalong needs to move to the right, that means it is to
            // the left of the left frustum plane. Remember that plane and set
            // our Ray's direction to point towards that plane (remember the
            // Ray's origin is already inside the view frustum.
            plane = frustumPlanes[frustumLeft];
            ray.direction = -mainCamera.transform.right;
        }
        else if (moveLeft)
        {
            // Apply similar logic to above for the case where the Tagalong
            // needs to move to the left.
            plane = frustumPlanes[frustumRight];
            ray.direction = mainCamera.transform.right;
        }
        if (moveRight || moveLeft)
        {
            // If the Tagalong needed to move in the X direction, cast a Ray
            // from the default position to the plane we are working with.
            plane.Raycast(ray, out distanceOffset);

            // Get the point along that ray that is on the plane and update
            // the x component of the Tagalong's desired position.
            toPosition.x = ray.GetPoint(distanceOffset).x;
        }

        // Similar logic follows below for determining if and how the
        // Tagalong would need to move up or down.
        bool moveDown = frustumPlanes[frustumTop].GetDistanceToPoint(fromPosition) < 0;
        bool moveUp = frustumPlanes[frustumBottom].GetDistanceToPoint(fromPosition) < 0;
        if (moveDown)
        {
            plane = frustumPlanes[frustumTop];
            ray.direction = mainCamera.transform.up;
        }
        else if (moveUp)
        {
            plane = frustumPlanes[frustumBottom];
            ray.direction = -mainCamera.transform.up;
        }
        if (moveUp || moveDown)
        {
            plane.Raycast(ray, out distanceOffset);
            toPosition.y = ray.GetPoint(distanceOffset).y;
        }

        // Create a ray that starts at the camera and points in the direction
        // of the calculated toPosition.
        ray = new Ray(mainCamera.transform.position, toPosition - mainCamera.transform.position);

        // Find the point along that ray that is the right distance away and
        // update the calculated toPosition to be that point.
        toPosition = ray.GetPoint(TagalongDistance);

        return needsToMove;
    }

    void Move(Vector3 moveTo)
    {
        transform.position = Vector3.Lerp(transform.position, moveTo, Time.deltaTime);
    }
}
