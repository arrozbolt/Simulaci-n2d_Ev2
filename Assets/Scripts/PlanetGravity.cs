using UnityEngine;

public class PlanetGravity : MonoBehaviour
{
    public float gravityStrength = 5f;
    public float influenceRadius = 5f;

    public void ApplyGravity(ProjectileController projectile)
    {
        Vector2 dir = (Vector2)transform.position - (Vector2)projectile.transform.position;
        float dist = dir.magnitude;

        if (dist < influenceRadius)
        {
            float force = gravityStrength * (1 - (dist / influenceRadius)); // regla de tres simple
            projectile.velocity += dir.normalized * force * Time.deltaTime;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, influenceRadius);
    }
}
