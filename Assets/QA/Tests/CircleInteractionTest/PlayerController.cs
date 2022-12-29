using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private bool _isTurn;
    private CircleAction controls;

    public GameObject Prefab_PointOrange;
    public GameObject Prefab_PointGreen;
    public GameObject Prefab_PointGrey;

    private GameObject DebugGO;

    private bool isStarted;
    private bool isPerformed;

    void Awake()
    {
        controls = new CircleAction();
        controls.Enable();
    }

    void Start()
    {
        DebugGO = GameObject.Find("DEBUG");
        controls.Player.Turn.started += ctx => OnTurnStarted(ctx);
        controls.Player.Turn.performed += ctx => OnTurnPerformed(ctx);
        controls.Player.Turn.canceled += ctx => OnTurnCanceled(ctx);
    }

    private void OnEnable()
    {
        if (controls != null)
            controls.Enable();
    }

    private void OnDisable()
    {
        if(controls!=null)
            controls.Disable();
    }

    void OnTurnStarted(InputAction.CallbackContext ctx)
    {
        isStarted = true;
        GameObject.Destroy(DebugGO);
        DebugGO = new GameObject();
        DebugGO.name = "DEBUG";
    }

    void OnTurnPerformed(InputAction.CallbackContext ctx)
    {
        isPerformed = true;
    }

    void OnTurnCanceled(InputAction.CallbackContext ctx)
    {
        isStarted = false;
        isPerformed = false;
    }

    void Update()
    {
        if (controls.Player.Turn.triggered)
        {
            gameObject.transform.RotateAround(gameObject.transform.position, Vector3.up, 30);
        }
        
        Vector2 mousePos2D = controls.Player.Turn.ReadValue<Vector2>();
        if(mousePos2D != null && isStarted)
        {
            Vector3 mousePos3D = new Vector3(mousePos2D.x, mousePos2D.y, Camera.main.nearClipPlane + 1);
            Vector3 worldPos3D = Camera.main.ScreenToWorldPoint(mousePos3D);

            GameObject newPoint;
            if (isPerformed)
            {
                newPoint = GameObject.Instantiate(Prefab_PointGreen);
                newPoint.transform.position = worldPos3D;
                newPoint.transform.Translate(0, 0, -0.01f);
            }
            else
            {
                newPoint = GameObject.Instantiate(Prefab_PointOrange);
                newPoint.transform.position = worldPos3D;
            }

            newPoint.transform.SetParent(DebugGO.transform);
        }
       
    }
}
