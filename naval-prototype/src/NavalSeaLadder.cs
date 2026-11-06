using System.Collections.Generic;
using UnityEngine;
public class NavalSeaLadder : MonoBehaviour
{
    public static readonly List<NavalSeaLadder> All=new List<NavalSeaLadder>();
    private readonly List<NavalHullVoxel>[] cells={new List<NavalHullVoxel>(),new List<NavalHullVoxel>()};
    public NavalHull Hull;
    public float OuterX(int side){return (side==0?-1f:1f)*(Hull.Definition.HullHalfWidth(Hull.Definition.LadderZ)+StandOff/Hull.WorldScale);}
    private float StandOff {get{return Hull.Definition.CapitalShip?1.05f:.65f;}}
    public float DeckX(int side){return (side==0?-1f:1f)*(Hull.Definition.HullHalfWidth(Hull.Definition.LadderZ)-1.15f/Hull.WorldScale);}
    private int usableRevision=-1;
    private readonly bool[] usable=new bool[2];
    public void Initialize(NavalHull hull,Material material)
    {
        Hull=hull;All.Add(this);
        var ship=hull.Definition;
        float scale=hull.WorldScale;
        float bottom=Mathf.Min(ship.DesignWaterline-.85f/scale,ship.WeatherDeck-1.4f/scale);
        float top=ship.BoardingY;
        float height=(top-bottom)*scale;
        int steps=Mathf.CeilToInt(height/.30f);
        for(int side=0;side<2;side++)
        {
            int first=hull.Voxels.Count;
            float x=OuterX(side),sign=side==0?-1f:1f;
            for(int rail=-1;rail<=1;rail+=2)
            {
                float z=ship.LadderZ+rail*.48f/scale;
                hull.AddDecorationBox(transform,material,new Vector3(x,(bottom+top)*.5f,z),
                    new Vector3(.12f,height,.12f)/scale,true);
                hull.AddDecorationBox(transform,material,new Vector3(x-sign*(StandOff-.25f)/scale,ship.WeatherDeck-.22f/scale,z),
                    new Vector3(.95f+(StandOff-.65f)*2f,.16f,.16f)/scale,true);
            }
            for(int rung=0;rung<=steps;rung++)
                hull.AddDecorationBox(transform,material,new Vector3(x,Mathf.Lerp(bottom,top,rung/(float)steps),ship.LadderZ),
                    new Vector3(.18f,.12f,1.02f)/scale,true);
            for(int i=first;i<hull.Voxels.Count;i++)cells[side].Add(hull.Voxels[i]);
        }
    }
    public bool Usable(int side)
    {
        if(Hull.Sunk||side<0||side>=2)return false;
        if(usableRevision!=Hull.Revision)
        {
            for(int index=0;index<2;index++)
            {
                int intact=0;
                foreach(NavalHullVoxel cell in cells[index])if(cell!=null&&!cell.Missing&&!cell.Detached)intact++;
                usable[index]=intact>=cells[index].Count*.75f;
            }
            usableRevision=Hull.Revision;
        }
        return usable[side];
    }
    private void OnDestroy(){All.Remove(this);}
}

public class NavalBoarding
{
    private readonly FirstPersonMover player;
    private readonly float footOffset;
    private NavalSeaLadder selected,nearby;
    private int side,nearSide,stage;
    private Vector3 feet;
    public bool Climbing {get{return selected!=null;}}
    public NavalBoarding(FirstPersonMover owner,float offset){player=owner;footOffset=offset;}
    public void Cancel(){selected=null;nearby=null;}
    public void Tick(float dt,bool operating)
    {
        if(Climbing)
        {
            if(!selected.Usable(side)||Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Space))Cancel();
            return;
        }
        nearby=null;if(operating)return;
        float nearest=3f;
        foreach(NavalSeaLadder ladder in NavalSeaLadder.All)
        {
            if(ladder==null)continue;
            Vector3 local=ladder.transform.InverseTransformPoint(player.transform.position);
            for(int i=0;i<2;i++)
            {
                float x=ladder.OuterX(i),edge=ladder.Hull.Definition.HullHalfWidth(ladder.Hull.Definition.LadderZ);
                if(!ladder.Usable(i)||Mathf.Abs(local.x)<edge+.05f||local.y>2.6f+ladder.Hull.Definition.UpperDeckOffset||local.y<-2.5f+ladder.Hull.Definition.UpperDeckOffset)continue;
                float distance=Vector2.Distance(new Vector2(local.x,local.z),new Vector2(x,ladder.Hull.Definition.LadderZ))*ladder.Hull.WorldScale;
                if(distance<nearest){nearby=ladder;nearSide=i;nearest=distance;}
            }
        }
        if(nearby!=null&&Input.GetKeyDown(KeyCode.E))
        {selected=nearby;side=nearSide;stage=0;feet=selected.transform.InverseTransformPoint(player.transform.position-selected.transform.up*footOffset);}
    }
    public void Apply(float dt)
    {
        if(!Climbing)return;
        float sign=side==0?-1f:1f;
        float outside=Mathf.Abs(selected.OuterX(side))+.35f/selected.Hull.WorldScale,deck=Mathf.Abs(selected.DeckX(side));
        Vector3 target=stage==0?new Vector3(sign*outside,feet.y,selected.Hull.Definition.LadderZ):stage==1?
            new Vector3(sign*outside,selected.Hull.Definition.BoardingY,selected.Hull.Definition.LadderZ):stage==2?new Vector3(sign*deck,selected.Hull.Definition.BoardingY,selected.Hull.Definition.LadderZ):new Vector3(sign*deck,selected.Hull.Definition.WeatherDeck+.035f,selected.Hull.Definition.LadderZ);
        feet=Vector3.MoveTowards(feet,target,3.8f*dt/selected.Hull.WorldScale);
        CharacterController controller=player.GetComponentInChildren<CharacterController>();
        bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
        player.transform.position=selected.transform.TransformPoint(feet)+selected.transform.up*footOffset;
        if(enabled)controller.enabled=true;
        if((feet-target).sqrMagnitude<0.0001f)
        {
            if(stage==3){Cancel();return;}
            stage++;
        }
    }
    public void Draw()
    {
        if(Climbing||nearby!=null)GUI.Box(new Rect(Screen.width*0.5f-210f,Screen.height-125f,420f,30f),
            Climbing?"Climbing aboard | E / Space: release ladder":"E: climb aboard");
    }
}
