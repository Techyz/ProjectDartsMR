using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCursor : MonoBehaviour
{
    private float distanceFromCollision = 0.01f;

    private bool _hidden;
    public bool IsHidden
    {
        get { return _hidden; }

        set
        {
            _hidden = value;
            gameObject.SetActive(!_hidden);
        }
    }

    private Quaternion cursorDefaultRotation;
    private MeshRenderer meshRenderer;

    private LayerMask interactiveLayers = (1 << 30) | (1 << 5);
    private Color interactiveColor;
    private Color defaultColor;
    private Color cursorTargetColor;

    void Start()
    {
        if (GazeManager.Instance == null)
        {
            Debug.Log("Must have a GazeManager somewhere in the scene.");
            return;
        }

        if (GazeManager.Instance.RaycastLayerMask == (GazeManager.Instance.RaycastLayerMask | (1 << gameObject.layer)))
        {
            Debug.LogError("The cursor has a layer that is checked in the GazeManager's Raycast Layer Mask.  Change the cursor layer (e.g.: to Ignore Raycast) or uncheck the layer in GazeManager: " +
                LayerMask.LayerToName(gameObject.layer));
        }

        meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();

        if (meshRenderer == null)
        {
            Debug.Log("This script requires that your cursor asset has a MeshRenderer component on it.");
            return;
        }
        
        // Cache the cursor default rotation so the cursor can be rotated with respect to the original orientation.
        cursorDefaultRotation = gameObject.transform.rotation;
        // Assign color values for cursor states
        interactiveColor = new Color(0.67f, 1.0f, 0.47f);
        defaultColor = new Color(1, 1, 1);
    }

    void LateUpdate()
    {
        // Place the cursor at the calculated position.
        gameObject.transform.position = GazeManager.Instance.Position + GazeManager.Instance.Normal * distanceFromCollision;

        // Reorient the cursor to match the hit object normal.
        gameObject.transform.forward = GazeManager.Instance.Normal;
        gameObject.transform.rotation *= cursorDefaultRotation;

        cursorTargetColor = defaultColor;

        if (GazeManager.Instance.HitTransform != null && (interactiveLayers.value & (1 << GazeManager.Instance.HitTransform.gameObject.layer)) > 0)
        {
            cursorTargetColor = interactiveColor;
        }

        meshRenderer.material.color = Color.Lerp(meshRenderer.material.color, cursorTargetColor, 2.0f * Time.deltaTime);
    }
}