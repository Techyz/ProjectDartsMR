﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = gameObject.transform.position;
    }

    void Update()
    {
        UpdatePosition();
    }

    void UpdatePosition()
    {
        targetPosition = GazeManager.Instance.HeadPosition + (GazeManager.Instance.GazeDirection * 3.0f);
        //+ (Camera.main.transform.up * 0.2f) + (Camera.main.transform.right * -0.2f)
        float distance = Vector3.Distance(gameObject.transform.position, targetPosition);

        gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, targetPosition, Time.deltaTime * 6.0f);
        gameObject.transform.rotation = Camera.main.transform.rotation;
    }
}