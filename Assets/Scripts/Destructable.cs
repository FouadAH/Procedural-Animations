using UnityEngine;

public class Destructable : MonoBehaviour
{
    public float health;
    public ParticleSystem damageParticles;

    public void TakeDamage(float damageAmount)
    {
        Instantiate(damageParticles, transform.position, Quaternion.identity).GetComponent<ParticleSystem>().Play();

        health -= damageAmount;
        if(health < 0)
        {
            Destroy(gameObject);
        }
    }
}
