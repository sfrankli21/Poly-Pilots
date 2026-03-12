using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CycleThroughCameras : MonoBehaviour
{
    public List<GameObject> Cameras = new List<GameObject>();

    int selectedCameraIndex;

    void Start()
    {
        selectedCameraIndex = 0;
        UpdateCameraStates();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            CycleCamera();
        }
    }

    public void CycleCamera()
    {
        if (Cameras == null || Cameras.Count == 0)
        {
            return;
        }

        if (selectedCameraIndex >= 0 && selectedCameraIndex < Cameras.Count && Cameras[selectedCameraIndex] != null)
        {
            Cameras[selectedCameraIndex].SetActive(false);
        }

        selectedCameraIndex++;

        if (selectedCameraIndex >= Cameras.Count)
        {
            selectedCameraIndex = 0;
        }

        UpdateCameraStates();
    }

    void UpdateCameraStates()
    {
        if (Cameras == null || Cameras.Count == 0)
        {
            return;
        }

        for (int i = 0; i < Cameras.Count; i++)
        {
            if (Cameras[i] != null)
            {
                Cameras[i].SetActive(i == selectedCameraIndex);
            }
        }
    }
}