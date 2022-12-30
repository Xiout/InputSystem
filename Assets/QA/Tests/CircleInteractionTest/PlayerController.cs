using Assets;
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
    private List<Vector2> GesturePoints;

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
        GesturePoints = new List<Vector2>();
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
            if (!GesturePoints.Contains(mousePos2D))
            {
                GesturePoints.Add(mousePos2D);
            }

            float z = Camera.main.nearClipPlane + 1;
            Vector3 mousePos3D = new Vector3(mousePos2D.x, mousePos2D.y, z);
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

            if (controls.Player.Turn.WasPerformedThisFrame())
            {

                var circle = GeometryHelp.GetCircleFurthestPoints(GesturePoints);

                Vector3 centerMouse3D = new Vector3(circle.Center.x, circle.Center.y, z);
                Vector3 centerWorldPos3D = Camera.main.ScreenToWorldPoint(centerMouse3D);
                GameObject circleCenterPoint = GameObject.Instantiate(Prefab_PointGrey);
                circleCenterPoint.transform.position = centerWorldPos3D;
                circleCenterPoint.transform.SetParent(DebugGO.transform);

                for (float theta = -Mathf.PI; theta < Mathf.PI; theta += 0.1f)
                {
                    Vector3 circlePointMouse3D = new Vector3(circle.Radius * Mathf.Sin(theta) + circle.Center.x, circle.Radius * Mathf.Cos(theta) + circle.Center.y, z);
                    Vector3 circlePointWorldPos3D = Camera.main.ScreenToWorldPoint(circlePointMouse3D);
                    GameObject circlePoint = GameObject.Instantiate(Prefab_PointGrey);
                    circlePoint.transform.position = circlePointWorldPos3D;
                    circlePoint.transform.SetParent(DebugGO.transform);
                }
            }
        }
       
    }
}
