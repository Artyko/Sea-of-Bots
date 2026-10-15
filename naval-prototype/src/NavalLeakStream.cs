using UnityEngine;

// Visualises the measured inward flow; never changes the hydraulic volume.
public class NavalLeakStream : MonoBehaviour
{
    private LineRenderer line;
    private readonly Vector3[] points = new Vector3[8];
    private readonly Transform[] drops = new Transform[3];
    public void Initialize(Material material)
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false; line.positionCount = points.Length;
        line.sharedMaterial = material;
        line.startColor = new Color(0.15f, 0.7f, 0.95f); line.endColor = new Color(0.6f, 0.9f, 1f);
        line.numCapVertices = 2;
        for (int i = 0; i < drops.Length; i++)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider collider = drop.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            drop.GetComponent<Renderer>().sharedMaterial = material;
            drop.transform.SetParent(transform, false); drops[i] = drop.transform;
        }
    }
    public void Animate(Vector3 origin, Vector3 inward, float flow, float time)
    {
        float strength = Mathf.Clamp01(flow / 0.1f);
        float velocity = 0.6f + strength * 1.4f;
        Vector3 gravity = transform.parent.InverseTransformDirection(Vector3.down) * 3f;
        origin += inward * 0.14f;
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1) * 0.4f;
            points[i] = origin + inward * velocity * t + gravity * t * t;
            points[i] += Vector3.up * Mathf.Sin(time * 18f + i * 1.7f) * 0.015f * t;
        }
        line.startWidth = 0.04f + 0.10f * strength; line.endWidth = 0.015f;
        line.SetPositions(points);
        for (int i = 0; i < drops.Length; i++)
        {
            float t = Mathf.Repeat(time * 0.8f + i / 3f, 1f) * 0.5f;
            drops[i].localPosition = origin + inward * velocity * t + gravity * t * t;
            drops[i].localScale = Vector3.one * (0.04f + strength * 0.04f);
        }
    }
}
