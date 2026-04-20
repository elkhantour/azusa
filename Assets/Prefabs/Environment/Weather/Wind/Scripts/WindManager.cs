using UnityEngine;

public class WindManager : MonoBehaviour
{
    [Header("Assets")]
    public Mesh Ribbon;
    public Material Material;

    [Header("Quantity & Bounds")]
    [Range(1, 1023)] public int Count = 50; // 1023 is the hardware limit for one DrawMeshInstanced call
    public Vector2 SpawnArea = new Vector2(50, 50);
    public float MinHeight = 2f;
    public float MaxHeight = 10f;
    public float MinScale = 1.0f;
    public float MaxScale = 2.0f;

    [Header("Movement")]
    public Vector3 Direction = Vector3.forward;
    public float MovementSpeed = 5f;

    private Matrix4x4[] _matrices;
    private Vector3[] _positions;
    private Vector3[] _scales;
    private float[] _timeOffsets;
    private MaterialPropertyBlock _propBlock;

    void Start()
    {
        _matrices = new Matrix4x4[Count];
        _positions = new Vector3[Count];
        _scales = new Vector3[Count];
        _timeOffsets = new float[Count];
        _propBlock = new MaterialPropertyBlock();

        // Normalize direction once
        Direction.Normalize();

        for (int i = 0; i < Count; i++)
        {
            // Initial random placement
            _positions[i] = new Vector3(
                Random.Range(-SpawnArea.x, SpawnArea.x),
                Random.Range(MinHeight, MaxHeight),
                Random.Range(-SpawnArea.y, SpawnArea.y)
            );

            float scale = UnityEngine.Random.Range(MinScale, MaxScale);
            _scales[i] = new Vector3(scale, scale, scale);

            // Give each trail a unique time offset so they don't sync up
            _timeOffsets[i] = Random.Range(0f, 100f);
        }
    }

    void Update()
    {
        Quaternion lookRot = Quaternion.LookRotation(Direction);

        Quaternion adjustRot = Quaternion.Euler(0, 90, 0);
        Quaternion finalRot = lookRot * adjustRot;

        for (int i = 0; i < Count; i++)
        {
            // 1. Move the position
            _positions[i] += Direction * MovementSpeed * Time.deltaTime;

            // 2. Wrap around logic (Keep them within bounds)
            if (Vector3.Distance(new Vector3(0, _positions[i].y, 0), new Vector3(0, _positions[i].y, 0) + _positions[i]) > SpawnArea.magnitude)
            {
                // Simple box-wrapping: if far enough away, flip to other side
                if (Mathf.Abs(_positions[i].x) > SpawnArea.x) _positions[i].x *= -0.95f;
                if (Mathf.Abs(_positions[i].z) > SpawnArea.y) _positions[i].z *= -0.95f;
            }

            // 3. Update the Matrix
            _matrices[i] = Matrix4x4.TRS(_positions[i], finalRot, _scales[i]);
        }

        // 4. Send unique data to the shader (optional: if you add _TimeOffset to shader)
        // _propBlock.SetFloatArray("_TimeOffset", _timeOffsets);

        // 5. High Performance Draw Call
        Graphics.DrawMeshInstanced(Ribbon, 0, Material, _matrices, Count, _propBlock);
    }

    // Visualization of the spawn area in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * ((MaxHeight + MinHeight) / 2),
            new Vector3(SpawnArea.x * 2, MaxHeight - MinHeight, SpawnArea.y * 2));
    }
}
