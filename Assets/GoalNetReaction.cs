using UnityEngine;

// Deform a private mesh instance, keeping the shared asset and other goal intact.
public class GoalNetReaction : MonoBehaviour
{
    public GoalTrigger ScoringTrigger { get; set; }
    private Mesh mesh;
    private Vector3[] rest;
    private Vector3[] vertices;
    private Transform netTransform;
    private Vector3 impact;
    private Vector3 impulse;
    private float elapsed = 2f;

    private void Awake()
    {
        Transform net = transform.Find("Goal Net");
        if (net == null) { enabled = false; return; }
        netTransform = net;
        MeshFilter filter = net.GetComponent<MeshFilter>();
        mesh = Instantiate(filter.sharedMesh);
        filter.sharedMesh = mesh;
        mesh.MarkDynamic();
        rest = mesh.vertices;
        vertices = new Vector3[rest.Length];
    }

    public void React(Vector3 point, Vector3 velocity)
    {
        if (mesh == null || velocity.sqrMagnitude < 0.25f) return;
        impact = netTransform.InverseTransformPoint(point);
        impulse = netTransform.InverseTransformDirection(velocity.normalized) * Mathf.Clamp(velocity.magnitude * 0.018f, 0.08f, 0.42f);
        elapsed = 0f;
    }

    private void Update()
    {
        if (mesh == null || elapsed >= 1.4f) return;
        elapsed += Time.deltaTime;
        Animate(elapsed);
    }

    private void Animate(float time)
    {
        float wave = time >= 1.4f ? 0f : Mathf.Sin(time * 15f) * Mathf.Exp(-time * 3.8f);
        for (int i = 0; i < rest.Length; i++)
        {
            Vector3 v = rest[i];
            float backZ = Mathf.Lerp(1.35f, 0.75f, Mathf.Clamp01((v.y - 0.04f) / 2.16f));
            float pin;
            if (Mathf.Abs(v.z - backZ) < 0.035f)
                pin = Mathf.Clamp01((3f - Mathf.Abs(v.x)) / 0.3f) * Mathf.Clamp01(v.y / 0.25f) * Mathf.Clamp01((2.2f - v.y) / 0.25f);
            else if (Mathf.Abs(v.y - 2.2f) < 0.035f)
                pin = Mathf.Clamp01((3f - Mathf.Abs(v.x)) / 0.3f) * Mathf.Clamp01(v.z / 0.2f) * Mathf.Clamp01((0.75f - v.z) / 0.2f);
            else
                pin = Mathf.Clamp01(v.y / 0.25f) * Mathf.Clamp01((2.2f - v.y) / 0.25f) * Mathf.Clamp01(v.z / 0.2f) * Mathf.Clamp01((backZ - v.z) / 0.2f);
            float influence = Mathf.Exp(-(v - impact).sqrMagnitude / 1.2f);
            vertices[i] = v + impulse * (wave * influence * pin);
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }
}
