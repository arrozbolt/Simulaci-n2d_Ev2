using UnityEngine;
using TMPro; // Para usar TMP_Text

public class Goal : MonoBehaviour
{
    public TMP_Text winText; // Texto de la UI que mostrará el mensaje
    private ProjectileController proj;
    private SpriteRenderer goalSprite;
    private SpriteRenderer projSprite;

    void Start()
    {
        if (winText != null)
            winText.gameObject.SetActive(false);

        proj = Object.FindFirstObjectByType<ProjectileController>();
        goalSprite = GetComponent<SpriteRenderer>();
        projSprite = proj != null ? proj.GetComponent<SpriteRenderer>() : null;
    }

    void Update()
    {
        if (proj == null)
        {
            proj = Object.FindFirstObjectByType<ProjectileController>();
            projSprite = proj != null ? proj.GetComponent<SpriteRenderer>() : null;
            if (proj == null) return;
        }

        // 1) Si ambos tienen SpriteRenderer: usar AABB con bounds
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

        if (winText != null)
        {
            winText.text = "¡Has llegado a la meta!";
            winText.gameObject.SetActive(true);
        }
    }
}
