using UnityEngine;

public class ShotgunAimSectorVisual : MonoBehaviour
{
    public enum SectorState
    {
        Ready,
        Reloading
    }

    [Header("Refs")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    [Header("Mesh")]
    [Range(4, 80)] public int segments = 24;
    public float groundOffset = 0.05f;

    [Header("Materials")]
    public Material readyMaterial;
    public Material reloadingMaterial;

    private Mesh runtimeMesh;
    private SectorState currentState = SectorState.Ready;
    private float lastRadius = -1f;
    private float lastAngle = -1f;
    private int lastSegments = -1;
    private SectorState appliedState = (SectorState)(-1);

    void Awake()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        EnsureMesh();
        Hide();
    }

    void EnsureMesh()
    {
        if (runtimeMesh != null) return;
        if (meshFilter == null) return;

        runtimeMesh = new Mesh();
        runtimeMesh.name = "ShotgunSectorMesh";
        meshFilter.mesh = runtimeMesh;
    }

    public void SetState(SectorState newState)
    {
        currentState = newState;
        ApplyStateMaterialIfNeeded();
    }

    void ApplyStateMaterialIfNeeded()
    {
        if (meshRenderer == null) return;
        if (appliedState == currentState) return;

        Material targetMat = currentState == SectorState.Ready ? readyMaterial : reloadingMaterial;
        if (targetMat != null)
            meshRenderer.sharedMaterial = targetMat;

        appliedState = currentState;
    }

    public void Show(Vector3 origin, Vector3 forward, float radius, float angle)
    {
        EnsureMesh();
        ApplyStateMaterialIfNeeded();

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        transform.position = origin + Vector3.up * groundOffset;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        int seg = Mathf.Max(4, segments);
        if (!Mathf.Approximately(radius, lastRadius) || !Mathf.Approximately(angle, lastAngle) || seg != lastSegments)
        {
            BuildSectorMesh(runtimeMesh, Vector3.zero, radius, angle, seg);
            lastRadius = radius;
            lastAngle = angle;
            lastSegments = seg;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void BuildSectorMesh(Mesh mesh, Vector3 localOrigin, float radius, float angle, int seg)
    {
        mesh.Clear();

        Vector3[] vertices = new Vector3[seg + 2];
        int[] triangles = new int[seg * 3];
        Vector2[] uvs = new Vector2[vertices.Length];

        vertices[0] = localOrigin;
        uvs[0] = new Vector2(0.5f, 0f);

        float startAngle = -angle * 0.5f;
        float step = angle / seg;

        for (int i = 0; i <= seg; i++)
        {
            float a = startAngle + step * i;
            Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * Vector3.forward;
            Vector3 p = localOrigin + dir * radius;

            vertices[i + 1] = p;
            uvs[i + 1] = new Vector2((float)i / seg, 1f);
        }

        for (int i = 0; i < seg; i++)
        {
            int t = i * 3;
            triangles[t] = 0;
            triangles[t + 1] = i + 1;
            triangles[t + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}