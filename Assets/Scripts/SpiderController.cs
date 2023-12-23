using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpiderController : MonoBehaviour
{
    public Transform orientation;
    
    public GameObject turretAimConstraint;

    public GameObject bulletPrefab;

    public GameObject turretShoot_Right;
    public GameObject turretShoot_left;
    public float gravity;

    public float _speed = 1f;
    public float jumpForce = 10f;

    public float _speedRotation = 10f;

    public float shootCooldown;
    float lastShootTime;
    bool canShoot;

    private CharacterController characterController;
    Vector3 inputVector;
    Vector3 movementDirection;
    int rotationDirection;


    Vector3 targetVelocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        targetVelocity = Vector3.Lerp(targetVelocity, movementDirection * _speed * Time.fixedDeltaTime, 0.1f);

        if (!characterController.isGrounded)
        {
            targetVelocity.y -= gravity;
        }

        characterController.Move(targetVelocity);

        transform.Rotate(new Vector3(0, _speedRotation * Time.fixedDeltaTime * rotationDirection, 0));
    }

    private void Update()
    {
        inputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        movementDirection = orientation.forward * inputVector.z + orientation.right * inputVector.x;

        RotationInput();
        Shoot();
        Jump();
    }

    void RotationInput()
    {
        bool leftRotation = Input.GetKey(KeyCode.Q);
        bool rightRotation = Input.GetKey(KeyCode.E);

        if (leftRotation && !rightRotation)
        {
            rotationDirection = -1;
        }
        else if (rightRotation && !leftRotation)
        {
            rotationDirection = 1;
        }
        else
        {
            rotationDirection = 0;
        }
    }
    void Jump()
    {
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);

        if (jumpInput && characterController.isGrounded)
        {
            targetVelocity.y = jumpForce;
        }
    }

    void Shoot()
    {
        bool shootInput = Input.GetMouseButton(0);

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));
        turretAimConstraint.transform.position = mousePos;
        var dir = mousePos - turretShoot_Right.transform.position;

        if (Time.time > lastShootTime + shootCooldown)
        {
            canShoot = true;
        }
        else if (Time.time < lastShootTime + shootCooldown)
        {
            canShoot = false;
        }

        if (shootInput && canShoot)
        {
            lastShootTime = Time.time;
            Instantiate(bulletPrefab, turretShoot_Right.transform.position, Quaternion.identity).GetComponent<BulletController>().dir = dir;
            Instantiate(bulletPrefab, turretShoot_left.transform.position, Quaternion.identity).GetComponent<BulletController>().dir = dir; ;
        }
    }

}