using UnityEngine;
using TMPro; // Para usar TMP_Text

public class Goal : MonoBehaviour
{
    public TMP_Text winText; // Texto de la UI que mostrará el mensaje

    void Start()
    {
        // Asegura que el texto esté apagado al iniciar
        if (winText != null)
            winText.gameObject.SetActive(false);
    }

    void Update()
    {
        ProjectileController proj = FindObjectOfType<ProjectileController>();
        if (proj == null) return;

        Vector2 pos = proj.transform.position;
        Vector2 goalPos = transform.position;

        float dist = Vector2.Distance(pos, goalPos);

        // Cuando el proyectil toca la meta
        if (dist < 0.5f && !proj.reachedGoal)
        {
            proj.reachedGoal = true;
            proj.Stop();

            if (winText != null)
            {
                winText.text = "¡Has llegado a la meta!";
                winText.gameObject.SetActive(true); // Mostrar el texto
            }
        }
    }
}
