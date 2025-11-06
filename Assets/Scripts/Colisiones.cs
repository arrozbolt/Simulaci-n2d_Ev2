using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Detecta colisiones AABB entre SpriteRenderer y muestra un feedback visual simple.
public class ColisionPorCodigo : MonoBehaviour
{
    // Sprites a comprobar (asignar en el inspector).
    public List<SpriteRenderer> objetos = new List<SpriteRenderer>();

    // objetos con los que se debe rebotar (separada de 'objetos')
    [Tooltip("Objetos que provocan rebote al colisionar con cualquiera de 'objetos'")]
    public List<SpriteRenderer> objetosRebote = new List<SpriteRenderer>();

    // Fuerza del impulso aplicado al proyectil (vector de signo)
    [Tooltip("Magnitud del impulso aplicado al proyectil al chocar con un objeto de 'objetosRebote'")]
    public float impulseStrength = 5f;

    // Small offset para sacar al proyectil de la superposición
    [Tooltip("Desplazamiento mínimo para separar el proyectil del objeto tras el impacto")]
    public float separationOffset = 0.02f;

    // Feedback visual
    public Color flashColor = Color.red;
    public float flashDuration = 0.12f;

    // "Punch" de escala al impactar
    [Tooltip("Factor de aumento de escala (por ejemplo 0.15 = +15%)")]
    public float punchAmount = 0.15f;
    public float punchDuration = 0.12f;

    // Colisiones del frame anterior (para disparar efecto sólo al inicio)
    HashSet<(SpriteRenderer, SpriteRenderer)> colisionesPrevias = new HashSet<(SpriteRenderer, SpriteRenderer)>();

    // Colisiones previas entre 'objetos' y 'objetosRebote' (evitar múltiples impulsos por frame)
    HashSet<(SpriteRenderer, SpriteRenderer)> rebotesPrevios = new HashSet<(SpriteRenderer, SpriteRenderer)>();

    // Evita lanzar varias corrutinas sobre el mismo sprite
    HashSet<SpriteRenderer> spritesEnEfecto = new HashSet<SpriteRenderer>();

    void Update()
    {
        var colisiones = ObtenerColisiones(objetos);

        // Disparar efecto sólo cuando la colisión comienza
        foreach (var pair in colisiones)
        {
            if (!colisionesPrevias.Contains(pair))
            {
                if (pair.Item1 != null) TriggerImpactVisual(pair.Item1);
                if (pair.Item2 != null) TriggerImpactVisual(pair.Item2);
            }
        }

        colisionesPrevias = new HashSet<(SpriteRenderer, SpriteRenderer)>(colisiones);

        // --- Gestión de impulsos entre 'objetos' y 'objetosRebote' ---
        var paresRebote = ObtenerColisionesEntreListas(objetos, objetosRebote);

        foreach (var pair in paresRebote)
        {
            if (!rebotesPrevios.Contains(pair))
            {
                if (pair.Item1 != null) TriggerImpactVisual(pair.Item1);
                if (pair.Item2 != null) TriggerImpactVisual(pair.Item2);

                AplicarImpulso(pair.Item1, pair.Item2);
            }
        }

        rebotesPrevios = new HashSet<(SpriteRenderer, SpriteRenderer)>(paresRebote);
    }

    // Lanza la corrutina de feedback si no está ya en curso
    void TriggerImpactVisual(SpriteRenderer sr)
    {
        if (sr == null) return;
        if (spritesEnEfecto.Contains(sr)) return;

        StartCoroutine(ImpactVisualCoroutine(sr));
    }

    // Corrutina: flash de color + punch de escala
    IEnumerator ImpactVisualCoroutine(SpriteRenderer sr)
    {
        if (sr == null) yield break;

        spritesEnEfecto.Add(sr);

        Color original = sr.color;
        sr.color = flashColor;

        Vector3 originalScale = sr.transform.localScale;
        Vector3 targetScale = originalScale * (1f + punchAmount);

        float half = punchDuration * 0.5f;
        float elapsed = 0f;

        // Escalar hacia arriba (interpolación manual y smoothstep manual)
        while (elapsed < half)
        {
            if (sr == null) break;
            elapsed += Time.deltaTime;
            float t = Clamp01(elapsed / half);
            float s = SmoothStep01(t);

            // Interpolación manual por componentes (sin usar Lerp)
            sr.transform.localScale = new Vector3(
                originalScale.x + (targetScale.x - originalScale.x) * s,
                originalScale.y + (targetScale.y - originalScale.y) * s,
                originalScale.z + (targetScale.z - originalScale.z) * s
            );

            yield return null;
        }

        // Volver a escala original
        elapsed = 0f;
        while (elapsed < half)
        {
            if (sr == null) break;
            elapsed += Time.deltaTime;
            float t = Clamp01(elapsed / half);
            float s = SmoothStep01(t);
            sr.transform.localScale = Lerp(targetScale, originalScale, s);
            yield return null;
        }

        // Mantener flash un poco
        yield return new WaitForSeconds(flashDuration);

        if (sr != null) sr.color = original;
        if (sr != null) sr.transform.localScale = originalScale;

        spritesEnEfecto.Remove(sr);
    }

    // Devuelve pares únicos (i,j con j>i) que se solapan
    List<(SpriteRenderer, SpriteRenderer)> ObtenerColisiones(List<SpriteRenderer> lista)
    {
        var resultado = new List<(SpriteRenderer, SpriteRenderer)>();
        if (lista == null) return resultado;

        int n = lista.Count;
        for (int i = 0; i < n; i++)
        {
            var a = lista[i];
            if (a == null) continue;
            for (int j = i + 1; j < n; j++)
            {
                var b = lista[j];
                if (b == null) continue;

                if (SeSuperponen(a, b))
                {
                    resultado.Add((a, b));
                }
            }
        }

        return resultado;
    }

    // Comprueba colisiones entre dos listas (pares (a,b) donde a ∈ listaA y b ∈ listaB)
    List<(SpriteRenderer, SpriteRenderer)> ObtenerColisionesEntreListas(List<SpriteRenderer> listaA, List<SpriteRenderer> listaB)
    {
        var resultado = new List<(SpriteRenderer, SpriteRenderer)>();
        if (listaA == null || listaB == null) return resultado;

        for (int i = 0; i < listaA.Count; i++)
        {
            var a = listaA[i];
            if (a == null) continue;
            for (int j = 0; j < listaB.Count; j++)
            {
                var b = listaB[j];
                if (b == null) continue;

                if (SeSuperponen(a, b))
                {
                    resultado.Add((a, b));
                }
            }
        }

        return resultado;
    }

    // Comprobación AABB usando bounds del SpriteRenderer
    bool SeSuperponen(SpriteRenderer a, SpriteRenderer b)
    {
        Vector2 posA = a.transform.position;
        Vector2 posB = b.transform.position;
        Vector2 sizeA = a.bounds.size;
        Vector2 sizeB = b.bounds.size;

        Rect rectA = new Rect(posA - sizeA / 2f, sizeA);
        Rect rectB = new Rect(posB - sizeB / 2f, sizeB);

        return rectA.Overlaps(rectB);
    }

    // Aplica un impulso al ProjectileController encontrado en uno de los dos sprites.
    // El impulso es un vector de signo determinado por la posición relativa.
    // Además separa al proyectil del objeto para evitar que quede solapado y atraviese otras paredes.
    void AplicarImpulso(SpriteRenderer a, SpriteRenderer b)
    {
        if (a == null || b == null) return;

        // Priorizar detectar ProjectileController en 'a' luego en 'b'
        ProjectileController pc = a.GetComponent<ProjectileController>();
        SpriteRenderer other = b;
        if (pc == null)
        {
            pc = b.GetComponent<ProjectileController>();
            other = a;
        }

        if (pc == null) return; // ninguno es proyectil

        // No aplicar si ya llegó a la meta
        if (pc.reachedGoal) return;

        // Dirección aproximada sin usar funciones matemáticas: signo de la diferencia de centros
        Vector2 dir = (Vector2)pc.transform.position - (Vector2)other.transform.position;

        float sx = dir.x > 0f ? 1f : (dir.x < 0f ? -1f : 0f);
        float sy = dir.y > 0f ? 1f : (dir.y < 0f ? -1f : 0f);

        // Si la diferencia es casi cero, usar impulso hacia arriba
        if (sx == 0f && sy == 0f)
        {
            sy = 1f;
        }

        Vector2 impulso = new Vector2(sx * impulseStrength, sy * impulseStrength);

        // Evitar que la componente de velocidad apunte hacia la pared:
        float vx = pc.velocity.x;
        float vy = pc.velocity.y;
        if (sx != 0f && vx * sx < 0f)
        {
            vx = -vx; // invertir componente que apunta hacia la pared
        }
        if (sy != 0f && vy * sy < 0f)
        {
            vy = -vy;
        }

        // Aplicar impulso sumándolo a la velocidad ajustada.
        pc.velocity = new Vector2(vx, vy) + impulso;

        // Separar ligeramente al proyectil fuera del objeto para evitar solapamiento
        float ox = sx * separationOffset;
        float oy = sy * separationOffset;
        pc.transform.position = (Vector2)pc.transform.position + new Vector2(ox, oy);

        // Asegurar que el proyectil está marcado como lanzado para que siga siendo actualizado
        pc.launched = true;
    }

    // Helpers manuales (sin usar Mathf)
    static float Clamp01(float v)
    {
        if (v <= 0f) return 0f;
        if (v >= 1f) return 1f;
        return v;
    }

    // SmoothStep para [0,1] -> t*t*(3 - 2*t)
    static float SmoothStep01(float t)
    {
        // asume t en [0,1]
        return (t * t) * (3f - 2f * t);
    }

    // Lerp manual para Vector3
    static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }
}