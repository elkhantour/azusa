using UnityEngine;
using System.Collections.Generic;

public class NomadTownGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct RingConfig
    {
        public string Name;
        public GameObject[] Prefabs;
        public int ItemCount;
        public float Radius;
        [Tooltip("Random offset applied to the position")]
        public float Distortion;
        [Tooltip("Minimum distance between objects in this ring")]
        public float MinSeparation;
        public bool FaceCenter;
    }

    public GameObject[] CenterPrefabs;
    public List<RingConfig> Rings = new List<RingConfig>();

    public void Init()
    {
        GenerateTown();
    }

    public void GenerateTown()
    {
        //ClearTown();
        SpawnCenter();

        foreach (var ring in Rings)
        {
            SpawnRing(ring);
        }
    }

    private void SpawnRing(RingConfig config)
    {
        List<Vector3> spawnedPositions = new List<Vector3>();

        for (int i = 0; i < config.ItemCount; i++)
        {
            Vector3 finalPos = Vector3.zero;
            bool validPosition = false;
            int attempts = 0;

            // Try to find a valid spot that respects MinSeparation
            while (!validPosition && attempts < 10)
            {
                float angle = (i * Mathf.PI * 2 / config.ItemCount);

                // Add distortion/jitter to the angle and radius
                float distortedRadius = config.Radius + Random.Range(-config.Distortion, config.Distortion);
                float distortedAngle = angle + Random.Range(-config.Distortion * 0.1f, config.Distortion * 0.1f);

                float x = Mathf.Cos(distortedAngle) * distortedRadius;
                float z = Mathf.Sin(distortedAngle) * distortedRadius;
                Vector3 candidatePos = transform.position + new Vector3(x, 0, z);

                if (IsSpaceFree(candidatePos, spawnedPositions, config.MinSeparation))
                {
                    finalPos = candidatePos;
                    validPosition = true;
                }
                attempts++;
            }

            Debug.Log(validPosition);

            if (validPosition)
            {
                GameObject prefab = config.Prefabs[Random.Range(0, config.Prefabs.Length)];

                // Rotation: Always face the center (transform.position)
                Vector3 directionToCenter = (transform.position - finalPos).normalized;
                Quaternion rotation = Quaternion.LookRotation(directionToCenter);

                Instantiate(prefab, finalPos, rotation, transform);
                spawnedPositions.Add(finalPos);
            }
        }
    }

    private bool IsSpaceFree(Vector3 candidate, List<Vector3> others, float minDest)
    {
        foreach (var pos in others)
        {
            if (Vector3.Distance(candidate, pos) < minDest)
                return false;
        }
        return true;
    }

    private void SpawnCenter()
    {
        if (CenterPrefabs.Length > 0)
        {
            Instantiate(CenterPrefabs[Random.Range(0, CenterPrefabs.Length)], transform.position, Quaternion.identity, transform);
        }
    }

    private void ClearTown()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    public Vector3 GetPosition()
    {
        return gameObject.transform.position;
    }

    public float GetOuterRadius()
    {
        float radius = 0.0f;

        foreach (var ring in Rings)
        {
            radius = Mathf.Max(radius, ring.Radius);
        }

        return radius;
    }
}
