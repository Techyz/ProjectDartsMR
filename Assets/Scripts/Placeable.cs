using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.WSA.Input;
using UnityEngine;

public class Placeable : MonoBehaviour
{
    // We can decide to place either on horizontal or vertical surfaces
    public enum PlaceableSurfaces
    {
        Horizontal,
        Vertical
    }

    // The base material used to render the bounds asset when placement is allowed
    public Material placeableBoundsMaterial = null;

    // The base material used to render the bounds asset when placement is not allowed
    public Material notPlaceableBoundsMaterial = null;

    // The material used to render the placement shadow when placement it allowed
    public Material placeableShadowMaterial = null;

    // The material used to render the placement shadow when placement it not allowed
    public Material notPlaceableShadowMaterial = null;

    private PlaceableSurfaces placeableSurfaces = PlaceableSurfaces.Vertical;

    // Possible child object(s) to hide during placement
    public List<GameObject> childrenToHide = null;

    // Visible assets meant to display the dimensions of the board and the surface attempting to place the board on
    private GameObject boundsAsset = null;
    private GameObject shadowAsset = null;

    // The box collider used to determine if the object will fit in the desired location
    private BoxCollider boxCollider = null;

    // Gesture Recognizer class that will listen to player's tap gestures to switch between placement mode
    private GestureRecognizer tapGestureRecognizer = null;

    // The position for the board to be placed on
    private Vector3 targetPosition;

    private float lastDistance = 2.0f;
    private float hoverDistance = 0.15f;
    private float maximumPlacementDistance = 5.0f;
    private float distanceFromSurface = 0.01f;
    private float shadowDistanceFromSurface = 0.01f;

    private float upNormalThreshold = 0.9f;
    private float distanceThreshold = 0.02f;

    private float placementVelocity = 0.06f;

    private bool managingBoxCollider = false;

    public bool IsPlacing { get; private set; } = false;

    public bool Placed { get; private set; } = false;

    void Start()
    {
        childrenToHide = new List<GameObject>();
        childrenToHide.Add(GameObject.FindGameObjectWithTag("Cursor"));

        targetPosition = gameObject.transform.position;
        gameObject.transform.LookAt(Camera.main.transform, Vector3.up);

        // Get the board's collider
        boxCollider = gameObject.GetComponentInChildren<BoxCollider>();

        if (boxCollider == null)
        {
            // Add a box collider to the board if one does not exist already
            // We are managing Board's box collider component
            managingBoxCollider = true;
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.enabled = false;
        }

        // Assign a cube primitive as an asset for the board's bounds
        boundsAsset = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundsAsset.transform.parent = gameObject.transform;
        boundsAsset.SetActive(false);

        // A quad representing "shadow" of the board, telling us if the board is in a valid placement position
        shadowAsset = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadowAsset.transform.parent = gameObject.transform;
        shadowAsset.SetActive(false);

        // Register for Tap gestures
        RegisterTapRecognizer();
    }

    void Update()
    {
        // We are placing the board
        if (IsPlacing)
        {
            // Move the board according to the player's gaze
            Move();

            // Display the visual elements of the board depending on its placement position against a valid or invalid surface
            bool canBePlaced = IsValidatedPlacement(out Vector3 targetPosition, out Vector3 surfaceNormal);
            DisplayBounds(canBePlaced);
            DisplayShadow(targetPosition, surfaceNormal, canBePlaced);
        }
        // We finished placing the board, or it has not been started yet
        else
        {
            // Hide the visual elements
            boundsAsset.SetActive(false);
            shadowAsset.SetActive(false);

            // Calculate distance between the board and the gazed surface
            float dist = (gameObject.transform.position - targetPosition).magnitude;
            // If there is a gap between the board and the surface
            if (dist > 0.00f)
            {
                // Smoothly place the board on the gazed surface
                gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, targetPosition, placementVelocity / dist);
                if (dist < 0.05f)
                {
                    //tapGestureRecognizer.StopCapturingGestures();
                    // Unhide the hidden child object(s)
                    for (int i = 0; i < childrenToHide.Count; i++)
                    {
                        childrenToHide[i].SetActive(true);
                    }
                    Placed = true;
                }
            }
            // The board is in its place
            else
            {
                // Unhide the hidden child object(s)
                for (int i = 0; i < childrenToHide.Count; i++)
                {
                    childrenToHide[i].SetActive(true);
                }
            }
        }
    }

    private void RegisterTapRecognizer()
    {
        tapGestureRecognizer = new GestureRecognizer();
        tapGestureRecognizer.SetRecognizableGestures(GestureSettings.Tap);
        tapGestureRecognizer.Tapped += (args) =>
        {
            // Attempt to place the board when tap gesture is detected
            if (!IsPlacing)
            {
                Debug.Log("Placement started!");
                OnPlacementStart();
            }
            else
            {
                Debug.Log("Attempting to place!");
                OnPlacementStop();
            }
        };
        tapGestureRecognizer.StartCapturingGestures();
    }

    bool IsValidatedPlacement(out Vector3 position, out Vector3 normal)
    {
        Vector3 raycastDirection = gameObject.transform.forward;

        if (placeableSurfaces == PlaceableSurfaces.Horizontal)
        {
            // Placing on a horizontal surface
            // Raycast from the bottom face of the collider
            raycastDirection = -(Vector3.up);
        }

        // Initialize out parameters
        position = Vector3.zero;
        normal = Vector3.zero;

        // Get the origin points of the collider
        Vector3[] facePoints = GetColliderBoxFacePoints();

        // Transform origin points of the collider into world space for raycasting purposes
        for (int i = 0; i < facePoints.Length; i++)
        {
            facePoints[i] = gameObject.transform.TransformVector(facePoints[i]) + gameObject.transform.position;
        }

        // Raycast from the center of the box collider to the surface
        if (!Physics.Raycast(facePoints[0], raycastDirection, out RaycastHit centerHit, maximumPlacementDistance, GazeManager.Instance.RaycastLayerMask))
        {
            // We did not hit a surface, return
            return false;
        }
        
        // We hit a surface, set the position and normal
        position = centerHit.point;
        normal = centerHit.normal;
        

        // Cast a ray from all of the corners of the collider box against the surface
        for (int i = 1; i < facePoints.Length; i++)
        {
            if (Physics.Raycast(facePoints[i], raycastDirection, out RaycastHit cornerHit, maximumPlacementDistance, GazeManager.Instance.RaycastLayerMask))
            {
                // For the placement to be valid, all of the corners must have
                // similar enough distance to the surface as the center point
                if (!IsSimilarEnoughDistance(centerHit.distance, cornerHit.distance))
                {
                    return false;
                }
            }
            else
            {
                // Raycast of a corner failed to hit the surface
                return false;
            }
        }
        return true;
    }

    // Points of the box collider used for raycasting to detect if we hit a valid surface
    Vector3[] GetColliderBoxFacePoints()
    {
        // Get the extents of the collider
        // Collider size values are extents values times two
        Vector3 extents = boxCollider.size / 2;

        // Calculate min and max values for each coordinate
        float minX = boxCollider.center.x - extents.x;
        float maxX = boxCollider.center.x + extents.x;
        float minY = boxCollider.center.y - extents.y;
        float maxY = boxCollider.center.y + extents.y;
        float minZ = boxCollider.center.z - extents.z;
        float maxZ = boxCollider.center.z + extents.z;

        Vector3 center;
        Vector3 corner0;
        Vector3 corner1;
        Vector3 corner2;
        Vector3 corner3;

        // We're (mainly) placing on vertical surfaces such as walls
        if (placeableSurfaces == PlaceableSurfaces.Vertical)
        {
            center = new Vector3(boxCollider.center.x, boxCollider.center.y, maxZ); // Center point
            corner0 = new Vector3(minX, minY, maxZ); // bottom left corner
            corner1 = new Vector3(minX, maxY, maxZ); // top left corner
            corner2 = new Vector3(maxX, minY, maxZ); // bottom right corner
            corner3 = new Vector3(maxZ, maxY, maxZ); // top right corner
        }
        else
        {
            // We're placing on a horizontal surface such as a floor or a table
            center = new Vector3(boxCollider.center.x, minY, boxCollider.center.z);
            corner0 = new Vector3(minX, minY, minZ); // back left corner
            corner1 = new Vector3(minX, minY, maxZ); // front left corner
            corner2 = new Vector3(maxX, minY, minZ); // back right corner
            corner3 = new Vector3(maxX, minY, maxZ); // front right corner
        }

        return new Vector3[]
        {
            center,
            corner0,
            corner1,
            corner2,
            corner3
        };
    }

    // Set the board into placement mode
    public void OnPlacementStart()
    {
        // We enable the collider if we want to manage it
        if (managingBoxCollider)
        {
            boxCollider.enabled = true;
        }

        // Hide the child object(s) to make placement easier
        for (int i = 0; i < childrenToHide.Count; i++)
        {
            childrenToHide[i].SetActive(false);
        }

        // Enter placement mode
        IsPlacing = true;
    }

    // Exit from the placement mode
    public void OnPlacementStop()
    {
        // Bail out early if the placement happened on an invalid surface
        if (!IsValidatedPlacement(out Vector3 position, out Vector3 normal))
        {
            Debug.Log("Placement failure: invalid surface!");
            return;
        }

        // We're allowed to place the board
        // Apply a small buffer between the board and the surface
        targetPosition = position + (distanceFromSurface * normal);

        // Orient the board to face opposite of the surface
        OrientBoard(true, normal);

        // If we're managing the collider, disable it
        if (managingBoxCollider)
        {
            boxCollider.enabled = false;
        }

        // Exit placement mode
        IsPlacing = false;
        Debug.Log("Placement success!");
    }

    // Move the board along the mesh surfaces by following player's gaze
    void Move()
    {
        Vector3 moveTo = gameObject.transform.position;
        Vector3 surfaceNormal = Vector3.zero;

        // Get reference to GazeManager's raycasting values
        bool hit = GazeManager.Instance.Hit;

        if (hit)
        {
            float offsetDistance = hoverDistance;

            RaycastHit hitInfo = GazeManager.Instance.HitInfo;

            // Place the board at a small distance from the surface
            // and prevent it from going behind the player
            if (hitInfo.distance <= hoverDistance)
            {
                offsetDistance = 0.0f;
            }

            moveTo = hitInfo.point + (hitInfo.normal * offsetDistance);

            lastDistance = hitInfo.distance;
            surfaceNormal = hitInfo.normal;
        }
        else
        {
            // GazeManager's raycasting failed to hit a surface,
            // so we keep the board at the distance of the last intersected surface
            moveTo = GazeManager.Instance.HeadPosition + (GazeManager.Instance.GazeDirection * lastDistance);
        }

        // Follow the player's gaze
        float distance = Mathf.Abs((gameObject.transform.position - moveTo).magnitude);
        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, moveTo, placementVelocity / distance);

        // Orient the board
        // Use the return value from raycasting to instruct
        // OrientObject function to align to the vertical surface if appropriate
        OrientBoard(hit, surfaceNormal);
    }

    void OrientBoard(bool alignToVerticalSurface, Vector3 surfaceNormal)
    {
        Quaternion rotation = GazeManager.Instance.Rotation;

        // If the player's gaze does not intersect with the Spatial Mapping mesh,
        // orient to board towards the player
        if (alignToVerticalSurface && (placeableSurfaces == PlaceableSurfaces.Vertical))
        {
            // We are placing on a vertical surface
            // If the normal of the Spatial Mapping mesh indicates that the
            // surface is vertical, orient parallel to the surface
            if (Mathf.Abs(surfaceNormal.y) <= (1 - upNormalThreshold))
            {
                rotation = Quaternion.LookRotation(-surfaceNormal, Vector3.up);
            }
        }
        else
        {
            rotation.x = 0.0f;
            rotation.z = 0.0f;
        }

        gameObject.transform.rotation = rotation;
    }

    void DisplayBounds(bool canBePlaced)
    {
        // Verify that the bounds asset is sized and positioned correctly.
        boundsAsset.transform.localPosition = boxCollider.center;
        boundsAsset.transform.localScale = boxCollider.size;
        boundsAsset.transform.rotation = gameObject.transform.rotation;

        // Apply a material to the bounds asset
        if (canBePlaced)
        {
            boundsAsset.GetComponent<Renderer>().sharedMaterial = placeableBoundsMaterial;
        }
        else
        {
            boundsAsset.GetComponent<Renderer>().sharedMaterial = notPlaceableBoundsMaterial;
        }

        // Display the bounds asset
        boundsAsset.SetActive(true);
    }

    void DisplayShadow(Vector3 position, Vector3 surfaceNormal, bool canBePlaced)
    {
        // Rotate and scale the shadow so that it is displayed on the correct surface and matches the board
        float rotationX = 0.0f;

        if (placeableSurfaces == PlaceableSurfaces.Vertical)
        {
            shadowAsset.transform.localScale = boxCollider.size;
        }
        else
        {
            rotationX = 90.0f;
            shadowAsset.transform.localScale = new Vector3(boxCollider.size.x, boxCollider.size.z, 0);
        }

        Quaternion rotation = Quaternion.Euler(rotationX, gameObject.transform.rotation.eulerAngles.y, 0);
        shadowAsset.transform.rotation = rotation;

        // Apply the correct material
        if (canBePlaced)
        {
            shadowAsset.GetComponent<Renderer>().sharedMaterial = placeableShadowMaterial;
        }
        else
        {
            shadowAsset.GetComponent<Renderer>().sharedMaterial = notPlaceableShadowMaterial;
        }

        // Display the shadow asset as appropriate
        if (position != Vector3.zero)
        {
            // Position the shadow a small distance from the target surface, along the normal.
            shadowAsset.transform.position = position + (surfaceNormal * shadowDistanceFromSurface);
            shadowAsset.SetActive(true);
        }
        else
        {
            shadowAsset.SetActive(false);
        }

    }

    bool IsSimilarEnoughDistance(float d1, float d2)
    {
        float distance = Mathf.Abs(d1 - d2);
        return (distance <= distanceThreshold);
    }

    void OnDestroy()
    {
        // Unload objects we have created.
        tapGestureRecognizer.StopCapturingGestures();
        Destroy(boundsAsset);
        boundsAsset = null;
        Destroy(shadowAsset);
        shadowAsset = null;
    }
}