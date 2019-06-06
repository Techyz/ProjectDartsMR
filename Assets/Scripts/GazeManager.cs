using UnityEngine;

public class GazeManager : MonoBehaviour
{
    public static GazeManager Instance { get; private set; }

    // Maximum gaze distance, in meters, for calculating a hit.
    public float MaxGazeDistance = 4.0f;

    // Last registered hit distance with a hologram.
    private float lastHitDistance = 4.0f;

    // The layers raycast should target.
    public LayerMask RaycastLayerMask = (1 << 31) | (1 << 30) | (1 << 5);

    // Physics.Raycast result is true if it hits a hologram.
    public bool Hit { get; private set; }

    // HitInfo property gives access to RaycastHit public members.
    public RaycastHit HitInfo { get; private set; }

    // Position of the intersection of the user's gaze and the holograms in the scene.
    public Vector3 Position { get; private set; }

    // RaycastHit Normal direction.
    public Vector3 Normal { get; private set; }

    // Transform component of the hologram hit by gaze
    public Transform HitTransform { get; private set; }

    public Vector3 HeadPosition
    {
        get
        {
            return Camera.main.transform.position;
        }
    }
    public Vector3 GazeDirection
    {
        get
        {
            return Camera.main.transform.forward;
        }
    }

    public Quaternion Rotation
    {
        get
        {
            return Camera.main.transform.localRotation;
        }
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRaycast();
    }

    // Calculates the Raycast hit position and normal.
    private void UpdateRaycast()
    {
        // Get the raycast hit information from Unity's physics system.
        Hit = Physics.Raycast(HeadPosition, GazeDirection, out RaycastHit hitInfo, MaxGazeDistance, RaycastLayerMask);

        // Update the HitInfo property so other classes can use this hit information.
        HitInfo = hitInfo;

        if (Hit)
        {
            // If the raycast hits a hologram, set the position and normal to match the intersection point.
            Position = hitInfo.point;
            Normal = hitInfo.normal;
            HitTransform = hitInfo.transform;
            lastHitDistance = hitInfo.distance;
        }
        else
        {
            // If the raycast does not hit a hologram, default the position to last hit distance in front of the user,
            // and the normal to face the user.
            Position = HeadPosition + (GazeDirection * lastHitDistance);
            Normal = GazeDirection;
            HitTransform = null;
        }
    }
}
