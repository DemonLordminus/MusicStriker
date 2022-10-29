using moveController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTestDash : MonoBehaviour
{
    public Camera cam;
    public playerMoveController controller;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Debug.DrawLine(transform.position, mousePos);

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 dir = mousePos - transform.position;
            controller.Dash(dir);
        }
        
    }
}
