using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            // Calculate direction from canvas to camera
            Vector3 direction = mainCamera.transform.position - transform.position;
            // Project direction to plane if we want to constrain rotation (optional, let's allow free 3D orientation first)
            // Face the camera directly by rotating around Y and X
            transform.LookAt(transform.position - direction);
        }
    }
}