using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;

public class Movement : MonoBehaviour
{
    public GameObject camera;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var axis = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick, OVRInput.Controller.LTouch);
        Vector3 targetDirection = axis.x * camera.transform.right + axis.y * camera.transform.forward;
        targetDirection.y = 0;

        transform.position = Vector3.MoveTowards(transform.position, targetDirection + transform.position, Time.deltaTime * speed);
    }
}
