using UnityEngine;

public class WaterBlock : MonoBehaviour
{
    public float buoyancyForce = 3f;

    public void ApplyBuoyancy(ProjectileController projectile)
    {
        Vector2 pos = projectile.transform.position;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector2 size = sr.bounds.size;
        Vector2 waterPos = transform.position;

        bool insideX = pos.x > waterPos.x - size.x / 2 && pos.x < waterPos.x + size.x / 2;
        bool insideY = pos.y > waterPos.y - size.y / 2 && pos.y < waterPos.y + size.y / 2;

        if (insideX && insideY)
        {
            projectile.velocity += Vector2.up * buoyancyForce * Time.deltaTime;
        }
    }
}
