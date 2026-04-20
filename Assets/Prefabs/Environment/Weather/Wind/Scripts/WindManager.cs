using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindManager : MonoBehaviour
{

    public Mesh Ribbon;
    public Material Material;

    public int Count = 50;
    public int Speed = 1;
    public float Amplitude = 0.1f;

    public Vector3 Direction = Vector3.forward;
    public Vector2 SpawnArea = new Vector2(50, 50);

    private Matrix4x4[] _matrices;

    void Start()
    {
        _matrices = new Matrix4x4[Count];

        for (int i = 0; i < Count; i++)
        {
            // Random position within your island bounds
            Vector3 pos = new Vector3(
                Random.Range(-SpawnArea.x, SpawnArea.x),
                Random.Range(2f, 10f), // Keep them in the air
                Random.Range(-SpawnArea.y, SpawnArea.y)
            );

            // Calculate rotation to face the wind direction
            Quaternion rot = Quaternion.LookRotation(Direction);

            // Randomize scale slightly for variety
            Vector3 scale = new Vector3(Random.Range(0.8f, 1.5f), 1, 1);

            _matrices[i] = Matrix4x4.TRS(pos, rot, scale);
        }
    }

    void Update()
    {
        // Draw all trails in one single call
        Graphics.DrawMeshInstanced(Ribbon, 0, Material, _matrices);
    }
}
