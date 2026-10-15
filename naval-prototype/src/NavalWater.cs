using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// CPU height field: rendered waves and buoyancy use exactly the same height function.
public class NavalWater : MonoBehaviour
{
    public float Amplitude = 0.20f;
    public Vector3 Focus;
    public Transform AdditionalHull;
    public Material InteriorMaterial { get; private set; }
    public Material SurfaceMaterial { get { return surfaceMaterial; } }
    private Material surfaceMaterial;
    private Mesh mesh;
    private Vector3[] vertices;
    private readonly List<int> indices = new List<int>(60000);
    private readonly List<Vector3> clippedVertices = new List<Vector3>(14000);
    private readonly List<Vector3> polygon = new List<Vector3>(10);
    private readonly List<Vector3> inside = new List<Vector3>(10);
    private readonly List<Vector3> outside = new List<Vector3>(10);
    private const int Count = 101;
    private const float Spacing = 3f;
    private const float Half = 50f;
    private float nextMeshAt;
    private readonly List<int> firstPass = new List<int>(65000);
    private Matrix4x4 waterToClip,clipToWater;
    private Vector3 clipCenter;
    private readonly System.Diagnostics.Stopwatch buildWatch=new System.Diagnostics.Stopwatch();
    public float BuildMilliseconds { get; private set; }
    private float simulationTime;

    public void Initialize()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) throw new System.InvalidOperationException("Water shader unavailable.");
        surfaceMaterial = WaterMaterial(shader, new Color(0.045f, 0.30f, 0.39f, 0.88f));
        // The sea surface writes depth: opaque shore objects then meet the
        // water correctly, instead of being sorted above a transparent sheet.
        Color seaColor=surfaceMaterial.color;seaColor.a=1f;surfaceMaterial.color=seaColor;
        surfaceMaterial.SetFloat("_Mode",0f);surfaceMaterial.SetInt("_SrcBlend",1);surfaceMaterial.SetInt("_DstBlend",0);surfaceMaterial.SetInt("_ZWrite",1);
        surfaceMaterial.DisableKeyword("_ALPHABLEND_ON");surfaceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");surfaceMaterial.DisableKeyword("_ALPHATEST_ON");
        surfaceMaterial.SetOverrideTag("RenderType","Opaque");surfaceMaterial.renderQueue=2000;
        InteriorMaterial = WaterMaterial(shader, new Color(0.06f, 0.42f, 0.48f, 0.65f));
        vertices = new Vector3[Count * Count];
        Vector2[] uv = new Vector2[vertices.Length];
        for (int z = 0; z < Count; z++) for (int x = 0; x < Count; x++)
        {
            int i = z * Count + x;
            vertices[i] = new Vector3((x - Half) * Spacing, 0f, (z - Half) * Spacing);
            uv[i] = new Vector2(x / 10f, z / 10f);
        }
        mesh = new Mesh(); mesh.name = "NavalPrototype_DynamicWaves";
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.MarkDynamic(); mesh.vertices = vertices; mesh.uv = uv;
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = surfaceMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private Material WaterMaterial(Shader shader, Color color)
    {
        Material material = new Material(shader); material.color = color;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.92f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.12f);
        if (shader.name == "Standard")
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.DisableKeyword("_ALPHATEST_ON"); material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
        return material;
    }

    public float Height(float x, float z)
    {
        x -= transform.position.x; z -= transform.position.z;
        return transform.position.y + Amplitude * (
            0.62f * Mathf.Sin(x * 0.47f + z * 0.21f - simulationTime * 1.35f) +
            0.28f * Mathf.Sin(z * 0.83f - x * 0.16f - simulationTime * 1.8f) +
            0.10f * Mathf.Sin(x * 1.9f + z * 1.3f - simulationTime * 2.6f));
    }

    public void RenderWaves(Transform boat, float speed)
    {
        simulationTime += Mathf.Min(Time.deltaTime, 0.05f);
        if(Time.time<nextMeshAt)return;nextMeshAt=Time.time+1f/30f;
        buildWatch.Restart();
        float offsetX = Mathf.Floor((Focus.x - transform.position.x) / 40f) * 40f;
        float offsetZ = Mathf.Floor((Focus.z - transform.position.z) / 40f) * 40f;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].x = (i % Count - Half) * Spacing + offsetX;
            vertices[i].z = (i / Count - Half) * Spacing + offsetZ;
            Vector3 world = transform.position + new Vector3(vertices[i].x, 0f, vertices[i].z);
            float h = Height(world.x, world.z);
            Vector3 relative = boat.InverseTransformPoint(new Vector3(world.x, boat.position.y, world.z));
            // A small travelling wake behind the stern.
            if (relative.z < -BoatDimensions.HalfLength && relative.z > -20f)
            {
                float width = BoatDimensions.HalfWidth + (-relative.z - BoatDimensions.HalfLength) * 0.4f;
                float ridge = Mathf.Exp(-Mathf.Pow((Mathf.Abs(relative.x) - width) * 1.6f, 2f));
                h += ridge * Mathf.Abs(speed) * 0.035f * Mathf.Exp((relative.z + BoatDimensions.HalfLength) * 0.13f)
                    * Mathf.Sin(relative.z * 2f + simulationTime * 4f);
            }
            vertices[i].y = h - transform.position.y;
        }
        indices.Clear();
        clippedVertices.Clear(); clippedVertices.AddRange(vertices);
        PrepareClip(boat);
        for (int z = 0; z < Count - 1; z++) for (int x = 0; x < Count - 1; x++)
        {
            int a = z * Count + x, b = a + 1, c = a + Count, d = c + 1;
            AddTriangle(a, c, b, boat); AddTriangle(b, c, d, boat);
        }
        // Water remains visible when viewed upward from below the surface.
        int frontCount = indices.Count;
        if(AdditionalHull!=null)
        {
            firstPass.Clear();firstPass.AddRange(indices);indices.Clear();PrepareClip(AdditionalHull);
            for(int i=0;i<firstPass.Count;i+=3)AddTriangle(firstPass[i],firstPass[i+1],firstPass[i+2],AdditionalHull);
        }
        frontCount=indices.Count;
        int frontVertices = clippedVertices.Count;
        for (int i = 0; i < frontVertices; i++) clippedVertices.Add(clippedVertices[i]);
        for (int i = 0; i < frontCount; i += 3)
        { indices.Add(indices[i + 2] + frontVertices); indices.Add(indices[i + 1] + frontVertices); indices.Add(indices[i] + frontVertices); }
        mesh.Clear(); mesh.SetVertices(clippedVertices); mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        buildWatch.Stop();BuildMilliseconds=(float)buildWatch.Elapsed.TotalMilliseconds;
    }

    private void AddTriangle(int a, int b, int c, Transform boat)
    {
        Vector3 pa=clippedVertices[a],pb=clippedVertices[b],pc=clippedVertices[c];
        if((pa.x<clipCenter.x-20f&&pb.x<clipCenter.x-20f&&pc.x<clipCenter.x-20f)||
           (pa.x>clipCenter.x+20f&&pb.x>clipCenter.x+20f&&pc.x>clipCenter.x+20f)||
           (pa.z<clipCenter.z-20f&&pb.z<clipCenter.z-20f&&pc.z<clipCenter.z-20f)||
           (pa.z>clipCenter.z+20f&&pb.z>clipCenter.z+20f&&pc.z>clipCenter.z+20f))
        {indices.Add(a);indices.Add(b);indices.Add(c);return;}
        Vector3 va = waterToClip.MultiplyPoint3x4(pa);
        Vector3 vb = waterToClip.MultiplyPoint3x4(pb);
        Vector3 vc = waterToClip.MultiplyPoint3x4(pc);
        // Cut at the middle of the solid walls, not outside them. The walls cover the seam.
        // A centroid test leaves whole triangular holes and lets overlapping corners enter the hold.
        for (int plane = 0; plane < 5; plane++)
            if (Distance(va, plane) >= 0f && Distance(vb, plane) >= 0f && Distance(vc, plane) >= 0f)
            { indices.Add(a); indices.Add(b); indices.Add(c); return; }
        polygon.Clear(); polygon.Add(va); polygon.Add(vb); polygon.Add(vc);
        // Subtract the convex hull mask. Emit only disjoint outside pieces at each plane.
        for (int plane = 0; plane < 5 && polygon.Count >= 3; plane++)
        {
            Split(polygon, plane);
            Emit(outside, boat);
            polygon.Clear(); polygon.AddRange(inside);
        }
        // Remaining polygon lies inside the boat below the rim and is intentionally discarded.
    }
    private void PrepareClip(Transform boat)
    {waterToClip=boat.worldToLocalMatrix*transform.localToWorldMatrix;clipToWater=transform.worldToLocalMatrix*boat.localToWorldMatrix;clipCenter=transform.InverseTransformPoint(boat.position);}

    private static float Distance(Vector3 p, int plane)
    {
        switch (plane)
        {
            case 0: return p.x - BoatDimensions.HalfWidth;
            case 1: return -p.x - BoatDimensions.HalfWidth;
            case 2: return p.z - BoatDimensions.HalfLength;
            case 3: return -p.z - BoatDimensions.HalfLength;
            default: return p.y - BoatDimensions.Rim;
        }
    }
    private void Split(List<Vector3> source, int plane)
    {
        inside.Clear(); outside.Clear();
        Vector3 previous = source[source.Count - 1];
        float previousDistance = Distance(previous, plane);
        foreach (Vector3 current in source)
        {
            float currentDistance = Distance(current, plane);
            if ((previousDistance < 0f) != (currentDistance < 0f))
            {
                float t = previousDistance / (previousDistance - currentDistance);
                Vector3 intersection = Vector3.Lerp(previous, current, t);
                inside.Add(intersection); outside.Add(intersection);
            }
            if (currentDistance < 0f) inside.Add(current); else outside.Add(current);
            previous = current; previousDistance = currentDistance;
        }
    }
    private void Emit(List<Vector3> piece, Transform boat)
    {
        if (piece.Count < 3) return;
        int first = clippedVertices.Count;
        foreach (Vector3 point in piece)
            clippedVertices.Add(clipToWater.MultiplyPoint3x4(point));
        for (int i = 1; i < piece.Count - 1; i++)
        { indices.Add(first); indices.Add(first + i); indices.Add(first + i + 1); }
    }
    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (surfaceMaterial != null) Destroy(surfaceMaterial);
        if (InteriorMaterial != null) Destroy(InteriorMaterial);
    }
}
