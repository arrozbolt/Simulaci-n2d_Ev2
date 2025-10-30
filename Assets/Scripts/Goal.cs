using UnityEngine;
using TMPro; // Para usar TMP_Text

public class Goal : MonoBehaviour
{
    public TMP_Text winText; // Texto de la UI que mostrará el mensaje
    private ProjectileController proj;
    private SpriteRenderer goalSprite;
    private SpriteRenderer projSprite;

    // Movimiento en forma de "C" configurable
    [Header("Movimiento en C")]
    public bool moveEnabled = true;
    public float radiusX = 0.7f;   // radio horizontal (ancho del "C")
    public float radiusY = 1.0f;   // radio vertical (alto del "C")
    public float speed = 0.5f;     // controla la velocidad (ver comentario)
    public float phase = 0f;       // fase inicial
    private Vector3 startPos;      // posición inicial (origen del "C")

    void Start()
    {
        if (winText != null)
            winText.gameObject.SetActive(false);

        proj = Object.FindFirstObjectByType<ProjectileController>();
        goalSprite = GetComponent<SpriteRenderer>();
        projSprite = proj != null ? proj.GetComponent<SpriteRenderer>() : null;

        // Guardar posición inicial para el movimiento en "C"
        startPos = transform.position;
    }

    void Update()
    {
        // Actualizar posición si está habilitado (trayectoria en forma de "C")
        if (moveEnabled)
        {
            // u en [0,1] recorre el arco; Mathf.PingPong hace que vaya y vuelva
            float u = Mathf.PingPong(Time.time * speed + phase, 1f);

            // Reemplazando Mathf.Lerp con cálculo manual:
            // theta = inicio + (final - inicio) * factor
            float startAngle = Mathf.PI * 0.5f;    // +90°
            float endAngle = -Mathf.PI * 0.5f;     // -90°
            float angleRange = endAngle - startAngle; // -180° (-π)
            float theta = startAngle + angleRange * u;

            // Centro del arco desplazado a la izquierda
            Vector3 center = startPos + new Vector3(-radiusX, 0f, 0f);

            Vector3 p;
            p.x = center.x + radiusX * Mathf.Cos(theta);
            p.y = center.y + radiusY * Mathf.Sin(theta);
            p.z = startPos.z;
            transform.position = p;
        }

        if (proj == null)
        {
            proj = Object.FindFirstObjectByType<ProjectileController>();
            projSprite = proj != null ? proj.GetComponent<SpriteRenderer>() : null;
            if (proj == null) return;
        }

        // 1) Si ambos tienen SpriteRenderer: usar AABB con bounds (más fiable que comparar centros)
        if (goalSprite != null && projSprite != null)
        {
            Rect rectGoal = new Rect((Vector2)goalSprite.bounds.center - (Vector2)goalSprite.bounds.size * 0.5f, (Vector2)goalSprite.bounds.size);
            Rect rectProj = new Rect((Vector2)projSprite.bounds.center - (Vector2)projSprite.bounds.size * 0.5f, (Vector2)projSprite.bounds.size);

            if (!proj.reachedGoal && rectGoal.Overlaps(rectProj))
            {
                ReachGoal();
            }

            return;
        }

        // 2) Fallback: comparación por distancia con radios basados en bounds/extents
        Vector2 pos = proj.transform.position;
        Vector2 goalPos = transform.position;

        float rGoal = (goalSprite != null) ? Mathf.Max(goalSprite.bounds.extents.x, goalSprite.bounds.extents.y) : 0.5f;
        float rProj = (projSprite != null) ? Mathf.Max(projSprite.bounds.extents.x, projSprite.bounds.extents.y) : 0.25f;
        float combined = rGoal + rProj;

        if ((pos - goalPos).sqrMagnitude < combined * combined && !proj.reachedGoal)
        {
            ReachGoal();
        }
    }

    void ReachGoal()
    {
        proj.reachedGoal = true;
        proj.Stop();

        // Detener el movimiento al ganar y fijar la posición actual
        moveEnabled = false;
        startPos = transform.position;

        if (winText != null)
        {
            winText.text = "¡Has llegado a la meta!";
            winText.gameObject.SetActive(true);
        }
    }
}
