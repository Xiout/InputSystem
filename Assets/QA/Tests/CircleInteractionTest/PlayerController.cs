using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerController : MonoBehaviour
{
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
    private GeometryHelp.CircleMethod circleMethod;

    private bool isMouse;

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

        //Get the accuracyPercent and circleMethod parameters from the interaction
        string interactionsStr = controls.Player.Turn.interactions;
        string cut = "Circle(";
        int index = interactionsStr.IndexOf(cut);
        if (index != -1)
        {
            interactionsStr = interactionsStr.Substring(index + cut.Length);

            index = interactionsStr.IndexOf(")");
            interactionsStr = interactionsStr.Substring(0, interactionsStr.Length-(interactionsStr.Length - index));

            var splits = interactionsStr.Split(',');
            for(int i=0; i< splits.Length; ++i)
            {
                cut = "accuracyPercent=";
                index = splits[i].IndexOf(cut);
                if (index != -1)
                {
                    splits[i] = splits[i].Substring(index + cut.Length);
                    float result = -1;

                    if (float.TryParse(splits[i], out result))
                    {
                        accuracyPercent = result;
                    }
                    continue;
                }

                cut = "circleMethod=";
                index = splits[i].IndexOf(cut);
                if (index != -1)
                {
                    splits[i] = splits[i].Substring(index + cut.Length);
                    GeometryHelp.CircleMethod method;
                    if (Enum.TryParse<GeometryHelp.CircleMethod>(splits[i], out method))
                    {
                        circleMethod = method;
                    }
                    return;
                }
            }
            
        }
        //in case the string reading fails, we use the default value.
        if (accuracyPercent == -1) { accuracyPercent = InputSystem.settings.defaultAccuracyPercent; }
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
        //wipe out previously drawn debug points so only one action is displayed t the time
        GameObject.Destroy(DebugGO);
        DebugGO = new GameObject();
        DebugGO.name = "DEBUG";

        isMouse= controls.Player.Turn.activeControl.device.description.deviceClass == "Mouse";
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
            //The action is canceled, draw the circles to visually compare the acquired points with the circles estimation
            DrawHelpCircles();

            //Get and draw all the incorrect points to ease the see which points wa considered as false;
            var incorrectPoints = GeometryHelp.GetIncorrectPointsCircle_DEBUG(GesturePoints, accuracyPercent, circleMethod);
            for(int i=0; i< incorrectPoints.Count; ++i)
            {
                Vector3 incorrectPoint = new Vector3(incorrectPoints[i].x, incorrectPoints[i].y, defaultZCoord2D);
                var incorrectPointGO = DrawDebugPoint(incorrectPoint, Prefab_PointRed);
                incorrectPointGO.transform.Translate(0, 0, -0.01f);
            }
        }
        
        hadBeenPerformed = false;
    }

    void Update()
    {
        //Use of the action to rotate the player GameObject 
        if (controls.Player.Turn.triggered)
        {
            gameObject.transform.RotateAround(gameObject.transform.position, Vector3.up, 30);
        }

        //DEBUG : display of sprite as visual aid
        //Orange points : points acquired when the action is started but not performed yet
        //Green  points : points acquired after the action is performed but when is not had been canceled yet (ie. still holding the control)
        //Grey   points : points of the circles (an their center) in between which all the points should lay so the acquired points are considered to form a circle
        //Red    points : points that stands outside of the 2 circles

        //Update defaultZCoord2D in case the camera had changed of position
        defaultZCoord2D = Camera.main.nearClipPlane + 1;

        Vector2 devicePos2D = controls.Player.Turn.ReadValue<Vector2>();
        if(devicePos2D != null && isStarted)
        {
            //As we cannot have access to the list of acquired points during the action, rebuild this list using ReadValue on the Turn action
            //To ease calculation, doublon of points are ignored
            if (!GesturePoints.Contains(devicePos2D))
            {
                GesturePoints.Add(devicePos2D);
            }

            //Draw acquired points in green if performed or orange if not
            Vector3 mousePos3D = new Vector3(devicePos2D.x, devicePos2D.y, defaultZCoord2D);
            GameObject prefab = Prefab_PointOrange;
            if (isPerformed)
            {
                prefab = Prefab_PointGreen;
            }
            DrawDebugPoint(mousePos3D, prefab);

            //If the action just had been performed, draw the circles to visually compare the acquired points with the circles estimation
            if (controls.Player.Turn.WasPerformedThisFrame())
            {
                DrawHelpCircles();
                hadBeenPerformed = true;
            }
        }
    }

    /// <summary>
    /// Draw limite circles in between which all the points of GesturePoints should lay to be considered as a circle
    /// </summary>
    private void DrawHelpCircles()
    {
        List<GameObject> GOs = new List<GameObject>();
        for (int i = 0; i < DebugGO.transform.childCount; ++i)
        {
            Transform child = DebugGO.transform.GetChild(i);
            GOs.Add(child.gameObject);
        }

        var circle = GeometryHelp.GetCircle(GesturePoints, circleMethod);
        if (circle == null)
        {
            return;
        }

        Vector3 center = new Vector3(circle.Center.x, circle.Center.y, defaultZCoord2D);
        DrawDebugPoint(center, Prefab_PointGrey);

        for (float theta = -Mathf.PI; theta < Mathf.PI; theta += 0.1f)
        {
            float accuracyOffset = circle.Radius * 2 * (100 - accuracyPercent) / 100;

            Vector3 bigCirclePoint = new Vector3(
                (circle.Radius + accuracyOffset / 2) * Mathf.Sin(theta) + circle.Center.x, 
                (circle.Radius + accuracyOffset / 2) * Mathf.Cos(theta) + circle.Center.y, 
                defaultZCoord2D
            );
            DrawDebugPoint(bigCirclePoint, Prefab_PointGrey);

            Vector3 smallCirclePoint = new Vector3(
                (circle.Radius - accuracyOffset / 2) * Mathf.Sin(theta) + circle.Center.x,
                (circle.Radius - accuracyOffset / 2) * Mathf.Cos(theta) + circle.Center.y,
                defaultZCoord2D
            );
            DrawDebugPoint(smallCirclePoint, Prefab_PointGrey);
        }
    }

    /// <summary>
    /// Instanciate an instance of the given prefab at the given position converted from mouse coordinate to world coordinate and set the Debug GameObject as parent
    /// </summary>
    /// <param name="coordinateDevice3D"></param>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private GameObject DrawDebugPoint(Vector3 coordinateDevice3D, GameObject prefab)
    {
        if (!isMouse)
        {
            Vector2 screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);
            coordinateDevice3D = new Vector3(coordinateDevice3D.x * 100 + screenMiddle.x, coordinateDevice3D.y * 100 + screenMiddle.y, coordinateDevice3D.z);
        }

        Vector3 coordinateWorld3D = Camera.main.ScreenToWorldPoint(coordinateDevice3D);
        GameObject drawnPoint = GameObject.Instantiate(prefab);
        drawnPoint.transform.position = coordinateWorld3D;
        drawnPoint.transform.SetParent(DebugGO.transform);

        return drawnPoint;
    }
}
