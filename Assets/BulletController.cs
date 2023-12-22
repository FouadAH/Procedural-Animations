using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class BulletController : MonoBehaviour
{
    public float speed = 30f;
    public float damageValue = 10f;
    public Vector3 dir;

    public LayerMask damagableMask;
    public LayerMask obstacleMask;

    void Update()
    {
        transform.Translate(speed * dir * Time.deltaTime);
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (damagableMask == (damagableMask | (1 << other.gameObject.layer)))
        {
            other.GetComponent<Destructable>()?.TakeDamage(damageValue);
        }

        if (obstacleMask == (obstacleMask | (1 << other.gameObject.layer)))
        {
            Debug.Log(other.gameObject.layer);

            Destroy(gameObject);
        }

    }
}
