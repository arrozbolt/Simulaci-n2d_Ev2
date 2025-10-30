using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Detecta colisiones AABB entre SpriteRenderer y muestra un feedback visual simple.
public class ColisionPorCodigo : MonoBehaviour
{
    // Sprites a comprobar (asignar en el inspector).
    public List<SpriteRenderer> objetos = new List<SpriteRenderer>();

    // Feedback visual
    public Color flashColor = Color.red;
    public float flashDuration = 0.12f;

    // "Punch" de escala al impactar
    [Tooltip("Factor de aumento de escala (por ejemplo 0.15 = +15%)")]
    public float punchAmount = 0.15f;
    public float punchDuration = 0.12f;

    // Colisiones del frame anterior (para disparar efecto sólo al inicio)
    HashSet<(SpriteRenderer, SpriteRenderer)> colisionesPrevias = new HashSet<(SpriteRenderer, SpriteRenderer)>();

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
            sr.transform.localScale = Lerp(originalScale, targetScale, s);
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