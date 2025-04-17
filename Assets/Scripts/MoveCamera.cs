using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{

    public Transform cameraPosition;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate() // Changed from Update to LateUpdate
    {
        if (cameraPosition != null)
        {
            transform.position = cameraPosition.position;
            transform.rotation = cameraPosition.rotation;
        }
    }
}
