using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;

public class Cube : MonoBehaviour
{
    private OVRCameraRig camera;
    private GameObject cube;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cube.transform.position = camera.centerEyeAnchor.transform.position;
    }
}
