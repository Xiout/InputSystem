using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private bool _isTurn;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (_isTurn)
        {
            gameObject.transform.RotateAround(gameObject.transform.position, Vector3.up, 30);
            _isTurn = false;
        }
    }

    void OnTurn(InputValue value)
    {
        _isTurn = value.isPressed;
    }
}
