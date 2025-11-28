using UnityEngine;

public class AsteroidRain : MonoBehaviour
{
    public ComputeShader compute;
    public Material mat;
    public int count = 1000;

    ComputeBuffer buffer;
    int kernel;

    struct Asteroid {
        public Vector2 pos;
        public float speed;
    }

    Camera cam;

    void Start()
    {
        cam = Camera.main;

        kernel = compute.FindKernel("CSMain");

        buffer = new ComputeBuffer(count, sizeof(float) * 3);

        // Obtener tamaño visible de la cámara
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        Asteroid[] data = new Asteroid[count];
        for (int i = 0; i < count; i++)
        {
            data[i].pos = new Vector2(
                Random.Range(-camWidth * 0.5f, camWidth * 0.5f),
                Random.Range(-camHeight * 0.5f, camHeight * 0.5f)
            );

            data[i].speed = Random.Range(1f, 4f);
        }

        buffer.SetData(data);

        compute.SetBuffer(kernel, "asteroids", buffer);
        mat.SetBuffer("asteroids", buffer);
    }

    void Update()
    {
        // recalcular bounds por si la cámara cambia de tamaño
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        compute.SetFloat("deltaTime", Time.deltaTime);
        compute.SetVector("bounds", new Vector2(camWidth * 0.5f, camHeight * 0.5f));

        compute.Dispatch(kernel, Mathf.CeilToInt(count / 256f), 1, 1);

        Graphics.DrawMeshInstancedProcedural(
            MeshGenerator.Quad,
            0,
            mat,
            new Bounds(Vector3.zero, Vector3.one * 500),
            count
        );
    }

    private void OnDestroy() {
        buffer?.Dispose();
    }
}
