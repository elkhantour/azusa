using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{

    public static class JitteredGrid
    {

        public static Dictionary<Vector2Int, Vector3> Spawn(GameObject targetObject, float margin = 0.0f, float cellSize = 1.0f, float jitter = 0.4f, float stagger = 0.5f, string raycastLayerName = null)
        {

            Mesh shrunkMesh = MeshUtils.Clone(targetObject.GetComponent<MeshFilter>().mesh);

            if (margin > 0)
                MeshUtils.Offset(shrunkMesh, -1 * margin);

            var gridPoints = new Dictionary<Vector2Int, Vector3>();

            // Get references
            MeshFilter mf = targetObject.GetComponent<MeshFilter>();
            MeshCollider mc = targetObject.GetComponent<MeshCollider>();
            bool cleanCollider = false;

            if (mf == null)
            {
                Debug.LogError("targetObject needs both a MeshFilter");
                return gridPoints;
            }

            if (mc == null)
            {
                mc = targetObject.AddComponent<MeshCollider>();
                cleanCollider = true;
            }


            // Store OLD references (use sharedMesh to avoid auto-instantiating copies)
            Mesh originalMesh = mf.sharedMesh;
            Mesh originalColliderMesh = mc.sharedMesh;

            // Swap in the shrunk mesh for the Raycast
            mf.sharedMesh = shrunkMesh;
            mc.sharedMesh = shrunkMesh;
            // Force Physics to update immediately so the Raycast "sees" the shrunken shape
            Physics.SyncTransforms();

            // Calculate bounds based on the shrunken mesh
            Bounds bounds = shrunkMesh.bounds;
            // If object is scaled/moved, transform these to World Space
            Vector3 min = targetObject.transform.TransformPoint(bounds.min);
            Vector3 max = targetObject.transform.TransformPoint(bounds.max);

            float rayStartHeight = max.y + 5.0f;

            // Calculate how many steps we need for X and Z
            int colCount = Mathf.CeilToInt((max.x - min.x) / cellSize);
            int rowCount = Mathf.CeilToInt((max.z - min.z) / cellSize);

            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    // 1. Calculate base position
                    float xBase = min.x + (c * cellSize);
                    float zBase = min.z + (r * cellSize);

                    // 2. Apply Jitter
                    float range = cellSize * jitter;
                    Vector3 rayOrigin = new Vector3(
                        xBase + UnityEngine.Random.Range(-range, range),
                        rayStartHeight,
                        zBase + UnityEngine.Random.Range(-range, range)
                    );


                    // 3. Layer Based Raycast
                    if (raycastLayerName != null)
                    {
                        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100.0f, LayerMask.GetMask(raycastLayerName)))
                        {
                            gridPoints.Add(new Vector2Int(c, r), hit.point);
                        }
                    }
                    // GameObject cmp Raycast
                    else if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit) && hit.collider.gameObject == targetObject)
                    {
                        // Use Vector2Int as the "Address" of this point
                        gridPoints.Add(new Vector2Int(c, r), hit.point);
                    }


                }
            }


            // 4. Restore the original state so the island looks normal again
            mf.sharedMesh = originalMesh;
            mc.sharedMesh = originalColliderMesh;
            Physics.SyncTransforms();

            if (cleanCollider)
            {
                UnityEngine.Object.Destroy(mc);
            }

            return gridPoints;
        }




    }

}
