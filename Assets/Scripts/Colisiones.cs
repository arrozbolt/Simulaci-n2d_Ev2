using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColisionPorCodigo : MonoBehaviour
{
	// --- Configuración pública ---
	public List<SpriteRenderer> objetos = new List<SpriteRenderer>();
	public List<SpriteRenderer> objetosRebote = new List<SpriteRenderer>();

	public float impulseStrength =5f;
	public float separationOffset =0.02f;

	public Color flashColor = Color.red;
	public float flashDuration =0.12f;

	public float punchAmount =0.15f;
	public float punchDuration =0.12f;

	public float bounceDamping =0.8f;
	public float maxSpeed =20f;

	// --- Estado interno ---
	HashSet<(SpriteRenderer, SpriteRenderer)> colisionesPrevias = new HashSet<(SpriteRenderer, SpriteRenderer)>();
	HashSet<(SpriteRenderer, SpriteRenderer)> rebotesPrevios = new HashSet<(SpriteRenderer, SpriteRenderer)>();
	HashSet<SpriteRenderer> spritesEnEfecto = new HashSet<SpriteRenderer>();

	// Reutilizar listas para evitar GC
	List<(SpriteRenderer, SpriteRenderer)> tempColisiones = new List<(SpriteRenderer, SpriteRenderer)>();
	List<(SpriteRenderer, SpriteRenderer)> tempParesRebote = new List<(SpriteRenderer, SpriteRenderer)>();

	// Caché de ProjectileController por SpriteRenderer
	readonly Dictionary<SpriteRenderer, ProjectileController> pcCache = new Dictionary<SpriteRenderer, ProjectileController>();

	// --- Bucle principal: detectar colisiones y aplicar efectos/impulsos ---
	void Update()
	{
		tempColisiones.Clear();
		ObtenerColisiones(objetos, tempColisiones);

		for (int i =0; i < tempColisiones.Count; i++)
		{
			var pair = tempColisiones[i];
			if (!colisionesPrevias.Contains(pair))
			{
				if (pair.Item1 != null) TriggerImpactVisual(pair.Item1);
				if (pair.Item2 != null) TriggerImpactVisual(pair.Item2);
			}
		}

		colisionesPrevias.Clear();
		for (int i =0; i < tempColisiones.Count; i++) colisionesPrevias.Add(tempColisiones[i]);

		tempParesRebote.Clear();
		ObtenerColisionesEntreListas(objetos, objetosRebote, tempParesRebote);

		for (int i =0; i < tempParesRebote.Count; i++)
		{
			var pair = tempParesRebote[i];
			if (!rebotesPrevios.Contains(pair))
			{
				if (pair.Item1 != null) TriggerImpactVisual(pair.Item1);
				if (pair.Item2 != null) TriggerImpactVisual(pair.Item2);

				AplicarImpulso(pair.Item1, pair.Item2);
			}
		}

		rebotesPrevios.Clear();
		for (int i =0; i < tempParesRebote.Count; i++) rebotesPrevios.Add(tempParesRebote[i]);
	}

	// --- Efecto visual: flash + "punch" ---
	void TriggerImpactVisual(SpriteRenderer sr)
	{
		if (sr == null) return;
		if (spritesEnEfecto.Contains(sr)) return;

		StartCoroutine(ImpactVisualCoroutine(sr));
	}

	IEnumerator ImpactVisualCoroutine(SpriteRenderer sr)
	{
		if (sr == null) yield break;

		spritesEnEfecto.Add(sr);

		Color original = sr.color;
		sr.color = flashColor;

		Vector3 originalScale = sr.transform.localScale;
		Vector3 targetScale = originalScale * (1f + punchAmount);

		float half = punchDuration *0.5f;
		float elapsed =0f;

		while (elapsed < half)
		{
			if (sr == null) break;
			elapsed += Time.deltaTime;
			float t = Clamp01(elapsed / half);
			float s = SmoothStep01(t);

			sr.transform.localScale = new Vector3(
				originalScale.x + (targetScale.x - originalScale.x) * s,
				originalScale.y + (targetScale.y - originalScale.y) * s,
				originalScale.z + (targetScale.z - originalScale.z) * s
			);

			yield return null;
		}

		elapsed =0f;
		while (elapsed < half)
		{
			if (sr == null) break;
			elapsed += Time.deltaTime;
			float t = Clamp01(elapsed / half);
			float s = SmoothStep01(t);

			sr.transform.localScale = new Vector3(
				targetScale.x + (originalScale.x - targetScale.x) * s,
				targetScale.y + (originalScale.y - targetScale.y) * s,
				targetScale.z + (originalScale.z - targetScale.z) * s
			);
			yield return null;
		}

		yield return new WaitForSeconds(flashDuration);

		if (sr != null) sr.color = original;
		if (sr != null) sr.transform.localScale = originalScale;

		spritesEnEfecto.Remove(sr);
	}

	// --- Detección de colisiones ---
	void ObtenerColisiones(List<SpriteRenderer> lista, List<(SpriteRenderer, SpriteRenderer)> resultado)
	{
		resultado.Clear();
		if (lista == null) return;

		int n = lista.Count;
		for (int i =0; i < n; i++)
		{
			var a = lista[i];
			if (a == null) continue;
			Vector2 posA = a.transform.position;
			Vector2 halfA = a.bounds.size *0.5f;

			for (int j = i +1; j < n; j++)
			{
				var b = lista[j];
				if (b == null) continue;

				Vector2 posB = b.transform.position;
				Vector2 halfB = b.bounds.size *0.5f;

				if (AABBOverlap(posA, halfA, posB, halfB))
				{
					resultado.Add((a, b));
				}
			}
		}
	}

	void ObtenerColisionesEntreListas(List<SpriteRenderer> listaA, List<SpriteRenderer> listaB, List<(SpriteRenderer, SpriteRenderer)> resultado)
	{
		resultado.Clear();
		if (listaA == null || listaB == null) return;

		int nA = listaA.Count;
		int nB = listaB.Count;
		for (int i =0; i < nA; i++)
		{
			var a = listaA[i];
			if (a == null) continue;
			Vector2 posA = a.transform.position;
			Vector2 halfA = a.bounds.size *0.5f;

			for (int j =0; j < nB; j++)
			{
				var b = listaB[j];
				if (b == null) continue;

				Vector2 posB = b.transform.position;
				Vector2 halfB = b.bounds.size *0.5f;

				if (AABBOverlap(posA, halfA, posB, halfB))
				{
					resultado.Add((a, b));
				}
			}
		}
	}

	static bool AABBOverlap(Vector2 posA, Vector2 halfA, Vector2 posB, Vector2 halfB)
	{
		float dx = posA.x - posB.x;
		float dy = posA.y - posB.y;
		float overlapX = halfA.x + halfB.x;
		float overlapY = halfA.y + halfB.y;
		if (dx <0f) dx = -dx;
		if (dy <0f) dy = -dy;
		return (dx <= overlapX) && (dy <= overlapY);
	}

	// --- Manejo de impulsos ---
	void AplicarImpulso(SpriteRenderer a, SpriteRenderer b)
	{
		if (a == null || b == null) return;

		ProjectileController pc = GetCachedProjectile(a);
		SpriteRenderer other = b;
		if (pc == null)
		{
			pc = GetCachedProjectile(b);
			other = a;
		}

		if (pc == null) return;
		if (pc.reachedGoal) return;

		Vector2 posP = pc.transform.position;
		Vector2 posO = other.transform.position;

		SpriteRenderer srP = pc.GetComponent<SpriteRenderer>();
		Vector2 halfP = srP != null ? srP.bounds.size *0.5f : Vector2.zero;
		Vector2 halfO = other.bounds.size *0.5f;

		float dx = posP.x - posO.x;
		float dy = posP.y - posO.y;
		float absDx = dx <0f ? -dx : dx;
		float absDy = dy <0f ? -dy : dy;

		float overlapX = (halfP.x + halfO.x) - absDx;
		float overlapY = (halfP.y + halfO.y) - absDy;

		if (overlapX <=0f && overlapY <=0f) return;

		float vx = pc.velocity.x;
		float vy = pc.velocity.y;

		if (overlapX < overlapY)
		{
			float signX = dx <0f ? -1f :1f;
			vx = -vx * bounceDamping;
			float push = overlapX + separationOffset;
			pc.transform.position = (Vector2)pc.transform.position + new Vector2(signX * push,0f);
			vx += signX * impulseStrength;
		}
		else
		{
			float signY = dy <0f ? -1f :1f;
			vy = -vy * bounceDamping;
			float push = overlapY + separationOffset;
			pc.transform.position = (Vector2)pc.transform.position + new Vector2(0f, signY * push);
			vy += signY * impulseStrength;
		}

		float magSq = vx * vx + vy * vy;
		float maxSq = maxSpeed * maxSpeed;
		if (magSq > maxSq)
		{
			float inv = (float)(1.0 / System.Math.Sqrt(magSq));
			float scale = maxSpeed * inv;
			vx *= scale;
			vy *= scale;
		}

		pc.velocity = new Vector2(vx, vy);
		pc.launched = true;
	}

	// --- Caché ---
	ProjectileController GetCachedProjectile(SpriteRenderer sr)
	{
		if (sr == null) return null;
		if (pcCache.TryGetValue(sr, out var pc)) return pc;
		pc = sr.GetComponent<ProjectileController>();
		pcCache[sr] = pc;
		return pc;
	}

	// --- Utilidades ---
	static float Clamp01(float v)
	{
		if (v <=0f) return 0f;
		if (v >=1f) return 1f;
		return v;
	}

	static float SmoothStep01(float t)
	{
		return (t * t) * (3f -2f * t);
	}
}