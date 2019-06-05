using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;

public class SpatialMappingManager : MonoBehaviour
{
    // Low-level API to map and visualize player environment
    // by using Unity's SurfaceObserver class

    public static SpatialMappingManager Instance { get; private set; }

    private SurfaceObserver surfaceObserver;

    public Material surfaceMaterial;

    public float removalDelay = 10.0f;
    public float timeBetweenUpdates = 2.5f;

    private Dictionary<int, GameObject> cachedSurfaces = new Dictionary<int, GameObject>();
    private Dictionary<int, float> surfacesToBeRemoved = new Dictionary<int, float>();

    private bool _observing = false;

    public bool IsObserving
    {
        get { return _observing; }
        set
        {
            _observing = value;
            StopAllCoroutines();
            if (_observing)
            {
                StartCoroutine(Observe());
            }
        }
    }

    private bool _surfacesVisible;

    public bool SurfacesVisible
    {
        get { return _surfacesVisible; }
        set
        {
            _surfacesVisible = value;

            foreach (KeyValuePair<int, GameObject> entry in cachedSurfaces)
            {
                MeshRenderer renderer = entry.Value.GetComponent<MeshRenderer>();
                renderer.enabled = _surfacesVisible;
            }
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        surfaceObserver = new SurfaceObserver();
        surfaceObserver.SetVolumeAsAxisAlignedBox(Vector3.zero, new Vector3(4.0f, 4.0f, 4.0f));
        //surfaceObserver.SetVolumeAsSphere(Vector3.zero, 2.0f);
    }

    void Update()
    {
        var surfaceIds = surfacesToBeRemoved.Keys;
        foreach (int surfaceId in surfaceIds)
        {
            if (surfacesToBeRemoved[surfaceId] >= Time.time)
            {
                surfacesToBeRemoved.Remove(surfaceId);

                if (cachedSurfaces.TryGetValue(surfaceId, out GameObject surface))
                {
                    cachedSurfaces.Remove(surfaceId);
                    Destroy(surface);
                }
            }
        }
    }

    IEnumerator Observe()
    {
        // While we are observing, update spatial mapping meshes every 2.5 seconds.
        var wait = new WaitForSeconds(timeBetweenUpdates);
        while (IsObserving)
        {
            surfaceObserver.Update(OnSurfaceChanged);
            yield return wait;
        }
    }

    void OnSurfaceChanged(SurfaceId surfaceId, SurfaceChange changeType, Bounds bounds, DateTime updateTime)
    {
        switch (changeType)
        {
            case SurfaceChange.Added:
            case SurfaceChange.Updated:
                {
                    if (surfacesToBeRemoved.ContainsKey(surfaceId.handle))
                    {
                        surfacesToBeRemoved.Remove(surfaceId.handle);
                    }

                    if (!cachedSurfaces.TryGetValue(surfaceId.handle, out GameObject surface))
                    {
                        surface = new GameObject();
                        surface.name = string.Format("surface_{0}", surfaceId.handle);
                        surface.layer = LayerMask.NameToLayer("SpatialSurface");
                        surface.transform.parent = transform;
                        surface.AddComponent<MeshRenderer>();
                        surface.AddComponent<MeshFilter>();
                        surface.AddComponent<WorldAnchor>();
                        surface.AddComponent<MeshCollider>();
                        cachedSurfaces.Add(surfaceId.handle, surface);
                    }

                    SurfaceData surfaceData;
                    surfaceData.id.handle = surfaceId.handle;
                    surfaceData.outputMesh = surface.GetComponent<MeshFilter>() ?? surface.AddComponent<MeshFilter>();
                    surfaceData.outputAnchor = surface.GetComponent<WorldAnchor>() ?? surface.AddComponent<WorldAnchor>();
                    surfaceData.outputCollider = surface.GetComponent<MeshCollider>() ?? surface.AddComponent<MeshCollider>();
                    surfaceData.trianglesPerCubicMeter = 1000;
                    surfaceData.bakeCollider = true;

                    if (!surfaceObserver.RequestMeshAsync(surfaceData, OnDataReady))
                    {
                        Debug.LogWarningFormat("Is {0} not a valid surface", surfaceData.id);
                    }
                    break;
                }
            case SurfaceChange.Removed:
                {
                    if (cachedSurfaces.TryGetValue(surfaceId.handle, out GameObject surface))
                    {
                        // Instead of removing surfaces on instant, pass the surfaces marked for removal into a queue
                        surfacesToBeRemoved.Add(surfaceId.handle, Time.time + removalDelay);
                    }
                    break;
                }
        }
    }

    void OnDataReady(SurfaceData bakedData, bool outputWritten, float elapsedBakeTimeSecond)
    {
        if (!outputWritten)
            return;

        if (cachedSurfaces.TryGetValue(bakedData.id.handle, out GameObject surface))
        {
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();

            if (surfaceMaterial != null)
            {
                renderer.sharedMaterial = surfaceMaterial;
            }
            renderer.enabled = SurfacesVisible;
        }
    }
}