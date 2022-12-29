using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private bool _isTurn;
    private CircleAction controls;

    void Awake()
    {
        controls = new CircleAction();
        controls.Enable();
    }

    void Start()
    {
        //controls.Player.Turn.performed += Turn;
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


    void Update()
    {
        if (controls.Player.Turn.triggered)
        {
            gameObject.transform.RotateAround(gameObject.transform.position, Vector3.up, 30);
        }
    }
}
