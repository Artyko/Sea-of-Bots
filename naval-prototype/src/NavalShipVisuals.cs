using System.Collections.Generic;
using UnityEngine;

public class NavalShipVisuals : MonoBehaviour,INavalPropulsion
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
    private NavalSteamPlant steamPlant;
    private NavalStations sailControls;
    private float sailTop;
    private float ownPower=1f,mastZ=1.6f,ownArea=1f;
    public float PowerFactor {get{return steamPlant!=null?steamPlant.PowerFactor:SailPowerFactor;}}
    public NavalPropulsionKind Kind {get{return hull!=null?hull.Definition.PropulsionKind:NavalPropulsionKind.Sail;}}
    public float Command {get{if(steamPlant!=null)return steamPlant.Setting;if(sailControls==null&&hull!=null)sailControls=NavalStations.ForShip(hull.Root);return sailControls!=null?sailControls.SailSetting:0f;}}
    public float Direction {get{return Mathf.Sign(Command);}}
    public bool StationReady {get{return steamPlant!=null?steamPlant.Coal>0f&&steamPlant.BoilerIntegrity>0f&&steamPlant.EngineIntegrity>0f:sail.Count>0;}}
    public float UpdatePropulsion(float dt,float command,bool operatorPresent,Vector3 shipVelocity)
    {return steamPlant!=null?steamPlant.Step(dt,command,operatorPresent,shipVelocity):command;}
    public float SailPowerFactor {get{if(steamPlant!=null)return steamPlant.PowerFactor;float total=ownPower*ownArea,area=ownArea;foreach(NavalShipVisuals rig in secondaryRigs){total+=rig.SailPowerFactor*rig.ownArea;area+=rig.ownArea;}return total/area;}private set{ownPower=value;}}
    public NavalSteamPlant SteamPlant {get{return steamPlant;}}
    public void Initialize(NavalHull owner, Material timber, Material trim, Material mast, Material fabric, Material dark, Material deck, Material alternatePlank,Material armorBelowWater=null,Material metalAccent=null)
    {
        hull = owner;
        if(owner.Definition.SteamPowered)ApplyIroncladPalette(armorBelowWater??dark);
        Material hullTrim=owner.Definition.SteamPowered?dark:trim;
        foreach(Material fuel in owner.Definition.SteamPowered?new Material[]{timber,trim,deck,alternatePlank}:new Material[]{timber,trim,mast,fabric,dark,deck,alternatePlank})owner.RegisterFireMaterial(fuel);
        // The central aft companionway leaves solid deck beneath both guns.
        // The hydraulic bottom forms the walkable lower deck at the profile floor.
        float opening=owner.Definition.StairWidth*.5f+.025f;
        float width=BoatDimensions.HoldWidth*.5f-opening,center=(BoatDimensions.HoldWidth*.5f+opening)*.5f;
        Deck(new Vector3(-center,.7f,0f),new Vector3(width,.25f,11.75f),deck,alternatePlank);
        Deck(new Vector3(center,.7f,0f),new Vector3(width,.25f,11.75f),deck,alternatePlank);
        // A full-width aft landing keeps the centred helm clear of the stairs.
        Deck(new Vector3(0f,0.7f,-4.4375f),new Vector3(opening*2f,0.25f,2.875f),deck,alternatePlank);
        Deck(new Vector3(0f,0.7f,3.1875f),new Vector3(opening*2f,0.25f,5.375f),deck,alternatePlank);
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
            Box(new Vector3(side*(opening+.05f),0.84f,-1.25f),new Vector3(0.1f,0.03f,3.5f),trim,false);
            if(owner.Definition.DeckCount<3)Box(new Vector3(side*2.80f,0.86f,0f),new Vector3(0.12f,0.07f,11.75f),trim,true);
            if(owner.Definition.DeckCount<3)Box(new Vector3(side*2.80f,hull.Floor+0.08f,0f),new Vector3(0.12f,0.16f,11.75f),trim,true);
            // Steam guns stand above an unbroken armoured bulwark.  Their
            // traversing shields are built with the gun, never as window frames.
            if(!hull.Definition.RifledWeapons)for(int gun=0;gun<hull.Definition.GunCount;gun++)
            {
                if(NavalShipDefinition.GunSide(gun)!=side)continue;
                float gunZ=NavalShipDefinition.GunZ(hull.Definition,gun);
                float frameOffset=hull.Definition.RifledWeapons?.82f:1.3f;
                for(int end=-1;end<=1;end+=2)
                {
                    if(hull.Definition.DeckCount<3)Box(new Vector3(side*3.02f,1.53f,gunZ+end*frameOffset),new Vector3(.36f,1.2f,.14f),trim,true);
                    else
                    {
                        float bottom=hull.Definition.GunPortSill,top=hull.Definition.GunPortTop;
                        RawBox(new Vector3(side*3.02f,(bottom+top)*.5f,gunZ+end*frameOffset),new Vector3(.36f,top-bottom+.14f,.14f),hullTrim,true);
                    }
                }
                if(hull.Definition.DeckCount>2)RawBox(new Vector3(side*3.02f,hull.Definition.GunPortTop+.06f,gunZ),new Vector3(.36f,.12f,hull.Definition.RifledWeapons?1.65f:2.4f),hullTrim,true);
                if(hull.Definition.DeckCount<3)Box(new Vector3(side*3.02f,BoatDimensions.CannonPortSill-.05f,gunZ),new Vector3(.36f,.1f,2.4f),trim,true);
                else RawBox(new Vector3(side*3.02f,hull.Definition.GunPortSill-.05f,gunZ),new Vector3(.36f,.1f,2.4f),hullTrim,true);
            }
        }
        // Lower-deck frames and stern windows give the taller hull a sloop silhouette.
        if(!owner.Definition.SteamPowered)for(int frame=0;frame<5;frame++)for(int side=-1;side<=1;side+=2)
            Box(new Vector3(side*2.78f,(hull.Floor+.575f)*.5f,-4.5f+frame*2.2f),new Vector3(0.18f,.575f-hull.Floor,0.18f),trim,true);
        if(owner.Definition.DeckCount<3)for(int window=0;window<3;window++)
            Box(new Vector3(-1.5f+window*1.5f,hull.Definition.HullY(-.65f),-6.14f),new Vector3(0.75f,0.7f,0.08f),dark,false);
        else if(owner.Definition.SteamPowered)BuildIroncladDetails(mast,metalAccent??trim,dark);
        else BuildGalleonDetails(trim,dark);
        if(!owner.Definition.SteamPowered)
        {
            for(int i=0;i<3;i++) ForeBox(new Vector3(0f,0.65f+i*0.12f,6.1f+i*0.45f),new Vector3(4.2f-i*1.1f,0.45f,0.6f),timber);
            ForeBox(new Vector3(0f,1.25f,7.4f),new Vector3(0.23f,0.23f,3.2f),trim);
        }
        if(!owner.Definition.SteamPowered)
        {
            RailBox(new Vector3(0f,1.78f,-6f),new Vector3(6.2f,0.25f,0.35f),trim,true);
            for(int side=-1;side<=1;side+=2)
            {
                RailBox(new Vector3(side*3f,1.78f,0f),new Vector3(0.32f,0.22f,12.1f),trim,true);
                if(owner.Definition.DeckCount<3)Box(new Vector3(side*3.135f,0.05f,0f),new Vector3(0.025f,0.1f,11.8f),trim,false);
                for(int i=0;i<9;i++)
                    RailBox(new Vector3(side*3f,1.95f,-4.8f+i*1.2f),new Vector3(0.18f,0.3f,0.18f),trim,true);
                RailBox(new Vector3(side*3f,2.12f,0f),new Vector3(0.2f,0.16f,11.8f),trim,true);
            }
        }
        if(owner.Definition.DeckCount>2)BuildWeatherDeck(deck,alternatePlank,trim);
        if(owner.Definition.SteamPowered)
        {
            BuildIroncladDeckFittings(mast,metalAccent??trim,dark,timber);
            steamPlant=gameObject.AddComponent<NavalSteamPlant>();steamPlant.Initialize(owner,mast,dark,metalAccent??trim);
            return;
        }
        InitializeRig(owner,timber,trim,mast,fabric,owner.Definition.MastZ[0],owner.Definition.SailSize(0));
        for(int mastIndex=1;mastIndex<owner.Definition.MastZ.Length;mastIndex++)
        {
            NavalShipVisuals secondary=gameObject.AddComponent<NavalShipVisuals>();secondaryRigs.Add(secondary);
            secondary.InitializeRig(owner,timber,trim,mast,fabric,owner.Definition.MastZ[mastIndex],owner.Definition.SailSize(mastIndex));
        }
    }
    private void RawBox(Vector3 position,Vector3 size,Material material,bool solid)
    {hull.AddDecorationBox(transform,material,position,size,solid,false);}
    private void VisualBox(Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;RawBox(position,size,material,false);
        // These are fixed iron fittings, not free cargo. Keep them in the
        // attachment graph but omit attached trigger shapes; structure code
        // restores a solid collider if a component actually falls.
        for(int i=first;i<hull.Voxels.Count;i++)
        {hull.Voxels[i].Metallic=true;hull.Voxels[i].Flammable=false;hull.Voxels[i].SetNonPhysicalDecoration(true);}
    }
    private void RawDeck(Vector3 position,Vector3 size,Material first,Material second)
    {
        // The steam weather deck covered more than a thousand tiny cube
        // meshes although its planks are read from a distance. Half-metre
        // rows retain damage holes and the alternating finish at one quarter
        // of the former render geometry.
        float targetZ=hull.Definition.SteamPowered?.50f:.25f,targetX=hull.Definition.SteamPowered?.70f:.45f;
        int rows=Mathf.CeilToInt(size.z/targetZ);float dz=size.z/rows;
        for(int row=0;row<rows;row++)
        {
            float z=position.z+(row+.5f)*dz-size.z*.5f,half=hull.Definition.HullHalfWidth(z)-.125f;
            float left=Mathf.Max(position.x-size.x*.5f,-half),right=Mathf.Min(position.x+size.x*.5f,half);
            if(right<=left)continue;int count=Mathf.CeilToInt((right-left)/targetX);float width=(right-left)/count;
            for(int i=0;i<count;i++)
            {
                int firstCell=hull.Voxels.Count;
                RawBox(new Vector3(left+(i+.5f)*width,position.y,z),new Vector3(width,size.y,dz),i%2==0?first:second,true);
                // A destroyed deck cell is a real movement gap, but it must
                // also be a board-repair job. Otherwise one blast at a stair
                // landing permanently disconnects every lower-deck duty.
                for(int cell=firstCell;cell<hull.Voxels.Count;cell++)hull.Voxels[cell].InternalBarrier=true;
            }
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
        {
            float z=-5.875f+i*.25f;
            // The wale is inset into the shell with only a 5 mm reveal; it no
            // longer forms a row of blocks visibly passing through the side.
            RawBox(new Vector3(side*(hull.Definition.HullHalfWidth(z)-.045f),y,z),new Vector3(.1f,height,.25f),material,false);
        }
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
    private void ApplyIroncladPalette(Material belowWater)
    {
        foreach(NavalHullVoxel cell in hull.Voxels)if(cell.HydraulicShell&&cell.LocalCenter.y<hull.Definition.DesignWaterline-.04f)
        {
            Renderer renderer=cell.GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=belowWater;
            cell.OriginalSurface=belowWater;cell.Flammable=false;
        }
    }
    private void BuildIroncladDetails(Material iron,Material accent,Material dark)
    {
        for(int side=-1;side<=1;side+=2)
        {
            VisualSheerBand(side,hull.Definition.DesignWaterline+.02f,.18f,dark,0f);
            VisualSheerBand(side,hull.Definition.DesignWaterline+.16f,.055f,accent,0f);
            VisualSheerBand(side,hull.Definition.WeatherDeck-.20f,.12f,dark,.16f);
            VisualSheerBand(side,hull.Definition.RimY-.055f,.07f,accent,.12f);
        }
        // Low semicircular-looking foundations identify six separate open-deck
        // gun positions without cutting windows into the armoured shell.
        for(int side=-1;side<=1;side+=2)
        {
            for(int bay=0;bay<3;bay++)
            {
                float z=NavalShipDefinition.GunZ(hull.Definition,bay*2);
                float outer=side*(hull.Definition.HullHalfWidth(z)-NavalIroncladVisualLayout.GunFoundationInset);
                VisualBox(new Vector3(outer,hull.Definition.WeatherDeck+.035f,z),new Vector3(NavalIroncladVisualLayout.GunFoundationWidth,.07f,1.18f),iron);
                VisualBox(new Vector3(outer-side*.11f,hull.Definition.WeatherDeck+.085f,z),new Vector3(.38f,.05f,.92f),accent);
            }
        }
        BuildIroncladRails(iron,dark);
        BuildClosedIroncladBow(iron,accent,dark);
        BuildCaptainConningTower(iron,accent,dark);
    }
    private void BuildClosedIroncladBow(Material iron,Material accent,Material dark)
    {
        float water=hull.Definition.DesignWaterline,deck=hull.Definition.WeatherDeck;
        // Overlapping cheek plates bridge the last tapered hull cells.  They
        // follow the outline, so the entrance stays fine instead of becoming
        // a rectangular cap when seen from the side.
        for(int side=-1;side<=1;side+=2)for(int row=0;row<3;row++)
        {
            float y=water+.20f+(deck-water-.36f)*row*.5f;
            for(int i=0;i<6;i++)
            {
                float z=4.48f+i*.23f,x=side*(hull.Definition.HullHalfWidth(z)-.045f);
                VisualBox(new Vector3(x,y,z),new Vector3(.10f,.32f,.29f),iron);
            }
        }
        // A stepped, narrow stem closes the frontal line of sight at every
        // height.  Adjacent plates overlap in Y and Z to hide voxel seams.
        float bottom=water-.48f,top=deck+.38f;const int layers=8;
        for(int layer=0;layer<layers;layer++)
        {
            float t=(layer+.5f)/layers,y=Mathf.Lerp(bottom,top,t);
            float width=Mathf.Lerp(.46f,hull.Definition.HullHalfWidth(5.70f)*2f+.10f,Mathf.Sin(t*Mathf.PI));
            VisualBox(new Vector3(0,y,5.80f),new Vector3(width,(top-bottom)/layers+.055f,.20f),layer==layers-1?accent:iron);
        }
        VisualBox(new Vector3(0,deck+.05f,5.73f),new Vector3(hull.Definition.HullHalfWidth(5.62f)*2f,.12f,.48f),iron);
    }
    private void VisualSheerBand(int side,float baseY,float height,Material material,float rise)
    {
        for(int i=0;i<48;i++){
            float z=-5.875f+i*.25f,u=Mathf.Abs(z)/6f,y=baseY+rise*u*u;
            VisualBox(new Vector3(side*(hull.Definition.HullHalfWidth(z)-.035f),y,z),new Vector3(.08f,height,.25f),material);
        }
    }
    private void BuildIroncladRails(Material iron,Material dark)
    {
        float deck=hull.Definition.WeatherDeck;
        for(int side=-1;side<=1;side+=2)
        {
            // Open rail is retained aft and amidships.  It ends before the bow;
            // closely spaced stanchions there read as a fence from a distance.
            for(int i=0;i<9;i++){
                float z=-4.8f+i*.8f,x=side*(hull.Definition.HullHalfWidth(z)-.08f);
                VisualBox(new Vector3(x,deck+.23f,z),new Vector3(.055f,.46f,.055f),dark);
                float next=z+.8f,mid=(z+next)*.5f,mx=side*(hull.Definition.HullHalfWidth(mid)-.08f);
                VisualBox(new Vector3(mx,deck+.45f,mid),new Vector3(.06f,.055f,.86f),iron);
            }
            // Forward bulwark is one overlapping armoured run.  Overlap hides
            // voxel bevel seams and closes the conspicuous gaps at the stem.
            const float start=2.35f,end=5.68f,step=.28f;
            int count=Mathf.CeilToInt((end-start)/step);
            for(int i=0;i<count;i++){
                float z=Mathf.Min(end,start+(i+.5f)*step),x=side*(hull.Definition.HullHalfWidth(z)-.055f);
                VisualBox(new Vector3(x,deck+.20f,z),new Vector3(.11f,.40f,step+.08f),dark);
                VisualBox(new Vector3(x-side*.015f,deck+.42f,z),new Vector3(.13f,.055f,step+.10f),iron);
            }
        }
        VisualBox(new Vector3(0,deck+.20f,5.68f),new Vector3(hull.Definition.HullHalfWidth(5.60f)*2f,.40f,.12f),iron);
    }
    private void BuildCaptainConningTower(Material iron,Material accent,Material dark)
    {
        float deck=hull.Definition.WeatherDeck,z=NavalConningTowerLayout.CenterZ,w=NavalConningTowerLayout.HalfWidth*2f,d=NavalConningTowerLayout.HalfDepth*2f;
        float platform=NavalConningTowerLayout.PlatformRise,lowerTop=NavalConningTowerLayout.LowerWallTop;
        float slitTop=NavalConningTowerLayout.SlitTop,roofBottom=NavalConningTowerLayout.RoofBottom;
        float lowerHeight=lowerTop-platform,lowerY=deck+(lowerTop+platform)*.5f;
        float upperHeight=roofBottom-slitTop,upperY=deck+(roofBottom+slitTop)*.5f;
        float front=NavalConningTowerLayout.FrontZ,rear=NavalConningTowerLayout.RearZ;
        // A large raised armoured command position. Two forward doors flank
        // the wheel and a side door is cut into each wall, giving four real
        // passages instead of a single centre line obstructed by the wheel.
        // Keep the physical floor top exactly at PlatformY. v243 reduced the
        // rise to .12 but left a .14-thick slab, embedding both command-post
        // capsules two centimetres into NavalPrototype_HullCollisionBox.
        ArmoredTowerBox(new Vector3(0,deck+platform*.5f,z),new Vector3(w,platform,d),accent);
        for(int side=-1;side<=1;side+=2)
        {
            float wallX=side*(NavalConningTowerLayout.HalfWidth-.07f);
            float sideDoorRear=NavalConningTowerLayout.SideDoorZ-NavalConningTowerLayout.SideDoorHalfDepth;
            float sideDoorFront=NavalConningTowerLayout.SideDoorZ+NavalConningTowerLayout.SideDoorHalfDepth;
            float rearLength=sideDoorRear-rear,frontLength=front-sideDoorFront;
            ArmoredTowerBox(new Vector3(wallX,lowerY,rear+rearLength*.5f),new Vector3(.14f,lowerHeight,rearLength),iron);
            ArmoredTowerBox(new Vector3(wallX,lowerY,sideDoorFront+frontLength*.5f),new Vector3(.14f,lowerHeight,frontLength),iron);
            ArmoredTowerBox(new Vector3(wallX,upperY,z),new Vector3(.14f,upperHeight,d),dark);
            // Uprights break each side slit into separate viewing ports while
            // staying outside the full-height side doorway.
            foreach(float postZ in new[]{rear+.16f,front-.16f})
                ArmoredTowerBox(new Vector3(wallX,deck+(lowerTop+slitTop)*.5f,postZ),new Vector3(.16f,slitTop-lowerTop,.14f),iron);

            float outerWidth=NavalConningTowerLayout.HalfWidth-NavalConningTowerLayout.DoorOuterX;
            float outerX=side*(NavalConningTowerLayout.DoorOuterX+outerWidth*.5f);
            ArmoredTowerBox(new Vector3(outerX,lowerY,front-.07f),new Vector3(outerWidth,lowerHeight,.14f),iron);
            // A narrow central armoured pier separates the two forward doors.
            ArmoredTowerBox(new Vector3(side*NavalConningTowerLayout.DoorInnerX*.5f,lowerY,front-.07f),
                new Vector3(NavalConningTowerLayout.DoorInnerX,lowerHeight,.14f),iron);
            ArmoredTowerBox(new Vector3(side*(NavalConningTowerLayout.DoorOuterX+.045f),lowerY,front-.09f),new Vector3(.09f,lowerHeight,.18f),accent);
        }
        // A narrow rear observation slit is framed by solid lower armour, a
        // continuous roof-bearing upper belt and three vertical mullions. It
        // is a window only: the opening remains far above capsule foot level.
        ArmoredTowerBox(new Vector3(0,lowerY,rear+.07f),new Vector3(w,lowerHeight,.14f),iron);
        ArmoredTowerBox(new Vector3(0,upperY,rear+.07f),new Vector3(w,upperHeight,.14f),dark);
        foreach(float postX in new[]{-NavalConningTowerLayout.HalfWidth*.48f,0f,NavalConningTowerLayout.HalfWidth*.48f})
            ArmoredTowerBox(new Vector3(postX,deck+(lowerTop+slitTop)*.5f,rear+.07f),new Vector3(.14f,slitTop-lowerTop,.16f),iron);
        // The full-width forward upper belt is the visible pair of door
        // lintels and closes the old floating-roof gap.
        ArmoredTowerBox(new Vector3(0,upperY,front-.07f),new Vector3(w,upperHeight,.14f),dark);
        ArmoredTowerBox(new Vector3(0,deck+roofBottom+NavalConningTowerLayout.RoofThickness*.5f,z),
            new Vector3(w+.12f,NavalConningTowerLayout.RoofThickness,d+.12f),dark);
        // Separate low treads lead to both forward doors and both side doors.
        float frontStepWidth=NavalConningTowerLayout.DoorOuterX-NavalConningTowerLayout.DoorInnerX-.08f;
        float sideStepWidth=NavalConningTowerLayout.SideDoorHalfDepth*2f-.08f;
        for(int step=0;step<NavalConningTowerLayout.StairCount;step++)
        {
            float height=NavalConningTowerLayout.StairRise*(step+1);
            for(int side=-1;side<=1;side+=2)
            {
                ArmoredTowerBox(new Vector3(side*NavalConningTowerLayout.EntryX,deck+height*.5f,NavalConningTowerLayout.StairZ(step)),
                    new Vector3(frontStepWidth,height,NavalConningTowerLayout.StairDepth),step==NavalConningTowerLayout.StairCount-1?accent:iron);
                float stepX=side*(NavalConningTowerLayout.HalfWidth+NavalConningTowerLayout.FirstStairOffset-step*NavalConningTowerLayout.StairSpacing);
                ArmoredTowerBox(new Vector3(stepX,deck+height*.5f,NavalConningTowerLayout.SideDoorZ),
                    new Vector3(NavalConningTowerLayout.StairDepth,height,sideStepWidth),step==NavalConningTowerLayout.StairCount-1?accent:iron);
            }
        }
    }
    private void ArmoredTowerBox(Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;RawBox(position,size,material,true);
        for(int i=first;i<hull.Voxels.Count;i++)
        {var cell=hull.Voxels[i];cell.Metallic=true;cell.Flammable=false;cell.Armored=true;cell.InternalBarrier=true;}
    }
    private void BuildIroncladDeckFittings(Material iron,Material accent,Material dark,Material timber)
    {
        float roof=hull.Definition.WeatherDeck;
        // Short signal staff only: no sail cloth, yards or active sailing rig.
        VisualBox(new Vector3(0,roof+.68f,-3.45f),new Vector3(.11f,1.36f,.11f),dark);
        VisualBox(new Vector3(0,roof+1.02f,-3.45f),new Vector3(.72f,.07f,.07f),accent);
        // The former paired steel ventilator boxes occupied the main deck and
        // visually read as loose cargo. Ventilation is integrated into the
        // funnel uptake instead.
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
            float top=hull.Definition.UpperStairFloor+(.825f-hull.Definition.UpperStairFloor)*(step+1)/NavalWalkRoutes.StepCount+offset;
            float run=3.5f/NavalWalkRoutes.StepCount,z=-3+(step+.5f)*run;
            RawBox(new Vector3(0,top-.0625f,z),new Vector3(hull.Definition.StairWidth,.125f,run),step%2==0?deck:alternate,true);
            RawBox(new Vector3(0,top-.012f,z-run*.5f+.02f),new Vector3(hull.Definition.StairWidth,.024f,.04f),trim,true);
        }
        for(int side=-1;side<=1;side+=2)
        {
            RawBox(new Vector3(side*(opening+.05f),y+.015f,-1.25f),new Vector3(.1f,.03f,3.5f),trim,false);
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
        if(steamPlant!=null){steamPlant.Restore();return;}
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
            if(!poseClock.Due(Time.time,deployed,distant,Time.frameCount,NavalPerformance.LaptopMode?2:1))return;
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

public static class NavalIroncladVisualLayout
{
    public const float GunFoundationInset=.38f,GunFoundationWidth=.58f;
    public static bool GunFoundationInside(NavalShipDefinition ship,float z)
    {return ship.HullHalfWidth(z)-GunFoundationInset+GunFoundationWidth*.5f<=ship.HullHalfWidth(z)+.001f;}
}
