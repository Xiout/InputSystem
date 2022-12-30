using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerController : MonoBehaviour
{
    private bool _isTurn;
    private CircleAction controls;

    public GameObject Prefab_PointOrange;
    public GameObject Prefab_PointGreen;
    public GameObject Prefab_PointGrey;
    public GameObject Prefab_PointRed;

    private GameObject DebugGO;
    private List<Vector2> GesturePoints;

    private bool isStarted;
    private bool isPerformed;
    private bool hadBeenPerformed;

    private float defaultZCoord2D=0;
    private float accuracyPercent=-1;

    void Awake()
    {
        controls = new CircleAction();
        controls.Enable();
    }

    void Start()
    {
        DebugGO = GameObject.Find("DEBUG");
        //Add listener to action events
        controls.Player.Turn.started += ctx => OnTurnStarted(ctx);
        controls.Player.Turn.performed += ctx => OnTurnPerformed(ctx);
        controls.Player.Turn.canceled += ctx => OnTurnCanceled(ctx);

        //Get the accuracyPercent from the interaction
        string interactionsStr = controls.Player.Turn.interactions;
        string cut = "Circle(";
        int index = interactionsStr.IndexOf(cut);
        if (index != -1)
        {
            interactionsStr = interactionsStr.Substring(index + cut.Length);
            Debug.Log("Step1 : "+interactionsStr);

            index = interactionsStr.IndexOf(")");
            interactionsStr = interactionsStr.Substring(0, interactionsStr.Length-(interactionsStr.Length - index));
            Debug.Log("Step2 : "+interactionsStr);

            cut = "accuracyPercent=";
            index = interactionsStr.IndexOf(cut);
            if(index != -1)
            {
                interactionsStr = interactionsStr.Substring(index + cut.Length);
                float result = -1;

                Debug.Log("Step3 : "+interactionsStr);
                if(float.TryParse(interactionsStr, out result))
                {
                    accuracyPercent = result;
                }
            }
        }
        if (accuracyPercent == -1) { accuracyPercent = 90; }
        Debug.Log($"AccuracyPercent : {accuracyPercent}");
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

        if (!hadBeenPerformed)
        {
            DrawHelpCircle();
            var incorrectPoints = GeometryHelp.GetIncorrectPointsCircle(GesturePoints, accuracyPercent);
            for(int i=0; i< incorrectPoints.Count; ++i)
            {
                Vector3 incorrectPointMouse3D = new Vector3(incorrectPoints[i].x, incorrectPoints[i].y, defaultZCoord2D);
                Vector3 incorrectPointWorld3D = Camera.main.ScreenToWorldPoint(incorrectPointMouse3D);
                GameObject incorrectPoint = GameObject.Instantiate(Prefab_PointRed);
                incorrectPoint.transform.position = incorrectPointWorld3D;
                incorrectPoint.transform.Translate(0, 0, -0.01f);
                incorrectPoint.transform.SetParent(DebugGO.transform);
            }
        }
        
        hadBeenPerformed = false;
    }

    void Update()
    {
        defaultZCoord2D = Camera.main.nearClipPlane + 1;

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

            Vector3 mousePos3D = new Vector3(mousePos2D.x, mousePos2D.y, defaultZCoord2D);
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
                DrawHelpCircle();
                hadBeenPerformed = true;
            }
        }
       
    }

    private void DrawHelpCircle()
    {
        List<GameObject> GOs = new List<GameObject>();
        for (int i = 0; i < DebugGO.transform.childCount; ++i)
        {
            Transform child = DebugGO.transform.GetChild(i);
            GOs.Add(child.gameObject);
        }
        GeometryHelp.FindFurthestGameObject(GOs);

        var circle = GeometryHelp.GetCircleFurthestPoints(GesturePoints);

        Vector3 centerMouse3D = new Vector3(circle.Center.x, circle.Center.y, defaultZCoord2D);
        Vector3 centerWorldPos3D = Camera.main.ScreenToWorldPoint(centerMouse3D);
        GameObject circleCenterPoint = GameObject.Instantiate(Prefab_PointGrey);
        circleCenterPoint.transform.position = centerWorldPos3D;
        circleCenterPoint.transform.SetParent(DebugGO.transform);

        for (float theta = -Mathf.PI; theta < Mathf.PI; theta += 0.1f)
        {
            float accuracyOffset = circle.Radius * 2 * (100 - accuracyPercent) / 100;
            Vector3 circlePointMouse3D = new Vector3((circle.Radius + accuracyOffset / 2) * Mathf.Sin(theta) + circle.Center.x, (circle.Radius + accuracyOffset / 2) * Mathf.Cos(theta) + circle.Center.y, defaultZCoord2D);
            Vector3 circlePointWorldPos3D = Camera.main.ScreenToWorldPoint(circlePointMouse3D);
            GameObject circlePoint = GameObject.Instantiate(Prefab_PointGrey);
            circlePoint.transform.position = circlePointWorldPos3D;
            circlePoint.transform.SetParent(DebugGO.transform);

            circlePointMouse3D = new Vector3((circle.Radius - accuracyOffset/2 )* Mathf.Sin(theta) + circle.Center.x, (circle.Radius - accuracyOffset/2) * Mathf.Cos(theta) + circle.Center.y, defaultZCoord2D);
            circlePointWorldPos3D = Camera.main.ScreenToWorldPoint(circlePointMouse3D);
            circlePoint = GameObject.Instantiate(Prefab_PointGrey);
            circlePoint.transform.position = circlePointWorldPos3D;
            circlePoint.transform.SetParent(DebugGO.transform);
        }
    }
}
