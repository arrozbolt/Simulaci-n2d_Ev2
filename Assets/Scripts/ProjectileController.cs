using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public Vector2 velocity;
    public float launchPower = 10f;
    public bool launched = false;
    public bool reachedGoal = false;

    void Update()
    {
        if (!launched && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mousePos - transform.position).normalized;
            velocity = dir * launchPower;
            launched = true;
        }

        if (launched && !reachedGoal)
        {
            ApplyGravities();
            transform.position += (Vector3)(velocity * Time.deltaTime);
        }
    }

    void ApplyGravities()
    {
        PlanetGravity[] planets = FindObjectsByType<PlanetGravity>(FindObjectsSortMode.None);
        foreach (var p in planets)
        {
            p.ApplyGravity(this);
        }

        WaterBlock[] waters = FindObjectsByType<WaterBlock>(FindObjectsSortMode.None);
        foreach (var w in waters)
        {
            w.ApplyBuoyancy(this);
        }
    }

    public void Stop()
    {
        launched = false;
        velocity = Vector2.zero;
    }
}
