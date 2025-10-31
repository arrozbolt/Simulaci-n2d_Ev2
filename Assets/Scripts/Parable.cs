using System.Collections;
using UnityEngine;

public class Parable : MonoBehaviour
{
    public Transform m_pivot;
    public float initialSpeed = 10f;
    public float gravity = 9.8f;
    
    public float time;
    public bool m_start;

    public float m_maxParable;
    public float m_minParable;
    public float m_duration;

    // Cachear referencia al proyectil en escena
    private ProjectileController proj;

    void Start()
    {
        transform.position = m_pivot.position;
        StartCoroutine(TimeCor());

        proj = Object.FindFirstObjectByType<ProjectileController>();
    } 

    void Update()
    {
        // Intentar obtener la referencia si aún no existe
        if (proj == null)
            proj = Object.FindFirstObjectByType<ProjectileController>();

        // Si existe el proyectil y llegó a la meta, detener el movimiento de este objeto
        if (proj != null && proj.reachedGoal)
        {
            m_start = false;
            return;
        }

        if (m_start) StartMove();
    }

    bool repeat;
    void StartMove()
    {
        Vector2 dir = m_pivot.right.normalized;

        Vector2 gravityVec = (Vector2)m_pivot.TransformDirection(Vector3.down) * gravity;

        float x = m_pivot.position.x + dir.x * initialSpeed * time + 0.5f * gravityVec.x * time * time;
        float y = m_pivot.position.y + dir.y * initialSpeed * time + 0.5f * gravityVec.y * time * time;

        transform.position = new Vector2(x, y);
    }
    
    IEnumerator TimeCor()
    {
        for (float i = 0; i < m_duration; i += Time.deltaTime)
        {
            float t = i / m_duration;
            time = Mathf.Lerp(m_minParable, m_maxParable, t);
            yield return null;
        }

        time = m_maxParable;

        for (float i = 0; i < m_duration; i += Time.deltaTime)
        {
            float t = i / m_duration;
            time = Mathf.Lerp(m_maxParable, m_minParable, t);
            yield return null;
        }

        time = m_minParable;

        StartCoroutine(TimeCor());
    }

    void OnDrawGizmos()
    {
        if (m_pivot == null) return;
        Gizmos.color = Color.red;

        Vector3 prevPos = m_pivot.position;
        Vector2 dir = m_pivot.right.normalized;

        // gravedad rotada para el gizmo
        Vector2 gravityVec = (Vector2)m_pivot.TransformDirection(Vector3.down) * gravity;

        for (float t = 0; t < 0.6f; t += 0.1f)
        {
            float x = m_pivot.position.x + dir.x * initialSpeed * t + 0.5f * gravityVec.x * t * t;
            float y = m_pivot.position.y + dir.y * initialSpeed * t + 0.5f * gravityVec.y * t * t;

            Vector3 newPos = new Vector3(x, y, 0);
            Gizmos.DrawLine(prevPos, newPos);
            prevPos = newPos;
        }
    }
}