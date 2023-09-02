using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpiderController : MonoBehaviour
{
    public Transform orientation;
    
    public GameObject turretAimConstraint;

    public GameObject bulletPrefab;

    public GameObject turretShoot_Right;
    public GameObject turretShoot_left;

    public float _speed = 1f;
    public float _speedRotation = 10f;

    private Rigidbody _rigidbody;
    Vector3 m_Input;
    Vector3 m_InputRotation;
    int rotationDirection;

    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 direction = orientation.forward;

        if (Input.GetAxisRaw("Vertical") != 0)
        {
            _rigidbody.MovePosition(transform.position + _speed * Time.fixedDeltaTime * orientation.forward * Mathf.Sign(Input.GetAxisRaw("Vertical")));
        }

        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            _rigidbody.MovePosition(transform.position + _speed * Time.fixedDeltaTime * orientation.right * Mathf.Sign(Input.GetAxisRaw("Horizontal")));
        }

        transform.Rotate(new Vector3(0, _speedRotation * Time.fixedDeltaTime * rotationDirection, 0));
    }

    private void Update()
    {
        m_Input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        m_InputRotation = new Vector3();

        bool leftRotation = Input.GetKey(KeyCode.Q);
        bool rightRotation = Input.GetKey(KeyCode.E);
        bool shootInput = Input.GetKey(KeyCode.Space);

        if (leftRotation && !rightRotation) 
        {
            rotationDirection = -1;
        }
        else if(rightRotation && !leftRotation) 
        { 
            rotationDirection = 1;
        }
        else
        {
            rotationDirection = 0;
        }


        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));
        turretAimConstraint.transform.position = mousePos;
        var dir = mousePos - turretShoot_Right.transform.position;

        if (shootInput)
        {
            Instantiate(bulletPrefab, turretShoot_Right.transform.position, Quaternion.identity).GetComponent<BulletController>().dir = dir;
            Instantiate(bulletPrefab, turretShoot_left.transform.position, Quaternion.identity).GetComponent<BulletController>().dir = dir; ;
        }

    }
}