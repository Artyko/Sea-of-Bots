using System;
using UnityEngine;
class CapitalAADeckChecks
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Main()
    {
        var ship=NavalShipDefinition.Dreadnought;
        Vector3 bottom=NavalCapitalAADeck.Bottom(ship),top=NavalCapitalAADeck.Landing(ship);
        Check(NavalCapitalAADeck.Ladder(bottom,ship)&&NavalCapitalAADeck.Ladder(top,ship),"Both ends identify the same ladder");
        var basePoint=new NavalWalkPoint(bottom.x,bottom.y,bottom.z);
        var post=new NavalWalkPoint(-1.32f,NavalCapitalAADeck.Top(ship),NavalCapitalAADeck.Z);
        var up=NavalSupplyRoutes.Plan(basePoint,post,ship);
        var down=NavalSupplyRoutes.Plan(post,basePoint,ship);
        for(int side=-1;side<=1;side+=2){
            Vector3 foot=NavalCapitalAADeck.Bottom(ship,side),rungs=NavalCapitalAADeck.Rungs(ship,side);
            float clearance=(foot.z-rungs.z)*ship.Scale-.05f;
            Check(clearance>.8f+.1f,"Full-size capsule clears ladder rungs at the approach");
            Check(NavalCapitalAADeck.Waypoint(foot,ship),"Base is a precise ladder waypoint");
            Check(!NavalCapitalAADeck.Reached(foot+Vector3.right*(.7f/ship.Scale),foot,ship.Scale),"Do not skip base from beside the ladder");
            Check(!NavalCapitalAADeck.Reached(foot+Vector3.up*(.5f/ship.Scale),foot,ship.Scale),"Do not skip landing half a metre early");
            Check(NavalCapitalAADeck.Reached(foot+Vector3.up*(.06f/ship.Scale),foot,ship.Scale),"Controller skin offset does not block arrival");
            Vector3 landing=NavalCapitalAADeck.Landing(ship,side),crest=NavalCapitalAADeck.Crest(ship,side);
            Check(NavalCapitalAADeck.Ladder(landing,ship)&&NavalCapitalAADeck.Ladder(crest,ship),"Mounting and dismounting both stay in ladder transfer zone");
            Check(Math.Abs(rungs.z-NavalCapitalAADeck.Z-.34f)<.001f,"Rungs flush with circular rim");
            Check(landing.z<rungs.z&&crest.z>rungs.z,"Separate inside landing and outside climbing axis");
            Check(Math.Abs((landing.z-NavalCapitalAADeck.Z)*ship.Scale)+.8f<.34f*ship.Scale,"Landing supports the full capsule footprint");
        }
        bool upCrest=false,downCrest=false;
        foreach(var p in up)if(Math.Abs(p.Z-NavalCapitalAADeck.Crest(ship,-1).z)<.001f&&Math.Abs(p.Y-NavalCapitalAADeck.Top(ship))<.001f)upCrest=true;
        foreach(var p in down)if(Math.Abs(p.Z-NavalCapitalAADeck.Crest(ship,-1).z)<.001f&&Math.Abs(p.Y-NavalCapitalAADeck.Top(ship))<.001f)downCrest=true;
        Check(upCrest&&downCrest,"Both directions traverse rim before changing height");
        Check(Math.Abs(ship.GunOperator(6).x)>NavalRaisedBridge.HalfWidth(ship)+.8f/ship.Scale,"Forward gun post clears bridge wall");
        Check(up.Count>0&&down.Count>0,"Round trip from a raised AA post");
        var rightPost=new NavalWalkPoint(1.30f,NavalCapitalAADeck.Top(ship),NavalCapitalAADeck.Z);
        var crossing=NavalSupplyRoutes.Plan(post,rightPost,ship);
        bool descended=false;
        foreach(var point in crossing)if(point.Y==ship.WeatherDeck)descended=true;
        Check(descended,"Separate AA nests connect via deck rather than an invisible bridge");
        Check(NavalCapitalAADeck.Bottom(ship,-1).x<0f,"Left nest has its own ladder");
        Check(up[up.Count-1].Y==post.Y&&down[down.Count-1].Y==basePoint.Y,"Routes terminate on the requested level");
        Check(NavalCrewDeckSafety.Inside(bottom.x,bottom.z,ship,.8f),"Bottom landing stays within hull");
        Check(NavalRaisedBridge.StairWidth>2.5f,"Wider command stair opening");
        float gap=Math.Abs(NavalCapitalLayout.Mount(3).z-NavalCapitalLayout.Mount(4).z);
        Check(gap<1.25f&&gap>1f,"Aft turret spacing reduced without merging barbettes");
        Console.WriteLine("PASS: raised AA platform return routes, vertical access endpoints, hull margin and revised aft spacing.");
    }
}
