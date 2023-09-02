using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float speed = 30f;
    public Vector3 dir;
    void Update()
    {
        transform.Translate(speed * dir * Time.deltaTime);
    }
}
