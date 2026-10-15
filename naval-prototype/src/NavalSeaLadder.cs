using System.Collections.Generic;
using UnityEngine;
public class NavalSeaLadder : MonoBehaviour
{
    public static readonly List<NavalSeaLadder> All=new List<NavalSeaLadder>();
    private readonly List<NavalHullVoxel>[] cells={new List<NavalHullVoxel>(),new List<NavalHullVoxel>()};
    public NavalHull Hull;
    public void Initialize(NavalHull hull,Material material)
    {
        Hull=hull;All.Add(this);
        for(int side=0;side<2;side++)
        {
            float x=side==0?-3.35f:3.35f;int first=hull.Voxels.Count;
            for(int rail=-1;rail<=1;rail+=2)
                hull.AddDecorationBox(transform,material,new Vector3(side==0?-3.1f:3.1f,0.6f,3.8f+rail*0.37f),new Vector3(0.65f,0.16f,0.16f),true);
            for(int rail=-1;rail<=1;rail+=2)
                hull.AddDecorationBox(transform,material,new Vector3(x,0.45f,3.8f+rail*0.37f),new Vector3(0.14f,4.1f,0.14f),true);
            for(int step=0;step<14;step++)
                hull.AddDecorationBox(transform,material,new Vector3(x,-1.5f+step*0.3f,3.8f),new Vector3(0.18f,0.12f,0.75f),true);
            for(int i=first;i<hull.Voxels.Count;i++)cells[side].Add(hull.Voxels[i]);
        }
    }
    public bool Usable(int side)
    {
        if(Hull.Sunk)return false;int intact=0;
        foreach(NavalHullVoxel cell in cells[side])if(cell!=null&&!cell.Missing&&!cell.Detached)intact++;
        return intact>=cells[side].Count*0.75f;
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
                float x=i==0?-3.35f:3.35f;
                if(!ladder.Usable(i)||Mathf.Abs(local.x)<3.05f||local.y>2.6f||local.y<-2.5f)continue;
                float distance=Vector2.Distance(new Vector2(local.x,local.z),new Vector2(x,3.8f))*BoatDimensions.WorldScale;
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
        Vector3 target=stage==0?new Vector3(sign*3.7f,feet.y,3.8f):stage==1?
            new Vector3(sign*3.7f,2.55f,3.8f):stage==2?new Vector3(sign*2.2f,2.55f,3.8f):new Vector3(sign*2.2f,0.86f,3.8f);
        feet=Vector3.MoveTowards(feet,target,1.5f*dt);
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
