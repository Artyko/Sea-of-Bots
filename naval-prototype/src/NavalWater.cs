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
    private NavalDistantWater distant;
    private Vector3[] vertices;
    private readonly List<int> indices = new List<int>(60000);
    private readonly List<Vector3> clippedVertices = new List<Vector3>(14000);
    private readonly List<Vector3> polygon = new List<Vector3>(10);
    private readonly List<Vector3> inside = new List<Vector3>(10);
    private readonly List<Vector3> outside = new List<Vector3>(10);
    private const int Count = 101;
    private readonly NavalWaveField waveField=new NavalWaveField(Count);
    private readonly NavalOceanNormals normalField=new NavalOceanNormals(Count);
    private readonly float[] heights=new float[Count*Count];
    private readonly List<Vector3> surfaceNormals=new List<Vector3>(28000);
    private const float Spacing = 3f;
    private const float Half = 50f;
    private float nextMeshAt;
    private bool renderedHeightReady;private float renderedOriginX,renderedOriginZ;
    private int nextMeshFrame;
    private readonly List<int>[] baseRows = new List<int>[Count-1];
    private readonly bool[] clippedRows = new bool[Count-1];
    private readonly List<int>[] surfaceRows = new List<int>[Count-1];
    private List<int> rowScratch = new List<int>(720);
    private List<int> clipOutput;
    private Matrix4x4 waterToClip,clipToWater;
    private Vector3 clipMin,clipMax;
    private Bounds surfaceBounds;
    private bool clipActive;
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
        renderer.receiveShadows = !NavalPerformance.LaptopMode;
        var horizon=new GameObject("Distant ocean");horizon.transform.SetParent(transform,false);
        distant=horizon.AddComponent<NavalDistantWater>();distant.Initialize(this);
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

    internal float WaveSampleTime { get { return simulationTime; } }

    public float Height(float x, float z)
    {
        x -= transform.position.x; z -= transform.position.z;
        if(renderedHeightReady){
            float gx=(x-renderedOriginX)/Spacing,gz=(z-renderedOriginZ)/Spacing;
            int ix=Mathf.FloorToInt(gx),iz=Mathf.FloorToInt(gz);
            if(ix>=0&&iz>=0&&ix<Count-1&&iz<Count-1){
                int a=iz*Count+ix;float u=gx-ix,v=gz-iz;
                float rendered=NavalWaterSurfaceMath.Triangle(heights[a],heights[a+1],heights[a+Count],heights[a+Count+1],u,v);
                if(!float.IsNaN(rendered)&&!float.IsInfinity(rendered))return transform.position.y+rendered;
            }
        }
        return transform.position.y+NavalWaveField.Sample(x,z,Amplitude,simulationTime);
    }

    private void ClipAdditionalHull(Transform ship)
    {
        if(ship==null||!PrepareClip(ship))return;
        float originZ=vertices[0].z;
        int first=Mathf.Max(0,Mathf.FloorToInt((clipMin.z-originZ)/Spacing)-1);
        int last=Mathf.Min(surfaceRows.Length-1,Mathf.FloorToInt((clipMax.z-originZ)/Spacing));
        for(int row=first;row<=last;row++)
        {
            bool clipped=clippedRows[row];
            List<int> input=clipped?surfaceRows[row]:baseRows[row];
            clipOutput=clipped?rowScratch:surfaceRows[row];
            clipOutput.Clear();
            for(int i=0;i<input.Count;i+=3)AddTriangle(input[i],input[i+1],input[i+2],ship);
            if(clipped){surfaceRows[row]=rowScratch;rowScratch=input;}
            clippedRows[row]=true;
        }
    }
    public void RenderWaves(Transform boat, float speed)
    {
        using(NavalPerformance.Measure(NavalPerfArea.Ocean))
        {
        simulationTime += Mathf.Min(Time.deltaTime, 0.05f);
        if(distant!=null)distant.Refresh(Focus,simulationTime,Amplitude);
        bool laptop=NavalPerformance.LaptopMode;
        if(Time.time<nextMeshAt||laptop&&Time.frameCount<nextMeshFrame)return;
        // Integrated GPUs pay both the CPU mesh upload and the GPU cost of a
        // double-sided 101x101 surface. Buoyancy still samples the wave field
        // every frame; only visual mesh uploads are paced more conservatively.
        nextMeshAt=Time.time+1f/(laptop?12f:30f);nextMeshFrame=Time.frameCount+(laptop?3:1);
        buildWatch.Restart();
        float offsetX = Mathf.Floor((Focus.x - transform.position.x) / 40f) * 40f;
        float offsetZ = Mathf.Floor((Focus.z - transform.position.z) / 40f) * 40f;
        Vector3 waterPosition=transform.position,boatPosition=boat.position;
        Matrix4x4 toBoat=boat.worldToLocalMatrix;
        waveField.Prepare(-Half*Spacing+offsetX,-Half*Spacing+offsetZ,Spacing,Amplitude,simulationTime);
        float minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
        float wakeStrength=Mathf.Abs(speed)*.035f;
        for (int z = 0; z < Count; z++)
        {
            float localZ=(z-Half)*Spacing+offsetZ;
            float worldZ=waterPosition.z+localZ;
            int row=z*Count;
            for (int x = 0; x < Count; x++)
            {
                int i=row+x;
                float localX=(x-Half)*Spacing+offsetX;
                float h=waveField.At(x,z);
                if(wakeStrength>0f)
                {
                    Vector3 relative=toBoat.MultiplyPoint3x4(new Vector3(waterPosition.x+localX,boatPosition.y,worldZ));
                    if(relative.z < -BoatDimensions.HalfLength && relative.z > -20f)
                    {
                        float width=BoatDimensions.HalfWidth+(-relative.z-BoatDimensions.HalfLength)*.4f;
                        float distance=(Mathf.Abs(relative.x)-width)*1.6f;
                        h+=Mathf.Exp(-distance*distance)*wakeStrength*Mathf.Exp((relative.z+BoatDimensions.HalfLength)*.13f)
                            *Mathf.Sin(relative.z*2f+simulationTime*4f);
                    }
                }
                vertices[i]=new Vector3(localX,h,localZ);
                heights[i]=h;
                minY=Mathf.Min(minY,h);maxY=Mathf.Max(maxY,h);
            }
        }
        surfaceBounds=new Bounds(new Vector3(offsetX,(minY+maxY)*.5f,offsetZ),new Vector3(2f*Half*Spacing,maxY-minY,2f*Half*Spacing));
        renderedOriginX=-Half*Spacing+offsetX;renderedOriginZ=-Half*Spacing+offsetZ;renderedHeightReady=true;
        indices.Clear();
        clippedVertices.Clear(); clippedVertices.AddRange(vertices);
        for(int z=0;z<surfaceRows.Length;z++)
        {
            clippedRows[z]=false;
            if(baseRows[z]!=null)continue;
            surfaceRows[z]=new List<int>(720);
            List<int> row=baseRows[z]=new List<int>(600);
            for(int x=0;x<Count-1;x++)
            {
                int a=z*Count+x,b=a+1,c=a+Count,d=c+1;
                row.Add(a);row.Add(c);row.Add(b);row.Add(b);row.Add(c);row.Add(d);
            }
        }
        ClipAdditionalHull(boat);
        var session=NavalTestWorld.Active;
        if(session!=null&&session.ConvoyMission)
        {
            for(int slot=0;slot<NavalPhysicsQueries.HullSlotCount;slot++)
            {
                var hull=NavalPhysicsQueries.HullAtSlot(slot);
                if(hull!=null&&!hull.SimulationRetired&&hull.Root!=boat)ClipAdditionalHull(hull.Root);
            }
        }
        else ClipAdditionalHull(AdditionalHull);
        for(int row=0;row<surfaceRows.Length;row++)indices.AddRange(clippedRows[row]?surfaceRows[row]:baseRows[row]);
        int frontCount=indices.Count;
        int frontVertices = clippedVertices.Count;
        normalField.Build(heights,Spacing);surfaceNormals.Clear();
        for(int i=0;i<frontVertices;i++)
        {
            Vector3 p=clippedVertices[i];NavalSurfaceNormal n=i<vertices.Length?normalField.At(i):normalField.Sample((p.x-offsetX)/Spacing+Half,(p.z-offsetZ)/Spacing+Half);
            surfaceNormals.Add(new Vector3(n.X,n.Y,n.Z));
        }
        for (int i = 0; i < frontVertices; i++) clippedVertices.Add(clippedVertices[i]);
        for (int i = 0; i < frontVertices; i++) surfaceNormals.Add(-surfaceNormals[i]);
        for (int i = 0; i < frontCount; i += 3)
        { indices.Add(indices[i + 2] + frontVertices); indices.Add(indices[i + 1] + frontVertices); indices.Add(indices[i] + frontVertices); }
        mesh.Clear(); mesh.SetVertices(clippedVertices); mesh.SetTriangles(indices, 0, false);
        mesh.SetNormals(surfaceNormals);mesh.bounds=surfaceBounds;
        buildWatch.Stop();BuildMilliseconds=(float)buildWatch.Elapsed.TotalMilliseconds;
            }
    }

    private void AddTriangle(int a, int b, int c, Transform boat)
    {
        Vector3 pa=clippedVertices[a],pb=clippedVertices[b],pc=clippedVertices[c];
        if(!clipActive||
           (pa.x<clipMin.x&&pb.x<clipMin.x&&pc.x<clipMin.x)||
           (pa.x>clipMax.x&&pb.x>clipMax.x&&pc.x>clipMax.x)||
           (pa.z<clipMin.z&&pb.z<clipMin.z&&pc.z<clipMin.z)||
           (pa.z>clipMax.z&&pb.z>clipMax.z&&pc.z>clipMax.z))
        {clipOutput.Add(a);clipOutput.Add(b);clipOutput.Add(c);return;}
        Vector3 va = waterToClip.MultiplyPoint3x4(pa);
        Vector3 vb = waterToClip.MultiplyPoint3x4(pb);
        Vector3 vc = waterToClip.MultiplyPoint3x4(pc);
        // Cut at the middle of the solid walls, not outside them. The walls cover the seam.
        // A centroid test leaves whole triangular holes and lets overlapping corners enter the hold.
        for (int plane = 0; plane < clipPlanes; plane++)
            if (Distance(va, plane) >= 0f && Distance(vb, plane) >= 0f && Distance(vc, plane) >= 0f)
            { clipOutput.Add(a); clipOutput.Add(b); clipOutput.Add(c); return; }
        polygon.Clear(); polygon.Add(va); polygon.Add(vb); polygon.Add(vc);
        // Subtract the convex hull mask. Emit only disjoint outside pieces at each plane.
        for (int plane = 0; plane < clipPlanes && polygon.Count >= 3; plane++)
        {
            Split(polygon, plane);
            Emit(outside, boat);
            polygon.Clear(); polygon.AddRange(inside);
        }
        // Remaining polygon lies inside the boat below the rim and is intentionally discarded.
    }
    private bool ironcladClip;
    private int clipPlanes=5;
    private float clipRim=BoatDimensions.Rim;
    private bool PrepareClip(Transform boat)
    {
        NavalSeaLadder equipment=boat.GetComponent<NavalSeaLadder>();
        clipDefinition=equipment!=null?equipment.Hull.Definition:null;
        ironcladClip=equipment!=null&&equipment.Hull.Definition.SteamPowered;
        clipPlanes=!ironcladClip&&equipment!=null&&equipment.Hull.Definition.DeckCount>2?9:5;
        clipRim=equipment!=null?equipment.Hull.Definition.RimY:BoatDimensions.Rim;
        waterToClip=boat.worldToLocalMatrix*transform.localToWorldMatrix;
        clipToWater=transform.worldToLocalMatrix*boat.localToWorldMatrix;
        Vector3 low=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),high=-low;
        for(int corner=0;corner<8;corner++)
        {
            Vector3 p=waterToClip.MultiplyPoint3x4(Corner(surfaceBounds.min,surfaceBounds.max,corner));
            low=Vector3.Min(low,p);high=Vector3.Max(high,p);
        }
        clipActive=low.x<BoatDimensions.HalfWidth&&high.x>-BoatDimensions.HalfWidth&&
            low.z<BoatDimensions.HalfLength&&high.z>-BoatDimensions.HalfLength&&low.y<clipRim;
        if(!clipActive)return false;
        // Limit the infinite downward mask to the actual ocean AABB, then project it back.
        Vector3 maskLow=new Vector3(-BoatDimensions.HalfWidth,low.y-.001f,-BoatDimensions.HalfLength);
        Vector3 maskHigh=new Vector3(BoatDimensions.HalfWidth,clipRim,BoatDimensions.HalfLength);
        clipMin=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);clipMax=-clipMin;
        for(int corner=0;corner<8;corner++)
        {
            Vector3 p=clipToWater.MultiplyPoint3x4(Corner(maskLow,maskHigh,corner));
            clipMin=Vector3.Min(clipMin,p);clipMax=Vector3.Max(clipMax,p);
        }
        clipMin-=Vector3.one*.001f;clipMax+=Vector3.one*.001f;
        return true;
    }
    private static Vector3 Corner(Vector3 low,Vector3 high,int corner)
    {return new Vector3((corner&1)==0?low.x:high.x,(corner&2)==0?low.y:high.y,(corner&4)==0?low.z:high.z);}

    private NavalShipDefinition clipDefinition;
    private float Distance(Vector3 p, int plane)
    {
        switch (plane)
        {
            case 0: return p.x - (clipDefinition!=null?clipDefinition.HullHalfWidth(p.z):BoatDimensions.HalfWidth);
            case 1: return -p.x - (clipDefinition!=null?clipDefinition.HullHalfWidth(p.z):BoatDimensions.HalfWidth);
            case 2: return p.z - BoatDimensions.HalfLength;
            case 3: return -p.z - BoatDimensions.HalfLength;
            case 4: return p.y - (clipDefinition!=null&&clipDefinition.Destroyer?clipDefinition.RimAt(p.z):clipRim);
            default: return NavalHullOutline.EndPlane(p.x,p.z,plane-5);
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
                if(ironcladClip&&plane<2){float lo=0,hi=1;for(int n=0;n<18;n++){float mid=(lo+hi)*.5f;if((Distance(Vector3.Lerp(previous,current,mid),plane)<0)==(previousDistance<0))lo=mid;else hi=mid;}t=(lo+hi)*.5f;}
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
        { clipOutput.Add(first); clipOutput.Add(first + i); clipOutput.Add(first + i + 1); }
    }
    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (surfaceMaterial != null) Destroy(surfaceMaterial);
        if (InteriorMaterial != null) Destroy(InteriorMaterial);
    }
}
