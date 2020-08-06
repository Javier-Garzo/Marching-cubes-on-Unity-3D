using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFlyMovement : MonoBehaviour
{
    public float speed = 1.0f;
    public float scrollSpeed = 10.0f;
    private Transform myCamera;
    // Start is called before the first frame update
    void Start()
    {
        myCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = new Vector3(-Input.GetAxis("Vertical"),0, Input.GetAxis("Horizontal")).normalized ;
        transform.position += direction * speed;
        myCamera.position += Vector3.up * -Input.mouseScrollDelta.y * scrollSpeed;
        myCamera.position = new Vector3(myCamera.position.x, Mathf.Clamp(myCamera.position.y,50,300), myCamera.position.z);
    }
}
