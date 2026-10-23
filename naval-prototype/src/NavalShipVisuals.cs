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
    private NavalStations sailControls;
    private float sailTop;
    private float ownPower=1f,mastZ=1.6f,ownArea=1f;
    public float SailPowerFactor {get{float total=ownPower*ownArea,area=ownArea;foreach(NavalShipVisuals rig in secondaryRigs){total+=rig.SailPowerFactor*rig.ownArea;area+=rig.ownArea;}return total/area;}private set{ownPower=value;}}
    public void Initialize(NavalHull owner, Material timber, Material trim, Material mast, Material fabric, Material dark, Material deck, Material alternatePlank)
    {
        hull = owner;
        foreach(Material fuel in new Material[]{timber,trim,mast,fabric,dark,deck,alternatePlank})owner.RegisterFireMaterial(fuel);
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
            if(owner.Definition.DeckCount<3)Box(new Vector3(side*2.80f,0.86f,0f),new Vector3(0.12f,0.07f,11.75f),trim,true);
            if(owner.Definition.DeckCount<3)Box(new Vector3(side*2.80f,hull.Floor+0.08f,0f),new Vector3(0.12f,0.16f,11.75f),trim,true);
            for(int gun=0;gun<hull.Definition.GunCount;gun++)
            {
                if(NavalShipDefinition.GunSide(gun)!=side)continue;
                float gunZ=NavalShipDefinition.GunZ(hull.Definition,gun);
                for(int end=-1;end<=1;end+=2)
                {
                    if(hull.Definition.DeckCount<3)Box(new Vector3(side*3.02f,1.53f,gunZ+end*1.3f),new Vector3(.36f,1.2f,.14f),trim,true);
                    else
                    {
                        float bottom=hull.Definition.GunPortSill,top=hull.Definition.GunPortTop;
                        RawBox(new Vector3(side*3.02f,(bottom+top)*.5f,gunZ+end*1.3f),new Vector3(.36f,top-bottom+.14f,.14f),trim,true);
                    }
                }
                if(hull.Definition.DeckCount>2)RawBox(new Vector3(side*3.02f,hull.Definition.GunPortTop+.06f,gunZ),new Vector3(.36f,.12f,2.4f),trim,true);
                if(hull.Definition.DeckCount<3)Box(new Vector3(side*3.02f,BoatDimensions.CannonPortSill-.05f,gunZ),new Vector3(.36f,.1f,2.4f),trim,true);
                else RawBox(new Vector3(side*3.02f,hull.Definition.GunPortSill-.05f,gunZ),new Vector3(.36f,.1f,2.4f),trim,true);
            }
        }
        // Lower-deck frames and stern windows give the taller hull a sloop silhouette.
        for(int frame=0;frame<5;frame++)for(int side=-1;side<=1;side+=2)
            Box(new Vector3(side*2.78f,(hull.Floor+.575f)*.5f,-4.5f+frame*2.2f),new Vector3(0.18f,.575f-hull.Floor,0.18f),trim,true);
        if(owner.Definition.DeckCount<3)for(int window=0;window<3;window++)
            Box(new Vector3(-1.5f+window*1.5f,hull.Definition.HullY(-.65f),-6.14f),new Vector3(0.75f,0.7f,0.08f),dark,false);
        else BuildGalleonDetails(trim,dark);
        for(int i=0;i<3;i++) ForeBox(new Vector3(0f,0.65f+i*0.12f,6.1f+i*0.45f),new Vector3(4.2f-i*1.1f,0.45f,0.6f),timber);
        ForeBox(new Vector3(0f,1.25f,7.4f),new Vector3(0.23f,0.23f,3.2f),trim);
        RailBox(new Vector3(0f,1.78f,-6f),new Vector3(6.2f,0.25f,0.35f),trim,true);
        for(int side=-1;side<=1;side+=2)
        {
            RailBox(new Vector3(side*3f,1.78f,0f),new Vector3(0.32f,0.22f,12.1f),trim,true);
            if(owner.Definition.DeckCount<3)Box(new Vector3(side*3.135f,0.05f,0f),new Vector3(0.025f,0.1f,11.8f),trim,false);
            for(int i=0;i<9;i++)
            {
                RailBox(new Vector3(side*3f,1.95f,-4.8f+i*1.2f),new Vector3(0.18f,0.3f,0.18f),trim,true);
            }
            RailBox(new Vector3(side*3f,2.12f,0f),new Vector3(0.2f,0.16f,11.8f),trim,true);
        }
        if(owner.Definition.DeckCount>2)BuildWeatherDeck(deck,alternatePlank,trim);
        InitializeRig(owner,timber,trim,mast,fabric,owner.Definition.MastZ[0],owner.Definition.SailSize(0));
        for(int mastIndex=1;mastIndex<owner.Definition.MastZ.Length;mastIndex++)
        {
            NavalShipVisuals secondary=gameObject.AddComponent<NavalShipVisuals>();secondaryRigs.Add(secondary);
            secondary.InitializeRig(owner,timber,trim,mast,fabric,owner.Definition.MastZ[mastIndex],owner.Definition.SailSize(mastIndex));
        }
    }
    private void RawBox(Vector3 position,Vector3 size,Material material,bool solid)
    {hull.AddDecorationBox(transform,material,position,size,solid,false);}
    private void RawDeck(Vector3 position,Vector3 size,Material first,Material second)
    {
        int rows=Mathf.CeilToInt(size.z/.25f);float dz=size.z/rows;
        for(int row=0;row<rows;row++)
        {
            float z=position.z+(row+.5f)*dz-size.z*.5f,half=hull.Definition.HullHalfWidth(z)-.125f;
            float left=Mathf.Max(position.x-size.x*.5f,-half),right=Mathf.Min(position.x+size.x*.5f,half);
            if(right<=left)continue;int count=Mathf.CeilToInt((right-left)/.45f);float width=(right-left)/count;
            for(int i=0;i<count;i++)RawBox(new Vector3(left+(i+.5f)*width,position.y,z),new Vector3(width,size.y,dz),i%2==0?first:second,true);
        }
    }
    private void ForeBox(Vector3 position,Vector3 size,Material material)
    {
        if(hull.Definition.DeckCount<3){Box(position,size,material,true);return;}
        float low=hull.Definition.AboveDeckY(position.y-size.y*.5f),high=hull.Definition.AboveDeckY(position.y+size.y*.5f);
        position.y=(low+high)*.5f+hull.Definition.UpperDeckOffset;size.y=high-low;size.x*=.7f;RawBox(position,size,material,true);
    }
    private void SideBand(int side,float y,float height,Material material)
    {
        for(int i=0;i<48;i++)
        {float z=-5.875f+i*.25f;RawBox(new Vector3(side*(hull.Definition.HullHalfWidth(z)+.02f),y,z),new Vector3(.1f,height,.25f),material,false);}
    }
    private void BuildGalleonDetails(Material trim,Material dark)
    {
        for(int side=-1;side<=1;side+=2)
        {SideBand(side,hull.Definition.DesignWaterline+.05f,.06f,dark);SideBand(side,hull.Definition.WeatherDeck-.25f,.07f,trim);}
        // Distinct dark wales at both enclosed decks make the three-level hull readable.
        for(int side=-1;side<=1;side+=2){
            SideBand(side,.825f-.22f,.075f,dark);
            SideBand(side,hull.Floor+.16f,.06f,dark);
        }
        float y=(hull.Definition.DesignWaterline+hull.Definition.WeatherDeck)*.5f;
        for(int i=0;i<5;i++)
        {
            float x=-1.6f+i*.8f;
            RawBox(new Vector3(x,y,-6.14f),new Vector3(.6f,.45f,.08f),dark,false);
            for(int sign=-1;sign<=1;sign+=2)
            {
                RawBox(new Vector3(x+sign*.34f,y,-6.16f),new Vector3(.08f,.6f,.12f),trim,true);
                RawBox(new Vector3(x,y+sign*.27f,-6.16f),new Vector3(.75f,.08f,.12f),trim,true);
            }
        }
    }
    private void RailBox(Vector3 position,Vector3 size,Material material,bool solid)
    {
        if(hull.Definition.DeckCount<3){Box(position,size,material,solid);return;}
        float low=hull.Definition.AboveDeckY(position.y-size.y*.5f),high=hull.Definition.AboveDeckY(position.y+size.y*.5f);
        position.y=(low+high)*.5f+hull.Definition.UpperDeckOffset;size.y=high-low;
        if(Mathf.Abs(position.x)>2.9f&&size.z>8)
        {
            int rows=Mathf.CeilToInt(size.z/.25f);float dz=size.z/rows;
            for(int i=0;i<rows;i++)
            {float z=position.z+(i+.5f)*dz-size.z*.5f;hull.AddDecorationBox(transform,material,new Vector3(Mathf.Sign(position.x)*hull.Definition.HullHalfWidth(z),position.y,z),new Vector3(size.x,size.y,dz),solid,true);}
        }
        else
        {
            if(position.z< -5.9f&&size.x>5)size.x=2*hull.Definition.HullHalfWidth(-6)+.2f;
            hull.AddDecorationBox(transform,material,position,size,solid,true);
        }
    }
    private void BuildWeatherDeck(Material deck,Material alternate,Material trim)
    {
        float offset=hull.Definition.UpperDeckOffset,y=hull.Definition.WeatherDeck,opening=hull.Definition.StairWidth*.5f+.025f;
        float width=BoatDimensions.HoldWidth*.5f-opening,center=(BoatDimensions.HoldWidth*.5f+opening)*.5f;
        RawDeck(new Vector3(-center,y-.125f,0),new Vector3(width,.25f,11.75f),deck,alternate);
        RawDeck(new Vector3(center,y-.125f,0),new Vector3(width,.25f,11.75f),deck,alternate);
        RawDeck(new Vector3(0,y-.125f,-4.4375f),new Vector3(opening*2,.25f,2.875f),deck,alternate);
        RawDeck(new Vector3(0,y-.125f,3.1875f),new Vector3(opening*2,.25f,5.375f),deck,alternate);
        for(int step=0;step<NavalWalkRoutes.StepCount;step++)
        {
            float top=hull.Floor+(.825f-hull.Floor)*(step+1)/NavalWalkRoutes.StepCount+offset;
            float run=3.5f/NavalWalkRoutes.StepCount,z=-3+(step+.5f)*run;
            RawBox(new Vector3(0,top-.0625f,z),new Vector3(hull.Definition.StairWidth,.125f,run),step%2==0?deck:alternate,true);
            RawBox(new Vector3(0,top-.012f,z-run*.5f+.02f),new Vector3(hull.Definition.StairWidth,.024f,.04f),trim,true);
        }
        for(int side=-1;side<=1;side+=2)
        {
            RawBox(new Vector3(side*(opening+.05f),y+.015f,-1.25f),new Vector3(.1f,.03f,3.5f),trim,true);
            SideBand(side,y+.035f,.07f,trim);
        }
    }
    private void InitializeRig(NavalHull owner,Material timber,Material trim,Material mast,Material fabric,float z,float factor=1f)
    {
        hull=owner;mastZ=z;buildingRig=true;
        float baseY=owner.Definition.WeatherDeck,topY=baseY+(8.7f-.825f)*factor;
        ownArea=factor*factor;
        // Step the mast into the weather deck, not down into the walking space of the hold.
        float foot=owner.Definition.MastFootY;
        RigBox(new Vector3(0f,(topY+foot)*.5f,mastZ),new Vector3(.3f,topY-foot,.3f),mast,mastCells);
        float bootHeight=.55f*factor,bootWidth=owner.Definition.MastFootWidth*factor;
        RigBox(new Vector3(0f,baseY+bootHeight*.5f,mastZ),new Vector3(bootWidth,bootHeight,bootWidth),mast,mastCells);
        RigBox(new Vector3(0f,baseY+(7.8f-.825f)*factor,mastZ),new Vector3(5.9f*factor,.2f*factor,.2f*factor),trim,attachedRig);
        RigBox(new Vector3(0f,baseY+(3.62f-.825f)*factor,mastZ),new Vector3(5.7f*factor,.16f*factor,.18f*factor),timber,attachedRig);
        RigBox(new Vector3(0f,baseY+(8.5f-.825f)*factor,mastZ),new Vector3(1.1f*factor,.16f*factor,.18f*factor),trim,attachedRig);
        int columns=Mathf.CeilToInt(22*factor),rows=Mathf.CeilToInt(16*factor);
        for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
        {
            float u=(x+.5f)/columns,v=(y+.5f)/rows;
            float clothY=owner.Definition.DeckCount<3?3.7f+v*4:baseY+(3.7f-.825f+v*4)*factor;
            Vector3 center=new Vector3((u-.5f)*5.5f*factor,clothY,mastZ+.02f+Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI)*.5f);
            sail.Add(hull.AddVoxel(transform,fabric,center,new Vector3(.25f*factor*22/columns,.25f*factor*16/rows,.07f),false,false));
        }
        attachedRig.AddRange(sail);
        foreach(NavalHullVoxel cell in sail)cell.RigFabric=true;
        foreach (NavalHullVoxel cell in mastCells){cell.MastTimber=true;originalSizes[cell] = cell.transform.localScale;}
        // Reinforce the small deck footing too: losing every support plank in the
        // same first blast must not bypass intact mast timber durability.
        foreach(NavalHullVoxel cell in hull.Voxels)
            if(!cell.ManagedRig&&!originalSizes.ContainsKey(cell)&&Mathf.Abs(cell.LocalCenter.x)<.38f&&Mathf.Abs(cell.LocalCenter.z-mastZ)<.38f&&Mathf.Abs(cell.LocalCenter.y-(baseY-.125f))<.14f)
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
        supportGraph=new NavalRigSupportGraph(bounds,cloth,vertical,fabricIndex,columns,baseY);
        float[] sailX=new float[sail.Count],sailY=new float[sail.Count];
        for(int i=0;i<sail.Count;i++){sailX[i]=sail[i].LocalCenter.x;sailY[i]=sail[i].LocalCenter.y;}
        sailTop=sailY[sailY.Length-1]+originalSizes[sail[sail.Count-1]].y*.5f;
        sailShape=owner.Definition.DeckCount<3?new NavalSailShape(sailX,sailY):
            new NavalSailShape(sailX,sailY,sailX[0],sailX[columns-1]-sailX[0],sailY[0],sailY[sailY.Length-1]-sailY[0]);
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
        if(hull.Definition.DeckCount>2)
        {
            float low=hull.Definition.AboveDeckY(position.y-size.y*.5f),high=hull.Definition.AboveDeckY(position.y+size.y*.5f);
            position.y=(low+high)*.5f;size.y=high-low;RawDeck(position,size,first,second);return;
        }
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
    private readonly NavalSailPoseClock poseClock=new NavalSailPoseClock();
    private void Update()
    {
        RefreshRig();
        if(sailShape==null)return;
        using(NavalPerformance.Measure(NavalPerfArea.Sail))
        {
            if(sailControls==null)sailControls=NavalStations.ForShip(hull.Root);
            float deployed=sailControls!=null?Mathf.Abs(sailControls.SailSetting):1f;
            var world=NavalTestWorld.Active;Camera view=world!=null?world.ViewCamera:null;
            bool distant=view!=null&&(view.transform.position-transform.position).sqrMagnitude>160f*160f;
            if(!poseClock.Due(Time.time,deployed,distant))return;
            float clothScale=Mathf.Max(.06f,deployed);
            sailShape.Prepare(Time.time);
            for(int i=0;i<sail.Count;i++)
            {
                NavalHullVoxel voxel=sail[i];if(voxel.Missing||voxel.Detached||voxel.transform.parent!=transform)continue;
                NavalSailPose pose=sailShape.At(i);Vector3 point=voxel.LocalCenter;point.y=NavalSailControlMath.ClothY(point.y,sailTop,deployed);point.x+=pose.X*deployed;point.z=mastZ+pose.Z*deployed;
                Vector3 clothSize=originalSizes[voxel];clothSize.y*=clothScale;voxel.transform.localScale=clothSize;
                Quaternion rotation=Quaternion.FromToRotation(Vector3.forward,new Vector3(-pose.SlopeX,-pose.SlopeY,1f));
                voxel.transform.SetLocalPositionAndRotation(point,rotation);
                if(voxel.RigBatchOwner!=null)voxel.RigBatchOwner.SetAnimatedPose(voxel.RigBatchIndex,point,rotation,clothScale);
            }
        }
    }
}
