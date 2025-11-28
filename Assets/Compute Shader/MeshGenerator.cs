using UnityEngine;
public static class MeshGenerator
{
    static Mesh _quad;
    public static Mesh Quad
    {
        get
        {
            if (_quad != null) return _quad;

            _quad = new Mesh();
            _quad.vertices = new Vector3[] {
                new Vector3(-0.5f,-0.5f,0),
                new Vector3(0.5f,-0.5f,0),
                new Vector3(0.5f,0.5f,0),
                new Vector3(-0.5f,0.5f,0)
            };
            _quad.uv = new Vector2[] {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1)
            };
            _quad.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

            return _quad;
        }
    }
}
