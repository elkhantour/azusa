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
    private const float RADIUS_NO_OVERRIDE = 0.0f;
    private const float RADIUS_BIAS = 10.0f; // To actually put the home on the circle

    public void Init()
    {
        GenerateTown();
    }

    // TODO: rn we use a radiusOverride cause RingConfig already have a radius attribute
    // So depending on the flow maybe we can simply remove the radius attribute are only use
    // this one in the method argument
    public void GenerateTown(GameObject parent = null, float radiusOverride = RADIUS_NO_OVERRIDE)
    {
        //ClearTown();
        SpawnCenter(parent);

        for (int i = 0; i < Rings.Count; i++)
        {
            RingConfig ring = Rings[i];
            float previousRadius = ring.Radius;

            if (radiusOverride != RADIUS_NO_OVERRIDE)
            {
                ring.Radius = radiusOverride;
            }

            SpawnRing(ring, parent);

            ring.Radius = previousRadius;
        }

    }

    private void SpawnRing(RingConfig config, GameObject parent = null)
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
                Vector3 candidatePos = parent.transform.position + new Vector3(x, 0, z);

                if (IsSpaceFree(candidatePos, spawnedPositions, config.MinSeparation))
                {
                    finalPos = candidatePos;
                    validPosition = true;
                }
                attempts++;
            }

            if (validPosition)
            {
                GameObject prefab = config.Prefabs[Random.Range(0, config.Prefabs.Length)];

                Quaternion rotation = Quaternion.identity;
                // TODO Rotation: Always face the center (transform.position)
                // Vector3 directionToCenter = (transform.position - finalPos).normalized;
                // Quaternion rotation = Quaternion.LookRotation(directionToCenter);

                Instantiate(prefab, finalPos, rotation, parent != null ? parent.transform : transform);
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

    private void SpawnCenter(GameObject parent = null)
    {
        if (CenterPrefabs.Length > 0)
        {
            Instantiate(CenterPrefabs[Random.Range(0, CenterPrefabs.Length)], transform.position, Quaternion.identity, parent != null ? parent.transform : transform);
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
