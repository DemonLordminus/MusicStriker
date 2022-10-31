using moveController;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugTestDash : MonoBehaviour
{
    public Camera cam;
    public playerMoveController controller;
    private Vector3 startPoint;
    // Start is called before the first frame update
    void Start()
    {
        startPoint=controller.transform.position;
    }
    private Vector3 mousePos;
    // Update is called once per frame
    void Update()
    {
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Debug.DrawLine(transform.position, mousePos);
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 dir = mousePos - transform.position;
            controller.Dash(dir);
        }
        if(Input.GetKeyDown(KeyCode.R))
        {
            controller.transform.position = startPoint;
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine("playTest");
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            StopCoroutine("playTest");
        }    
    }
    public Slider slider;
    public TextMeshProUGUI text;
    public void countChange()
    {
        controller.dashCountMax = (int)slider.value;
        text.text = string.Format("³å´Ì´ÎÊý:{0}", controller.dashCountMax);
    }
    IEnumerator playTest()
    {
        var dir = mousePos - transform.position; ;
        controller.Dash(dir);
        yield return new WaitForSeconds(0.6f);
        yield return StartCoroutine("playTest");
    }
}
