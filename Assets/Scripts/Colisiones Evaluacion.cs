using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SistemaColisionesGPU : MonoBehaviour
{
    // --- Configuración pública ---
    [Header("Listas de Objetos")]
    public List<SpriteRenderer> objetosColisionables = new List<SpriteRenderer>();
    public List<SpriteRenderer> objetosEstaticos = new List<SpriteRenderer>();

    [Header("Configuración Física")]
    public float fuerzaImpulso = 5f;
    public float offsetSeparacion = 0.02f;
    public float amortiguacionRebote = 0.8f;
    public float velocidadMaxima = 20f;

    [Header("Configuración Visual")]
    public Color colorDestello = Color.red;
    public float duracionDestello = 0.12f;
    public float cantidadEscala = 0.15f;
    public float duracionEscala = 0.12f;

    // --- Compute Shader ---
    public ComputeShader shaderColisiones;
    private const int TAMANIO_GRUPO_HILOS = 64;

    // --- Buffers GPU ---
    private ComputeBuffer _bufferObjetos;
    private ComputeBuffer _bufferObjetosEstaticos;
    private ComputeBuffer _bufferVelocidades;
    private ComputeBuffer _bufferPosiciones;
    private ComputeBuffer _bufferParesColision;
    private ComputeBuffer _bufferParesRebote;
    private ComputeBuffer _bufferContadorColisiones;
    private ComputeBuffer _bufferContadorRebotes;

    // --- Datos para GPU ---
    private DatosSprite[] _datosObjetosGpu;
    private DatosSprite[] _datosObjetosEstaticosGpu;
    private Vector2[] _resultadoVelocidades;
    private Vector3[] _resultadoPosiciones;
    private int[] _resultadoParesColision;
    private int[] _resultadoParesRebote;
    private int[] _resultadoContadorColisiones;
    private int[] _resultadoContadorRebotes;

    // --- Estado interno ---
    private HashSet<(SpriteRenderer, SpriteRenderer)> _colisionesDetectadas = new HashSet<(SpriteRenderer, SpriteRenderer)>();
    private HashSet<(SpriteRenderer, SpriteRenderer)> _rebotesDetectados = new HashSet<(SpriteRenderer, SpriteRenderer)>();
    private HashSet<SpriteRenderer> _spritesConEfecto = new HashSet<SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, ProjectileController> _cacheControladores = new Dictionary<SpriteRenderer, ProjectileController>();

    struct DatosSprite
    {
        public Vector3 posicion;
        public Vector2 tamanioBounds;
        public Vector2 velocidad;
        public int esProyectil;
        public int alcanzoMeta;
    }

    void Start()
    {
        CrearBuffersCompute();
    }

    void Update()
    {
        if (objetosColisionables.Count == 0) return;

        ActualizarDatosGPU();
        EjecutarShaderCompute();
        ProcesarResultadosGPU();
        AplicarEfectosVisuales();
    }

    void CrearBuffersCompute()
    {
        LiberarBuffers();

        int tamanioDatosSprite = sizeof(float) * 7 + sizeof(int) * 2;

        _bufferObjetos = new ComputeBuffer(objetosColisionables.Count, tamanioDatosSprite);
        _bufferObjetosEstaticos = new ComputeBuffer(objetosEstaticos.Count, tamanioDatosSprite);
        _bufferVelocidades = new ComputeBuffer(objetosColisionables.Count, sizeof(float) * 2);
        _bufferPosiciones = new ComputeBuffer(objetosColisionables.Count, sizeof(float) * 3);

        _bufferParesColision = new ComputeBuffer(1024, sizeof(int));
        _bufferParesRebote = new ComputeBuffer(1024, sizeof(int));
        _bufferContadorColisiones = new ComputeBuffer(1, sizeof(int));
        _bufferContadorRebotes = new ComputeBuffer(1, sizeof(int));

        _resultadoVelocidades = new Vector2[objetosColisionables.Count];
        _resultadoPosiciones = new Vector3[objetosColisionables.Count];
        _resultadoParesColision = new int[1024];
        _resultadoParesRebote = new int[1024];
        _resultadoContadorColisiones = new int[1];
        _resultadoContadorRebotes = new int[1];
    }

    void ActualizarDatosGPU()
    {
        // Actualizar datos de objetos colisionables
        _datosObjetosGpu = new DatosSprite[objetosColisionables.Count];
        for (int i = 0; i < objetosColisionables.Count; i++)
        {
            if (objetosColisionables[i] != null)
            {
                ProjectileController controlador = ObtenerControladorProyectil(objetosColisionables[i]);
                _datosObjetosGpu[i] = new DatosSprite
                {
                    posicion = objetosColisionables[i].transform.position,
                    tamanioBounds = objetosColisionables[i].bounds.size,
                    velocidad = controlador != null ? controlador.velocity : Vector2.zero,
                    esProyectil = controlador != null ? 1 : 0,
                    alcanzoMeta = (controlador != null && controlador.reachedGoal) ? 1 : 0
                };
            }
        }

        // Actualizar datos de objetos estáticos
        _datosObjetosEstaticosGpu = new DatosSprite[objetosEstaticos.Count];
        for (int i = 0; i < objetosEstaticos.Count; i++)
        {
            if (objetosEstaticos[i] != null)
            {
                _datosObjetosEstaticosGpu[i] = new DatosSprite
                {
                    posicion = objetosEstaticos[i].transform.position,
                    tamanioBounds = objetosEstaticos[i].bounds.size,
                    velocidad = Vector2.zero,
                    esProyectil = 0,
                    alcanzoMeta = 0
                };
            }
        }

        // Resetear contadores
        _resultadoContadorColisiones[0] = 0;
        _resultadoContadorRebotes[0] = 0;

        // Subir datos a GPU
        _bufferObjetos.SetData(_datosObjetosGpu);
        _bufferObjetosEstaticos.SetData(_datosObjetosEstaticosGpu);
        _bufferContadorColisiones.SetData(_resultadoContadorColisiones);
        _bufferContadorRebotes.SetData(_resultadoContadorRebotes);
    }

    void EjecutarShaderCompute()
    {
        int indiceKernel = shaderColisiones.FindKernel("CSMain");

        // Establecer buffers
        shaderColisiones.SetBuffer(indiceKernel, "Objetos", _bufferObjetos);
        shaderColisiones.SetBuffer(indiceKernel, "ObjetosRebote", _bufferObjetosEstaticos);
        shaderColisiones.SetBuffer(indiceKernel, "OutVelocities", _bufferVelocidades);
        shaderColisiones.SetBuffer(indiceKernel, "OutPositions", _bufferPosiciones);
        shaderColisiones.SetBuffer(indiceKernel, "OutCollisionPairs", _bufferParesColision);
        shaderColisiones.SetBuffer(indiceKernel, "OutRebotePairs", _bufferParesRebote);
        shaderColisiones.SetBuffer(indiceKernel, "CollisionCounter", _bufferContadorColisiones);
        shaderColisiones.SetBuffer(indiceKernel, "ReboteCounter", _bufferContadorRebotes);

        // Establecer parámetros
        shaderColisiones.SetInt("ObjetosCount", objetosColisionables.Count);
        shaderColisiones.SetInt("ObjetosReboteCount", objetosEstaticos.Count);
        shaderColisiones.SetFloat("ImpulseStrength", fuerzaImpulso);
        shaderColisiones.SetFloat("SeparationOffset", offsetSeparacion);
        shaderColisiones.SetFloat("BounceDamping", amortiguacionRebote);
        shaderColisiones.SetFloat("MaxSpeed", velocidadMaxima);

        // Ejecutar
        int gruposHilos = Mathf.CeilToInt(objetosColisionables.Count / (float)TAMANIO_GRUPO_HILOS);
        shaderColisiones.Dispatch(indiceKernel, gruposHilos, 1, 1);
    }

    void ProcesarResultadosGPU()
    {
        // Descargar resultados
        _bufferVelocidades.GetData(_resultadoVelocidades);
        _bufferPosiciones.GetData(_resultadoPosiciones);
        _bufferParesColision.GetData(_resultadoParesColision);
        _bufferParesRebote.GetData(_resultadoParesRebote);
        _bufferContadorColisiones.GetData(_resultadoContadorColisiones);
        _bufferContadorRebotes.GetData(_resultadoContadorRebotes);

        // Aplicar nuevas posiciones y velocidades
        for (int i = 0; i < objetosColisionables.Count; i++)
        {
            if (objetosColisionables[i] != null)
            {
                ProjectileController controlador = ObtenerControladorProyectil(objetosColisionables[i]);
                if (controlador != null && !controlador.reachedGoal)
                {
                    controlador.velocity = _resultadoVelocidades[i];
                    objetosColisionables[i].transform.position = _resultadoPosiciones[i];
                }
            }
        }
    }

    void AplicarEfectosVisuales()
    {
        // Procesar colisiones normales para efectos visuales
        int cantidadColisiones = _resultadoContadorColisiones[0];
        for (int i = 0; i < cantidadColisiones; i += 3)
        {
            int indiceA = _resultadoParesColision[i + 0];
            int indiceB = _resultadoParesColision[i + 1];

            if (indiceA < objetosColisionables.Count && indiceB < objetosColisionables.Count &&
                objetosColisionables[indiceA] != null && objetosColisionables[indiceB] != null)
            {
                var par = (objetosColisionables[indiceA], objetosColisionables[indiceB]);
                if (!_colisionesDetectadas.Contains(par))
                {
                    ActivarEfectoVisual(objetosColisionables[indiceA]);
                    ActivarEfectoVisual(objetosColisionables[indiceB]);
                }
            }
        }

        // Procesar colisiones de rebote para efectos visuales
        int cantidadRebotes = _resultadoContadorRebotes[0];
        for (int i = 0; i < cantidadRebotes; i += 3)
        {
            int indiceA = _resultadoParesRebote[i + 0];
            int indiceB = _resultadoParesRebote[i + 1];

            if (indiceA < objetosColisionables.Count && indiceB < objetosEstaticos.Count &&
                objetosColisionables[indiceA] != null && objetosEstaticos[indiceB] != null)
            {
                var par = (objetosColisionables[indiceA], objetosEstaticos[indiceB]);
                if (!_rebotesDetectados.Contains(par))
                {
                    ActivarEfectoVisual(objetosColisionables[indiceA]);
                    ActivarEfectoVisual(objetosEstaticos[indiceB]);
                }
            }
        }

        // Actualizar colisiones detectadas
        ActualizarColisionesPrevias();
    }

    void ActualizarColisionesPrevias()
    {
        _colisionesDetectadas.Clear();
        _rebotesDetectados.Clear();

        int cantidadColisiones = _resultadoContadorColisiones[0];
        for (int i = 0; i < cantidadColisiones; i += 3)
        {
            int indiceA = _resultadoParesColision[i + 0];
            int indiceB = _resultadoParesColision[i + 1];
            if (indiceA < objetosColisionables.Count && indiceB < objetosColisionables.Count &&
                objetosColisionables[indiceA] != null && objetosColisionables[indiceB] != null)
            {
                _colisionesDetectadas.Add((objetosColisionables[indiceA], objetosColisionables[indiceB]));
            }
        }

        int cantidadRebotes = _resultadoContadorRebotes[0];
        for (int i = 0; i < cantidadRebotes; i += 3)
        {
            int indiceA = _resultadoParesRebote[i + 0];
            int indiceB = _resultadoParesRebote[i + 1];
            if (indiceA < objetosColisionables.Count && indiceB < objetosEstaticos.Count &&
                objetosColisionables[indiceA] != null && objetosEstaticos[indiceB] != null)
            {
                _rebotesDetectados.Add((objetosColisionables[indiceA], objetosEstaticos[indiceB]));
            }
        }
    }

    void ActivarEfectoVisual(SpriteRenderer sprite)
    {
        if (sprite == null) return;
        if (_spritesConEfecto.Contains(sprite)) return;
        StartCoroutine(CorutinaEfectoVisual(sprite));
    }

    IEnumerator CorutinaEfectoVisual(SpriteRenderer sprite)
    {
        if (sprite == null) yield break;

        _spritesConEfecto.Add(sprite);

        Color colorOriginal = sprite.color;
        sprite.color = colorDestello;

        Vector3 escalaOriginal = sprite.transform.localScale;
        Vector3 escalaObjetivo = escalaOriginal * (1f + cantidadEscala);

        float mitad = duracionEscala * 0.5f;
        float tiempoTranscurrido = 0f;

        // Escalar hacia arriba
        while (tiempoTranscurrido < mitad)
        {
            if (sprite == null) break;
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / mitad);
            float s = t * t * (3f - 2f * t);
            sprite.transform.localScale = Vector3.Lerp(escalaOriginal, escalaObjetivo, s);
            yield return null;
        }

        // Escalar hacia abajo
        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < mitad)
        {
            if (sprite == null) break;
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / mitad);
            float s = t * t * (3f - 2f * t);
            sprite.transform.localScale = Vector3.Lerp(escalaObjetivo, escalaOriginal, s);
            yield return null;
        }

        yield return new WaitForSeconds(duracionDestello);

        if (sprite != null) sprite.color = colorOriginal;
        if (sprite != null) sprite.transform.localScale = escalaOriginal;

        _spritesConEfecto.Remove(sprite);
    }

    ProjectileController ObtenerControladorProyectil(SpriteRenderer sprite)
    {
        if (sprite == null) return null;
        if (_cacheControladores.TryGetValue(sprite, out var controlador)) return controlador;
        controlador = sprite.GetComponent<ProjectileController>();
        if (controlador != null) _cacheControladores[sprite] = controlador;
        return controlador;
    }

    void LiberarBuffers()
    {
        _bufferObjetos?.Release();
        _bufferObjetosEstaticos?.Release();
        _bufferVelocidades?.Release();
        _bufferPosiciones?.Release();
        _bufferParesColision?.Release();
        _bufferParesRebote?.Release();
        _bufferContadorColisiones?.Release();
        _bufferContadorRebotes?.Release();
    }

    void OnDestroy()
    {
        LiberarBuffers();
    }

    void OnDisable()
    {
        LiberarBuffers();
    }
}