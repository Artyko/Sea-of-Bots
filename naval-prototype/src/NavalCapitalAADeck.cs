using System.Collections.Generic;
using UnityEngine;

// Shared geometry and endpoints for the aft AA platform.
public static class NavalCapitalAADeck
{
    public const float Z=-3.06f,HalfDepth=.34f,HalfWidth=1.65f,LadderX=1.30f;
    public static float Top(NavalShipDefinition ship){return ship.WeatherDeck+1.48f;}
    // The body travels in front of the rungs, never along their collider centres.
    public static Vector3 Rungs(NavalShipDefinition ship,int side=1){return new Vector3(side*LadderX,ship.WeatherDeck,Z+.34f);}
    public static Vector3 Bottom(NavalShipDefinition ship,int side=1){return Rungs(ship,side)+Vector3.forward*(1.26f/ship.Scale);}
    public static bool Waypoint(Vector3 point,NavalShipDefinition ship)
    {
        if(!ship.CapitalShip)return false;
        Vector3 bottom=Bottom(ship,point.x<0?-1:1),top=Landing(ship,point.x<0?-1:1),crest=Crest(ship,point.x<0?-1:1);
        return (point-bottom).sqrMagnitude*ship.Scale*ship.Scale<.01f||
            (point-top).sqrMagnitude*ship.Scale*ship.Scale<.01f||(point-crest).sqrMagnitude*ship.Scale*ship.Scale<.01f;
    }
    public static bool Reached(Vector3 feet,Vector3 point,float scale)
    {Vector3 d=(feet-point)*scale;return new Vector2(d.x,d.z).sqrMagnitude<.18f*.18f&&Mathf.Abs(d.y)<.12f;}
    public static Vector3 Crest(NavalShipDefinition ship,int side=1){Vector3 point=Bottom(ship,side);point.y=Top(ship);return point;}
    public static Vector3 Landing(NavalShipDefinition ship,int side=1){return new Vector3(side*LadderX,Top(ship),Z+.16f);}
    public static bool Upper(NavalWalkPoint p,NavalShipDefinition ship)
    {return ship.CapitalShip&&p.Y>=Top(ship)-.12f/ship.Scale&&Mathf.Abs(p.Z-Z)<1.2f&&Mathf.Abs(p.X)<HalfWidth;}
    public static bool Ladder(Vector3 p,NavalShipDefinition ship)
    {
        if(!ship.CapitalShip)return false;
        Vector3 basePoint=Bottom(ship,p.x<0?-1:1);
        return Mathf.Abs(p.x-basePoint.x)*ship.Scale<.8f&&(Mathf.Abs(p.z-basePoint.z)*ship.Scale<.8f||Mathf.Abs(p.y-Top(ship))*ship.Scale<.2f&&p.z>=Z+.12f&&p.z<=basePoint.z+.1f)&&
            p.y>=ship.WeatherDeck-.1f&&p.y<=Top(ship)+.15f;
    }
    public static bool TryRoute(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship,out List<NavalWalkPoint> route)
    {
        route=null;
        bool from=Upper(start,ship)||start.Y>ship.WeatherDeck+.2f&&Ladder(new Vector3(start.X,start.Y,start.Z),ship),to=Upper(goal,ship);
        if(!from&&!to)return false;
        route=new List<NavalWalkPoint>();
        int side=(to?goal.X:start.X)<0?-1:1;
        Vector3 bottom=Bottom(ship,side),top=Landing(ship,side),crest=Crest(ship,side);
        if(from&&to)
        {
            if(start.X*goal.X>=0f){route.Add(goal);return true;}
            Vector3 otherBottom=Bottom(ship,start.X<0?-1:1),otherTop=Landing(ship,start.X<0?-1:1);
            route.Add(new NavalWalkPoint(otherTop.x,otherTop.y,otherTop.z));
            var otherCrest=Crest(ship,start.X<0?-1:1);
            route.Add(new NavalWalkPoint(otherCrest.x,otherCrest.y,otherCrest.z));
            route.Add(new NavalWalkPoint(otherBottom.x,otherBottom.y,otherBottom.z));
            route.AddRange(NavalSupplyRoutes.Plan(new NavalWalkPoint(otherBottom.x,otherBottom.y,otherBottom.z),
                new NavalWalkPoint(bottom.x,bottom.y,bottom.z),ship));
            route.Add(new NavalWalkPoint(crest.x,crest.y,crest.z));route.Add(new NavalWalkPoint(top.x,top.y,top.z));route.Add(goal);return true;
        }
        var a=new NavalWalkPoint(bottom.x,bottom.y,bottom.z);
        var b=new NavalWalkPoint(top.x,top.y,top.z);
        if(from){route.Add(b);route.Add(new NavalWalkPoint(crest.x,crest.y,crest.z));route.Add(a);route.AddRange(NavalSupplyRoutes.Plan(a,goal,ship));}
        else {route.AddRange(NavalSupplyRoutes.Plan(start,a,ship));route.Add(new NavalWalkPoint(crest.x,crest.y,crest.z));route.Add(b);route.Add(goal);}
        return true;
    }
}

public partial class NavalShipVisuals
{
    private void BuildAftAADeck(Material iron,Material accent)
    {
        var ship=hull.Definition;
        float y=NavalCapitalAADeck.Top(ship),z=NavalCapitalAADeck.Z,q=ship.Scale;
        for(int side=-1;side<=1;side+=2)
        {
            float center=side*1.30f,radius=.34f;
            // Narrow strips form a round floor with a clear, independent ladder lane.
            for(int row=0;row<8;row++)
            {
                float dz=-radius+(row+.5f)*radius*.25f;
                float half=Mathf.Sqrt(Mathf.Max(0f,radius*radius-dz*dz));
                ArmoredTowerBox(new Vector3(center,y-.035f,z+dz),new Vector3(half*2f,.07f,radius*.25f+.001f),iron);
            }
            for(int post=-1;post<=1;post+=2)
                ArmoredTowerBox(new Vector3(center,(ship.WeatherDeck+y)*.5f,z+post*.20f),
                    new Vector3(.06f,y-ship.WeatherDeck,.08f),iron);
            for(int segment=0;segment<14;segment++)
            {
                float angle=(segment*24f+24f)*Mathf.Deg2Rad;
                if(Mathf.Cos(angle)>.65f)continue;
                var point=new Vector3(center+Mathf.Sin(angle)*radius,y+.45f/q,z+Mathf.Cos(angle)*radius);
                ArmoredTowerBox(point,new Vector3(.14f,.9f/q,.10f),accent);
            }
            var bottom=NavalCapitalAADeck.Rungs(ship,side);
            // Ladder is flush with the circular rim; there is no projecting bridge.
            for(int rail=-1;rail<=1;rail+=2)
                ArmoredTowerBox(new Vector3(bottom.x+rail*.30f/q,(ship.WeatherDeck+y)*.5f,bottom.z),new Vector3(.08f/q,y-ship.WeatherDeck,.08f/q),iron);
            int rungs=Mathf.CeilToInt((y-ship.WeatherDeck)*q/.3f);
            for(int rung=0;rung<=rungs;rung++)
                ArmoredTowerBox(new Vector3(bottom.x,Mathf.Lerp(ship.WeatherDeck,y,rung/(float)rungs),bottom.z),new Vector3(.68f,.08f,.1f)/q,accent);
        }
    }
}

public partial class NavalRepairSailor
{
    private bool ClimbAftAALadder(Vector3 goal,float dt)
    {
        if(!NavalCapitalAADeck.Ladder(feet,hull.Definition)||!NavalCapitalAADeck.Ladder(goal,hull.Definition))return false;
        Vector3 delta=boat.TransformVector(goal-feet);
        bool transfer=Mathf.Abs(feet.y-NavalCapitalAADeck.Top(hull.Definition))*hull.WorldScale<.2f&&Mathf.Abs(goal.y-feet.y)*hull.WorldScale<.2f;
        if(!transfer&&(new Vector2(delta.x,delta.z).sqrMagnitude>.8f*.8f||Mathf.Abs(delta.y)<.04f))return false;
        feet=Vector3.MoveTowards(feet,goal,2.4f*dt/hull.WorldScale);
        HoldPosition();return true;
    }
}

public partial class NavalTestWorld
{
    private bool climbingCapitalLadder;
    private Vector3 capitalLadderGoal,capitalLadderFeet,capitalLadderFinal;
    private bool capitalLadderTransfer;
    private bool TickCapitalLadder(float dt)
    {
        if(hull==null||!hull.Definition.CapitalShip||Spectating||swimming)return false;
        if(climbingCapitalLadder)
        {
            if(hull.Sunk||Input.GetKeyDown(KeyCode.E)){climbingCapitalLadder=false;return true;}
            capitalLadderFeet=Vector3.MoveTowards(capitalLadderFeet,capitalLadderGoal,2.4f*dt/hull.WorldScale);
            var body=player.GetComponentInChildren<CharacterController>();
            bool enabled=body!=null&&body.enabled;if(enabled)body.enabled=false;
            player.transform.position=boat.TransformPoint(capitalLadderFeet)+Vector3.up*footOffset;
            if(enabled)body.enabled=true;
            if((capitalLadderFeet-capitalLadderGoal).sqrMagnitude<.00001f){if(capitalLadderTransfer){capitalLadderGoal=capitalLadderFinal;capitalLadderTransfer=false;}else climbingCapitalLadder=false;}
            return true;
        }
        if(!Input.GetKeyDown(KeyCode.E)||stations.Operating)return false;
        Vector3 foot=boat.InverseTransformPoint(player.transform.position-Vector3.up*footOffset);
        var bottom=NavalCapitalAADeck.Bottom(hull.Definition);
        var top=NavalCapitalAADeck.Landing(hull.Definition);
        Vector3 turretBase=NavalTurretAccessLayout.Approach(hull.Definition,3);
        Vector3 turretTop=NavalTurretAccessLayout.ToShip(hull.Definition,3,
            new Vector3((NavalTurretAccessLayout.Door(3).x+.15f),NavalTurretAccessLayout.Floor,0));
        Vector3[] ends={bottom,top,NavalCapitalAADeck.Bottom(hull.Definition,-1),NavalCapitalAADeck.Landing(hull.Definition,-1),turretBase,turretTop};
        float nearest=1.4f*1.4f;int selected=-1;
        for(int i=0;i<ends.Length;i++)
        {
            float d=boat.TransformVector(foot-ends[i]).sqrMagnitude;
            if(d<nearest){nearest=d;selected=i;}
        }
        if(selected<0)return false;
        capitalLadderFeet=foot;capitalLadderFinal=ends[selected^1];capitalLadderTransfer=selected<4;capitalLadderGoal=capitalLadderTransfer?NavalCapitalAADeck.Crest(hull.Definition,selected<2?1:-1):capitalLadderFinal;climbingCapitalLadder=true;
        return true;
    }
}
