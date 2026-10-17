using System.Collections.Generic;
using UnityEngine;

public class NavalShipVisuals : MonoBehaviour
{
    private NavalHull hull;
    private readonly List<NavalHullVoxel> sail = new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> mastCells = new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> attachedRig = new List<NavalHullVoxel>();
    private readonly Dictionary<NavalHullVoxel, Vector3> originalSizes = new Dictionary<NavalHullVoxel, Vector3>();
    private readonly List<GameObject> debris = new List<GameObject>();
    private readonly List<NavalHullVoxel> rigCells=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> falling=new List<NavalHullVoxel>();
    private NavalRigSupportGraph supportGraph;
    private bool[] missingRig;
    private NavalSailShape sailShape;
    private bool rigDirty=true;
    private readonly List<NavalHullVoxel> foundationCells=new List<NavalHullVoxel>();
    private readonly List<NavalShipVisuals> secondaryRigs=new List<NavalShipVisuals>();
    private bool buildingRig;
    private float ownPower=1f,mastZ=1.6f;
    public float SailPowerFactor {get{float total=ownPower;foreach(NavalShipVisuals rig in secondaryRigs)total+=rig.SailPowerFactor;return total/(secondaryRigs.Count+1);}private set{ownPower=value;}}
    public void Initialize(NavalHull owner, Material timber, Material trim, Material mast, Material fabric, Material dark, Material deck, Material alternatePlank)
    {
        hull = owner;
        // The central aft companionway leaves solid deck beneath both guns.
        // The hydraulic bottom forms the walkable lower deck at the profile floor.
        float opening=owner.Definition.StairWidth*.5f+.025f;
        float width=BoatDimensions.HoldWidth*.5f-opening,center=(BoatDimensions.HoldWidth*.5f+opening)*.5f;
        Deck(new Vector3(-center,.7f,0f),new Vector3(width,.25f,11.75f),deck,alternatePlank);
        Deck(new Vector3(center,.7f,0f),new Vector3(width,.25f,11.75f),deck,alternatePlank);
        // A full-width aft landing keeps the centred helm clear of the stairs.
        Deck(new Vector3(0f,0.7f,-4.4375f),new Vector3(opening*2f,0.25f,2.875f),deck,alternatePlank);
        Deck(new Vector3(0f,0.7f,3.1875f),new Vector3(opening*2f,0.25f,5.375f),deck,alternatePlank);
        // Broad, shallow steps: walk down toward the stern, up toward the bow.
        for (int step=0;step<NavalWalkRoutes.StepCount;step++)
        {
            float top=hull.Floor+(0.825f-hull.Floor)*(step+1)/NavalWalkRoutes.StepCount;
            float run=3.5f/NavalWalkRoutes.StepCount;
            float z=-3f+(step+0.5f)*run;
            Box(new Vector3(0f,top-0.0625f,z),new Vector3(owner.Definition.StairWidth,0.125f,run),step%2==0?deck:alternatePlank,true);
            Box(new Vector3(0f,top-0.012f,z-run*0.5f+0.02f),new Vector3(owner.Definition.StairWidth,0.024f,0.04f),trim,true);
        }
        // Contrasting companionway posts mark the opening without blocking it.
        for(int side=-1;side<=1;side+=2)
        {
            Box(new Vector3(side*(opening+.05f),0.84f,-1.25f),new Vector3(0.1f,0.03f,3.5f),trim,true);
            Box(new Vector3(side*2.80f,0.86f,0f),new Vector3(0.12f,0.07f,11.75f),trim,true);
            Box(new Vector3(side*2.80f,hull.Floor+0.08f,0f),new Vector3(0.12f,0.16f,11.75f),trim,true);
            for(int gun=0;gun<hull.Definition.GunCount;gun++)
            {
                if(NavalShipDefinition.GunSide(gun)!=side)continue;
                float gunZ=NavalShipDefinition.GunZ(gun);
                for(int end=-1;end<=1;end+=2)
                    Box(new Vector3(side*3.02f,1.53f,gunZ+end*1.3f),new Vector3(.36f,1.2f,.14f),trim,true);
                Box(new Vector3(side*3.02f,.975f,gunZ),new Vector3(.36f,.1f,2.4f),trim,true);
            }
        }
        // Lower-deck frames and stern windows give the taller hull a sloop silhouette.
        for(int frame=0;frame<5;frame++)for(int side=-1;side<=1;side+=2)
            Box(new Vector3(side*2.78f,(hull.Floor+.575f)*.5f,-4.5f+frame*2.2f),new Vector3(0.18f,.575f-hull.Floor,0.18f),trim,true);
        for(int window=0;window<3;window++)
            Box(new Vector3(-1.5f+window*1.5f,hull.Definition.HullY(-.65f),-6.14f),new Vector3(0.75f,0.7f,0.08f),dark,false);
        for(int i=0;i<3;i++) Box(new Vector3(0f,0.65f+i*0.12f,6.1f+i*0.45f),new Vector3(4.2f-i*1.1f,0.45f,0.6f),timber,true);
        Box(new Vector3(0f,1.25f,7.4f),new Vector3(0.23f,0.23f,3.2f),trim,true);
        Box(new Vector3(0f,1.78f,-6f),new Vector3(6.2f,0.25f,0.35f),trim,true);
        for(int side=-1;side<=1;side+=2)
        {
            Box(new Vector3(side*3f,1.78f,0f),new Vector3(0.32f,0.22f,12.1f),trim,true);
            Box(new Vector3(side*3.135f,0.05f,0f),new Vector3(0.025f,0.1f,11.8f),trim,false);
            for(int i=0;i<9;i++)
            {
                Box(new Vector3(side*3f,1.95f,-4.8f+i*1.2f),new Vector3(0.18f,0.3f,0.18f),trim,true);
            }
            Box(new Vector3(side*3f,2.12f,0f),new Vector3(0.2f,0.16f,11.8f),trim,true);
        }
        InitializeRig(owner,timber,trim,mast,fabric,owner.Definition.MastZ[0]);
        for(int mastIndex=1;mastIndex<owner.Definition.MastZ.Length;mastIndex++)
        {
            NavalShipVisuals secondary=gameObject.AddComponent<NavalShipVisuals>();secondaryRigs.Add(secondary);
            secondary.InitializeRig(owner,timber,trim,mast,fabric,owner.Definition.MastZ[mastIndex]);
        }
    }
    private void InitializeRig(NavalHull owner,Material timber,Material trim,Material mast,Material fabric,float z)
    {
        hull=owner;mastZ=z;buildingRig=true;
        RigBox(new Vector3(0f,4.1f,mastZ),new Vector3(0.3f,9.2f,0.3f),mast,mastCells);
        RigBox(new Vector3(0f,1.1f,mastZ),new Vector3(0.65f,0.55f,0.65f),mast,mastCells);
        RigBox(new Vector3(0f,7.8f,mastZ),new Vector3(5.9f,0.2f,0.2f),trim,attachedRig);
        // A lower boom fixes the foot of the sail to the lower mast.
        RigBox(new Vector3(0f,3.62f,mastZ),new Vector3(5.7f,0.16f,0.18f),timber,attachedRig);
        RigBox(new Vector3(0f,8.5f,mastZ),new Vector3(1.1f,0.16f,0.18f),trim,attachedRig);
        // One sail made of removable cells, not an indestructible cloth mesh.
        for(int y=0;y<16;y++)for(int x=0;x<22;x++)
        {
            float u=(x+0.5f)/22f,v=(y+0.5f)/16f;
            Vector3 center=new Vector3((u-0.5f)*5.5f,3.7f+v*4f,mastZ+.02f+Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI)*0.5f);
            sail.Add(hull.AddVoxel(transform,fabric,center,new Vector3(0.25f,0.25f,0.07f),false,false));
        }
        attachedRig.AddRange(sail);
        foreach(NavalHullVoxel cell in sail)cell.RigFabric=true;
        foreach (NavalHullVoxel cell in mastCells){cell.MastTimber=true;originalSizes[cell] = cell.transform.localScale;}
        // Reinforce the small deck footing too: losing every support plank in the
        // same first blast must not bypass intact mast timber durability.
        foreach(NavalHullVoxel cell in hull.Voxels)
            if(!cell.ManagedRig&&!originalSizes.ContainsKey(cell)&&Mathf.Abs(cell.LocalCenter.x)<.38f&&Mathf.Abs(cell.LocalCenter.z-mastZ)<.38f&&Mathf.Abs(cell.LocalCenter.y-.7f)<.14f)
                {cell.MastTimber=true;foundationCells.Add(cell);}
        foreach (NavalHullVoxel cell in attachedRig) originalSizes[cell] = cell.transform.localScale;
        foreach (NavalHullVoxel cell in originalSizes.Keys) cell.ManagedRig = true;
        rigCells.AddRange(mastCells);rigCells.AddRange(attachedRig);
        int n=rigCells.Count;float[,] bounds=new float[n,6];bool[] cloth=new bool[n],vertical=new bool[n];int[] fabricIndex=new int[n];
        missingRig=new bool[n];
        for(int i=0;i<n;i++)
        {
            NavalHullVoxel cell=rigCells[i];cell.RigOwner=this;cloth[i]=cell.RigFabric;vertical[i]=i<mastCells.Count;fabricIndex[i]=cloth[i]?sail.IndexOf(cell):-1;
            for(int axis=0;axis<3;axis++){bounds[i,axis]=cell.LocalCenter[axis];bounds[i,axis+3]=originalSizes[cell][axis]*.5f;}
        }
        foreach(NavalHullVoxel cell in foundationCells)cell.RigOwner=this;
        supportGraph=new NavalRigSupportGraph(bounds,cloth,vertical,fabricIndex,22,.825f);
        float[] sailX=new float[sail.Count],sailY=new float[sail.Count];
        for(int i=0;i<sail.Count;i++){sailX[i]=sail[i].LocalCenter.x;sailY[i]=sail[i].LocalCenter.y;}
        sailShape=new NavalSailShape(sailX,sailY);
        List<NavalHullVoxel> timberCells=new List<NavalHullVoxel>();
        foreach(NavalHullVoxel cell in rigCells)if(!cell.RigFabric)timberCells.Add(cell);
        NavalRigRenderBatch.Create(transform,timberCells,false);
        NavalRigRenderBatch.Create(transform,sail,true);
        SailPowerFactor = 1f;buildingRig=false;
    }
    private void RigBox(Vector3 position, Vector3 size, Material material, List<NavalHullVoxel> target)
    {
        int first = hull.Voxels.Count;
        Box(position, size, material, true);
        for (int i = first; i < hull.Voxels.Count; i++) target.Add(hull.Voxels[i]);
    }
    private void Deck(Vector3 position, Vector3 size, Material first, Material second)
    {
        int count=Mathf.CeilToInt(size.x/0.45f);
        float width=size.x/count;
        for(int i=0;i<count;i++)
            Box(position+new Vector3((i+0.5f)*width-size.x*0.5f,0f,0f),
                new Vector3(width,size.y,size.z),i%2==0?first:second,true);
    }
    public void RestoreRig()
    {
        foreach(NavalShipVisuals rig in secondaryRigs)rig.RestoreRig();
        foreach (GameObject group in debris)
        {
            Rigidbody body = group.GetComponent<Rigidbody>();
            if (body != null) { body.isKinematic = true; body.detectCollisions = false; }
        }
        foreach (KeyValuePair<NavalHullVoxel, Vector3> entry in originalSizes)
        {
            Transform cell = entry.Key.transform;
            cell.SetParent(transform, false);
            cell.localPosition = entry.Key.LocalCenter;
            cell.localRotation = Quaternion.identity;
            cell.localScale = entry.Value;
            entry.Key.Detached=false;entry.Key.PhysicsCollider.isTrigger = entry.Key.RigFabric;entry.Key.NotifyVisualChanged();
        }
        foreach (GameObject group in debris) Destroy(group);
        debris.Clear(); rigDirty=true;
        RefreshRig();
    }
    public void MarkRigDirty(){rigDirty=true;}
    public void RefreshRig()
    {
        foreach(NavalShipVisuals rig in secondaryRigs)rig.RefreshRig();
        if(!rigDirty||supportGraph==null)return;
        using(NavalPerformance.Measure(NavalPerfArea.RigSupport))
        {
            rigDirty=false;bool foundation=false;
            foreach(NavalHullVoxel cell in foundationCells)if(!cell.Missing&&!cell.Detached&&cell.transform.parent==transform){foundation=true;break;}
            for(int i=0;i<rigCells.Count;i++)missingRig[i]=rigCells[i].Missing||rigCells[i].Detached||rigCells[i].transform.parent!=transform;
            supportGraph.Evaluate(missingRig,foundation);
            foreach(List<int> component in supportGraph.FallingComponents(missingRig))
            {
                if(component.Count==0)continue;falling.Clear();
                foreach(int index in component)falling.Add(rigCells[index]);
                Drop(falling);
            }
            int remaining=0;foreach(NavalHullVoxel cell in sail)if(!cell.Missing&&!cell.Detached&&cell.transform.parent==transform)remaining++;
            SailPowerFactor=NavalRigMath.SailPower(remaining,sail.Count,true);
        }
    }
    private void Drop(List<NavalHullVoxel> cells)
    {
        GameObject group = new GameObject("NavalPrototype_FallenRig");
        group.layer = PhysicsManager.GetEnvironmentLayer();
        group.transform.SetParent(transform.parent, false);
        group.transform.position = cells[0].transform.position;
        group.transform.rotation = transform.rotation;
        foreach (NavalHullVoxel cell in cells)
        {
            cell.Detached=true;cell.transform.SetParent(group.transform, true);cell.NotifyVisualChanged();
            // Cloth cells become physical debris, so they land even if all
            // wooden supports were removed. Missing cells stay disabled.
            cell.PhysicsCollider.isTrigger = false;
        }
        // Cloth below a high cut overlaps the remaining mast. Those contacts
        // used to pin the detached assembly in place instead of letting it fall.
        foreach (NavalHullVoxel cell in cells)
            foreach (NavalHullVoxel support in mastCells)
                if (!support.Missing && support.transform.parent == transform)
                    Physics.IgnoreCollision(cell.PhysicsCollider, support.PhysicsCollider, true);
        Rigidbody body = group.AddComponent<Rigidbody>();
        body.mass = Mathf.Clamp(cells.Count*.4f,.5f,80f); body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.angularVelocity = transform.right * 0.45f;
        debris.Add(group);hull.Revision++;
    }
    private void Box(Vector3 position,Vector3 size,Material material,bool solid)
    {
        if(!buildingRig)
        {
            float low=hull.Definition.AboveDeckY(position.y-size.y*.5f),high=hull.Definition.AboveDeckY(position.y+size.y*.5f);
            position.y=(low+high)*.5f;size.y=high-low;
        }
        hull.AddDecorationBox(transform,material,position,size,solid,true);
    }
    private void Update()
    {
        RefreshRig();
        if(sailShape==null)return;
        using(NavalPerformance.Measure(NavalPerfArea.Sail))
        {
            sailShape.Prepare(Time.time);
            for(int i=0;i<sail.Count;i++)
            {
                NavalHullVoxel voxel=sail[i];if(voxel.Missing||voxel.Detached||voxel.transform.parent!=transform)continue;
                NavalSailPose pose=sailShape.At(i);Vector3 point=voxel.LocalCenter;point.x+=pose.X;point.z=mastZ+pose.Z;
                voxel.transform.localPosition=point;
                voxel.transform.localRotation=Quaternion.FromToRotation(Vector3.forward,new Vector3(-pose.SlopeX,-pose.SlopeY,1f));
            }
        }
    }
}
