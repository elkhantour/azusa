using UnityEngine;
using System.Collections.Generic;

public class NomadHouseGenerator : MonoBehaviour
{
    [System.Serializable]
    public class ElementData
    {
        public string name;
        public GameObject prefab;
        [Range(0, 1)] public float probability;
    }

    [Header("Shared Assets")]
    public Material sharedMaterial; // All elements will use this

    [Header("Core Components")]
    public GameObject houseBase;

    [Header("UV Settings")]
    public string uvOffsetPropertyName = "_MainTex";
    public float uvStep = 0.25f;
    public int totalUVMaps = 4;

    [Header("Element Lists")]
    public List<ElementData> doorElements;
    public List<ElementData> wallElements;
    public List<ElementData> groundElements;
    public List<ElementData> roofElements;

    [Header("Generation Rules")]
    public int maxWalls = 5;

    private bool[] occupiedPanes = new bool[16];

    void Start()
    {
        Generate();
    }

    [ContextMenu("Generate House")]
    public void Generate()
    {
        ClearExistingElements();


        SpawnBase();
        ApplyUVShift();

        // 1. Place Door
        int doorSlot = Random.Range(0, 16);
        SpawnElement(GetRandomElement(doorElements), doorSlot);
        MarkOccupied(doorSlot);

        // 2. Place Walls
        int wallAttempts = 0;
        int wallsPlaced = 0;
        while (wallsPlaced < maxWalls && wallAttempts < 50)
        {
            int randomSlot = Random.Range(0, 16);
            if (IsValidWallSlot(randomSlot))
            {
                ElementData wall = GetRandomElement(wallElements);
                if (Random.value <= wall.probability)
                {
                    SpawnElement(wall, randomSlot);
                    MarkOccupied(randomSlot);
                    wallsPlaced++;
                }
            }
            wallAttempts++;
        }

        // 3. Place Ground Elements
        for (int i = 0; i < 16; i++)
        {
            ElementData ground = GetRandomElement(groundElements);
            if (ground != null && Random.value <= ground.probability && i != doorSlot)
            {
                SpawnElement(ground, i);
            }
        }

        // 4. Place Roof
        if (roofElements.Count > 0)
        {
            ElementData roof = GetRandomElement(roofElements);
            if (Random.value <= roof.probability)
            {
                SpawnElement(roof, 0);
            }
        }
    }

    void UnrotateBone(GameObject instance)
    {
        Transform bone = instance.transform.Find("Bone");

        if (bone != null)
        {
            // Force the bone to zero local rotation
            bone.localRotation = Quaternion.identity;
        }
    }

    void SpawnBase()
    {
        if (houseBase == null)
        {
            Debug.LogError("Base Prefab is missing!");
            return;
        }

        // Spawn the base at the center of this house object
        GameObject baseInstance = Instantiate(houseBase, transform.position, transform.rotation, transform);

        // Setup Base Material
        ApplySharedMaterial(baseInstance);
    }

    void SpawnElement(ElementData data, int slot)
    {
        if (data == null || data.prefab == null) return;

        // Spawn at this object's position (the center)
        GameObject instance = Instantiate(data.prefab, transform.position, transform.rotation, transform);

        // TODO fix the FBX bone rotation issue from blender to unity...
        UnrotateBone(instance);

        // Assign the shared material to the new element
        ApplySharedMaterial(instance);

        // Rotate to the correct pane
        float angle = slot * 22.5f;
        instance.transform.RotateAround(transform.position, Vector3.up, angle);
    }

    void ApplySharedMaterial(GameObject obj)
    {
        if (sharedMaterial == null) return;

        // Check if the object itself has a renderer
        if (obj.TryGetComponent<Renderer>(out Renderer r))
        {
            r.sharedMaterial = sharedMaterial;
        }

        // Check all children (in case the prefab is a container)
        Renderer[] childRenderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer cr in childRenderers)
        {
            cr.sharedMaterial = sharedMaterial;
        }
    }

    void ApplyUVShift()
    {
        if (houseBase == null) return;

        MeshRenderer renderer = houseBase.GetComponentInChildren<MeshRenderer>();

        int randomStep = Random.Range(0, totalUVMaps);
        float offset = randomStep * uvStep;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        // Standard shader uses _MainTex_ST (Scale/Offset). 
        // If using URP/Lit, property might be _BaseMap_ST
        block.SetVector(uvOffsetPropertyName + "_ST", new Vector4(1, 1, offset, 0));

        renderer.SetPropertyBlock(block);
    }

    // ... (Keep existing IsValidWallSlot, MarkOccupied, GetRandomElement, and ClearExistingElements from previous)

    bool IsValidWallSlot(int slot)
    {
        if (occupiedPanes[slot]) return false;
        int prev = (slot + 15) % 16;
        int next = (slot + 1) % 16;
        return !occupiedPanes[prev] && !occupiedPanes[next];
    }

    void MarkOccupied(int slot)
    {
        occupiedPanes[slot] = true;
    }

    ElementData GetRandomElement(List<ElementData> list)
    {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    void ClearExistingElements()
    {
        occupiedPanes = new bool[16];
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            // Only destroy if it's not the base mesh renderer itself
            if (transform.GetChild(i).gameObject != houseBase)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
