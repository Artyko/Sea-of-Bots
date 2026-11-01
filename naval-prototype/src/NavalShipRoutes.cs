using System;
using System.Collections.Generic;
// Deck transitions share the same stair geometry. Two-deck profiles retain the original route exactly.
public static class NavalShipRoutes
{
    // v262's x=1.40 lane still left only a few centimetres between the
    // occupied gun post and the casemate. A 1.50 centre line gives two
    // full-size friendly capsules room while retaining 2.2 m to the mount.
    public const float SteamBatteryLane=1.50f;
    // At the aft end of the upper stair the x=1.50/z=-3.90 point was exactly
    // tangent to both the open-flight coaming and the aft casemate clearance.
    // Native controller skin stopped several returning sailors at x=1.16.
    // Move the gateway aft and slightly outboard into the real gap before the
    // conning tower; ordinary battery traffic keeps the established lane.
    public const float SteamAftGatewayLane=1.58f,SteamAftGatewayZ=-4.05f;
    public static bool InStairwell(NavalWalkPoint p,NavalShipDefinition ship)
    {
        if(NavalWalkRoutes.InStairwell(p.X,p.Y,p.Z,ship.StairWidth*.5f+.175f,ship.HoldFloor))return true;
        return ship.DeckCount>2&&NavalWalkRoutes.InStairwell(p.X,p.Y-ship.UpperDeckOffset,p.Z,ship.StairWidth*.5f+.175f,ship.UpperStairFloor);
    }
    private static void Shift(List<NavalWalkPoint> route,List<NavalWalkPoint> points,float offset)
    {foreach(var p in points)route.Add(new NavalWalkPoint(p.X,p.Y+offset,p.Z));}
    private static void UpperPlan(List<NavalWalkPoint> route,NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
    {
        float offset=ship.UpperDeckOffset;start.Y-=offset;goal.Y-=offset;
        Shift(route,NavalBallastRoutes.Plan(start,goal,false,ship.StairWidth,ship.UpperStairFloor,ship),offset);
    }
    public static List<NavalWalkPoint> Plan(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
    {
        var route=NavalStairTraffic.Apply(start,PlanCore(start,goal,ship),ship);
        if(ship.SteamPowered){route=AvoidSteamUnderFlight(start,route,ship);route=AvoidSteamUpperStair(start,route,ship);route=AvoidSteamDeckEquipment(start,route,ship);route=AvoidSteamCasemates(start,route,ship);route=NavalConningTowerLayout.Route(start,route,ship);route=AvoidSteamUpperStair(start,route,ship);bool sameWeather=route.Count>0&&start.Y>ship.WeatherDeck-.12f&&route[route.Count-1].Y>ship.WeatherDeck-.12f;for(int i=0;i<route.Count-1;i++){
            var point=route[i];float limit=System.Math.Max(.15f,ship.HullHalfWidth(point.Z)-.85f/ship.Scale);
            // The weather-deck battery and its ready-use ammunition occupy the
            // outer lane.  Route full-size robot bodies through the clear inner
            // lane, then approach only the final work post from there.
            // A level weather-deck trip must skirt the open upper stairwell;
            // otherwise its centre-line waypoint is interpreted as stair
            // traffic and can send the engineer into the battery below.
            bool weather=point.Y>ship.WeatherDeck-.12f;
            bool bowMount=weather&&point.Z>=3.05f&&point.Z<=4.65f;
            bool passage=sameWeather&&weather&&point.Z>=-3.85f&&point.Z<=1.05f;
            bool stairBypass=weather&&point.Z>=SteamAftGatewayZ-.01f&&point.Z<=.80f&&System.Math.Abs(point.X)>=1.20f;
            bool conningDoor=NavalConningTowerLayout.DoorLane(point,ship);
            if(conningDoor){}
            else if(bowMount)
            {
                float side=System.Math.Abs(point.X)>.01f?System.Math.Sign(point.X):System.Math.Abs(start.X)>.01f?System.Math.Sign(start.X):1f;
                // Keep an already safe outer lane. The generic .40 clamp
                // pulled x=1.40 back through the main-battery shield.
                point.X=side*System.Math.Min(limit,System.Math.Max(1.35f,System.Math.Abs(point.X)));
            }
            else if(stairBypass){}
            else if(passage&&System.Math.Abs(point.X)<SteamBatteryLane)
            {float side=System.Math.Abs(point.X)>.01f?System.Math.Sign(point.X):System.Math.Abs(start.X)>.01f?System.Math.Sign(start.X):1f;point.X=side*System.Math.Min(SteamBatteryLane,limit);}
            else
            {
                if(point.Y>ship.WeatherDeck-.12f)limit=System.Math.Min(limit,point.Z>=1.15f&&point.Z<=2.35f?.56f:.40f);
                point.X=System.Math.Max(-limit,System.Math.Min(limit,point.X));
            }
            route[i]=point;
        }}
        return route;
    }
    private static List<NavalWalkPoint> AvoidSteamUnderFlight(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship)
    {
        // A middle-deck horizontal route must not use the low space beneath
        // the upper flight. v243 repeatedly recovered sailors standing around
        // (0.60, .85, .12). Route them to a full-height side lane up front.
        if(route.Count==0||ship.DeckCount<3)return route;
        var result=new List<NavalWalkPoint>(route.Count+4);NavalWalkPoint previous=start;
        float floor=.825f,half=ship.StairWidth*.5f+.5f/ship.Scale,lane=NavalStairRecovery.SideLane(ship);
        const float aft=-3.15f,forward=.65f;
        foreach(NavalWalkPoint point in route)
        {
            bool stairTransition=NavalStairTraffic.InPassage(previous,ship)||NavalStairTraffic.InPassage(point,ship);
            bool horizontal=!stairTransition&&Math.Abs(previous.Y-floor)<.12f&&Math.Abs(point.Y-floor)<.12f&&Math.Abs(previous.Y-point.Y)<.04f;
            bool previousInside=Math.Abs(previous.X)<half&&previous.Z>aft&&previous.Z<forward;
            bool pointInside=Math.Abs(point.X)<half&&point.Z>aft&&point.Z<forward;
            bool crosses=horizontal&&NavalSupplyRoutes.Hits(previous,point,-half,half,aft,forward);
            if(horizontal&&(crosses||previousInside||pointInside))
            {
                float side=Math.Abs(previous.X)>.05f?Math.Sign(previous.X):Math.Abs(point.X)>.05f?Math.Sign(point.X):1f;
                float x=side*lane;
                if(previousInside)result.Add(new NavalWalkPoint(x,floor,previous.Z));
                else if(crosses)
                {
                    float entry=previous.Z<(aft+forward)*.5f?aft:forward;
                    result.Add(new NavalWalkPoint(x,floor,entry));
                }
                if(!pointInside&&crosses)
                {
                    float exit=point.Z<(aft+forward)*.5f?aft:forward;
                    if(result.Count==0||Math.Abs(result[result.Count-1].Z-exit)>.001f)result.Add(new NavalWalkPoint(x,floor,exit));
                }
            }
            result.Add(point);previous=point;
        }
        return result;
    }
    // Supply routing may rebuild individual segments after the base route was
    // planned. Reapply the open-stairwell and battery clearances afterwards.
    public static List<NavalWalkPoint> ClearSteamWeatherDeck(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship)
    {
        if(!ship.SteamPowered)return route;
        route=AvoidSteamUnderFlight(start,route,ship);
        route=AvoidSteamUpperStair(start,route,ship);
        route=AvoidSteamDeckEquipment(start,route,ship);
        route=AvoidSteamCasemates(start,route,ship);
        route=NavalConningTowerLayout.Route(start,route,ship);
        route=AvoidSteamUpperStair(start,route,ship);
        bool sameWeather=route.Count>0&&start.Y>ship.WeatherDeck-.12f&&route[route.Count-1].Y>ship.WeatherDeck-.12f;
        for(int i=0;i<route.Count-1;i++)
        {
            NavalWalkPoint point=route[i];if(point.Y<=ship.WeatherDeck-.12f)continue;
            float limit=System.Math.Max(.15f,ship.HullHalfWidth(point.Z)-.85f/ship.Scale);
            bool stairBypass=point.Z>=SteamAftGatewayZ-.01f&&point.Z<=.80f&&System.Math.Abs(point.X)>=1.20f;
            if(NavalConningTowerLayout.DoorLane(point,ship)){}
            else if(point.Z>=3.05f&&point.Z<=4.65f)
            {
                float side=System.Math.Abs(point.X)>.01f?System.Math.Sign(point.X):System.Math.Abs(start.X)>.01f?System.Math.Sign(start.X):1f;
                point.X=side*System.Math.Min(limit,System.Math.Max(1.35f,System.Math.Abs(point.X)));
            }
            else if(stairBypass){}
            else if(sameWeather&&point.Z>=-3.85f&&point.Z<=1.05f)
            {
                float side=System.Math.Abs(point.X)>.01f?System.Math.Sign(point.X):System.Math.Abs(start.X)>.01f?System.Math.Sign(start.X):1f;
                point.X=side*System.Math.Min(SteamBatteryLane,limit);
            }
            else
            {
                limit=System.Math.Min(limit,point.Z>=1.15f&&point.Z<=2.35f?.56f:.40f);
                point.X=System.Math.Max(-limit,System.Math.Min(limit,point.X));
            }
            route[i]=point;
        }
        return route;
    }
    private static List<NavalWalkPoint> AvoidSteamUpperStair(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship)
    {
        if(route.Count==0)return route;
        var result=new List<NavalWalkPoint>(route.Count+4);NavalWalkPoint previous=start;
        // Do not push the bypass under the conning tower. The v265 -4.25
        // corner lay in the tapered stern/tower collision and trapped a
        // sailor from the first frame. -3.90 remains behind the aft shield,
        // while the tower-specific router owns every point farther aft.
        const float aft=SteamAftGatewayZ,forward=.80f;
        for(int routeIndex=0;routeIndex<route.Count;routeIndex++)
        {
            NavalWalkPoint point=route[routeIndex],adjusted=point;
            bool horizontal=previous.Y>ship.WeatherDeck-.12f&&point.Y>ship.WeatherDeck-.12f&&System.Math.Abs(previous.Y-point.Y)<.04f;
            // Door-lane points on the deck still need the lateral stair
            // bypass. Suppressing them here created a diagonal from the
            // conning-tower doorway straight across the open upper flight.
            // Only points actually inside the raised tower are exempt.
            bool conningTransit=NavalConningTowerLayout.Inside(previous,ship)||NavalConningTowerLayout.Inside(point,ship);
            bool crosses=horizontal&&!conningTransit&&NavalSupplyRoutes.Hits(previous,point,-1.20f,1.20f,aft,forward);
            bool previousInside=System.Math.Abs(previous.X)<1.20f&&previous.Z>aft&&previous.Z<forward;
            bool pointInside=System.Math.Abs(point.X)<1.20f&&point.Z>aft&&point.Z<forward;
            // Gun posts sit just inside the broad stair exclusion but on real
            // deck.  When leaving one, step outward before travelling along
            // the ship; otherwise the diagonal cuts across the open flight.
            if(horizontal&&!conningTransit&&previousInside&&!pointInside&&!NavalStairTraffic.SamePassage(previous,point,ship))
            {
                float side=System.Math.Abs(previous.X)>.05f?System.Math.Sign(previous.X):System.Math.Abs(point.X)>.05f?System.Math.Sign(point.X):1f;
                float lane=previous.Z<=-3.70f?SteamAftGatewayLane:SteamBatteryLane;
                result.Add(new NavalWalkPoint(side*lane,ship.WeatherDeck,previous.Z));
            }
            // Base same-deck planning may first add an inboard corridor point
            // while both positions are nominally inside the flight rectangle.
            // It is not a stair traversal; redirect that intermediate point
            // to the bypass immediately instead of commanding an inward step
            // followed by the outward step which stalled in the v266 battle.
            if(horizontal&&!conningTransit&&previousInside&&pointInside&&routeIndex<route.Count-1&&!NavalStairTraffic.SamePassage(previous,point,ship))
            {
                float side=System.Math.Abs(previous.X)>.05f?System.Math.Sign(previous.X):System.Math.Abs(point.X)>.05f?System.Math.Sign(point.X):1f;
                adjusted.X=side*(previous.Z<=-3.70f?SteamAftGatewayLane:SteamBatteryLane);
            }
            if(crosses&&!previousInside&&!pointInside)
            {
                float side=System.Math.Abs(previous.X)>.05f?System.Math.Sign(previous.X):System.Math.Abs(point.X)>.05f?System.Math.Sign(point.X):1f;
                float entry=previous.Z<(aft+forward)*.5f?aft:forward,exit=point.Z<(aft+forward)*.5f?aft:forward;
                float x=side*(entry==aft||exit==aft?SteamAftGatewayLane:SteamBatteryLane);
                result.Add(new NavalWalkPoint(x,ship.WeatherDeck,entry));if(entry!=exit)result.Add(new NavalWalkPoint(x,ship.WeatherDeck,exit));
            }
            // A cargo/conning detour may place an intermediate weather-deck
            // point just inside the open-flight rectangle without actually
            // entering the stairs. Keep that point on the side bypass; the
            // final caller-owned endpoint is preserved verbatim.
            if(horizontal&&!conningTransit&&!previousInside&&pointInside&&routeIndex<route.Count-1&&!NavalStairTraffic.SamePassage(previous,point,ship))
            {
                float side=System.Math.Abs(previous.X)>.05f?System.Math.Sign(previous.X):System.Math.Abs(point.X)>.05f?System.Math.Sign(point.X):1f;
                float x=side*SteamAftGatewayLane;
                result.Add(new NavalWalkPoint(x,ship.WeatherDeck,aft));adjusted.X=x;
            }
            result.Add(adjusted);previous=adjusted;
        }
        return result;
    }
    private static List<NavalWalkPoint> AvoidSteamCasemates(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship)
    {
        // The capsule cannot round a pedestal corner diagonally. The logged
        // failure stopped at (1.80,-0.45) while aiming directly for
        // (1.48,0.80). First move squarely into the inboard battery lane, then
        // continue fore/aft past the fixed shield and mounting trunk.
        var result=new List<NavalWalkPoint>(route.Count+8);NavalWalkPoint previous=start;
        const float inner=1.70f,outer=2.60f,halfDepth=.55f,lane=SteamBatteryLane;
        foreach(NavalWalkPoint point in route)
        {
            bool weather=previous.Y>ship.WeatherDeck-.12f&&point.Y>ship.WeatherDeck-.12f&&System.Math.Abs(previous.Y-point.Y)<.04f;
            bool inserted=false;
            if(weather)for(int bay=0;bay<3&&!inserted;bay++)
            {
                float z=NavalShipDefinition.GunZ(ship,bay*2);
                for(int side=-1;side<=1;side+=2)
                {
                    float left=side<0?-outer:inner,right=side<0?-inner:outer;
                    if(!NavalSupplyRoutes.Hits(previous,point,left,right,z-halfDepth,z+halfDepth))continue;
                    bool previousInside=previous.X>left&&previous.X<right&&previous.Z>z-halfDepth&&previous.Z<z+halfDepth;
                    bool pointInside=point.X>left&&point.X<right&&point.Z>z-halfDepth&&point.Z<z+halfDepth;
                    float x=side*lane;
                    if(previousInside)result.Add(new NavalWalkPoint(x,ship.WeatherDeck,previous.Z));
                    else
                    {
                        float entry=previous.Z<z?z-halfDepth:z+halfDepth;
                        result.Add(new NavalWalkPoint(x,ship.WeatherDeck,entry));
                    }
                    if(!pointInside)
                    {
                        float exit=point.Z<z?z-halfDepth:z+halfDepth;
                        if(result.Count==0||result[result.Count-1].Z!=exit)result.Add(new NavalWalkPoint(x,ship.WeatherDeck,exit));
                    }
                    inserted=true;break;
                }
            }
            result.Add(point);previous=point;
        }
        return result;
    }
    private static List<NavalWalkPoint> AvoidSteamDeckEquipment(NavalWalkPoint start,List<NavalWalkPoint> route,NavalShipDefinition ship)
    {
        var result=new List<NavalWalkPoint>(route.Count+4);var previous=start;
        const float lane=.54f,front=1.20f,back=2.30f,half=.48f;
        foreach(var point in route)
        {
            bool deck=previous.Y>ship.WeatherDeck-.12f&&point.Y>ship.WeatherDeck-.12f;
            if(deck&&NavalSupplyRoutes.Hits(previous,point,NavalSteamLayout.FunnelX-half,NavalSteamLayout.FunnelX+half,front,back))
            {
                float side=System.Math.Abs(previous.X)>.2f?System.Math.Sign(previous.X):System.Math.Abs(point.X)>.2f?System.Math.Sign(point.X):1f;
                float entry=previous.Z<NavalSteamLayout.FunnelZ?front:back,exit=point.Z<NavalSteamLayout.FunnelZ?front:back;
                result.Add(new NavalWalkPoint(side*lane,ship.WeatherDeck,entry));
                if(entry!=exit)result.Add(new NavalWalkPoint(side*lane,ship.WeatherDeck,exit));
            }
            result.Add(point);previous=point;
        }
        return result;
    }
    private static List<NavalWalkPoint> PlanCore(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship)
    {
        if(ship.DeckCount<3)return NavalBallastRoutes.Plan(start,goal,ship.Partitions,ship.StairWidth,ship.HoldFloor,ship);
        float offset=ship.UpperDeckOffset;var route=new List<NavalWalkPoint>();
        bool highGoal=goal.Y>.825f+ship.UpperDeckOffset*.5f;
        if(NavalWalkRoutes.InStairwell(start.X,start.Y-offset,start.Z,ship.StairWidth*.5f+.175f,ship.UpperStairFloor))
        {
            route.Add(new NavalWalkPoint(0,start.Y,start.Z));
            if(highGoal)Shift(route,NavalWalkRoutes.UpFrom(start.Y-offset,start.Z,ship.UpperStairFloor),offset);
            else foreach(var p in NavalWalkRoutes.Down(ship.UpperStairFloor))if(p.Y+offset<=start.Y+.03f&&p.Z<=start.Z+.1f)route.Add(new NavalWalkPoint(p.X,p.Y+offset,p.Z));
            start=route[route.Count-1];
        }
        bool highStart=start.Y>.825f+ship.UpperDeckOffset*.5f;
        if(highStart&&highGoal){UpperPlan(route,start,goal,ship);return route;}
        if(highStart)
        {
            UpperPlan(route,start,new NavalWalkPoint(0,ship.WeatherDeck,NavalWalkRoutes.TopLandingZ),ship);
            Shift(route,NavalWalkRoutes.Down(ship.UpperStairFloor),offset);start=route[route.Count-1];
        }
        bool middleGoal=!highGoal&&goal.Y>.3f;
        if(middleGoal){
            bool lowerStair=NavalWalkRoutes.InStairwell(start.X,start.Y,start.Z,ship.StairWidth*.5f+.175f,ship.HoldFloor);
            if(start.Y<.3f||lowerStair){
                route.AddRange(NavalBallastRoutes.Plan(start,new NavalWalkPoint(0,.825f,NavalWalkRoutes.TopLandingZ),ship.Partitions,ship.StairWidth,ship.HoldFloor,ship));
                start=route[route.Count-1];
            }
            UpperPlan(route,start,goal,ship);return route;
        }
        float middleTank=NavalBallastLayout.Surface(start.X,start.Z,.825f,ship);
        if(middleTank>.826f&&System.Math.Abs(start.Y-middleTank)<.2f){
            // The sail-era 1.65 lane leaves the tapered ironclad bow and was
            // the source of the recurring z=4.46 stale-deck route. Step off a
            // steam wing tank through its real inboard service lane.
            float tankExit=ship.SteamPowered?1.20f:1.65f;
            UpperPlan(route,start,new NavalWalkPoint(start.X<0?-tankExit:tankExit,.825f,start.Z),ship);
            start=route[route.Count-1];
        }
        NavalWalkPoint landing=highGoal?new NavalWalkPoint(0,.825f,-3.65f):goal;
        route.AddRange(NavalBallastRoutes.Plan(start,landing,ship.Partitions,ship.StairWidth,ship.HoldFloor,ship));
        if(highGoal)
        {
            Shift(route,NavalWalkRoutes.UpFrom(ship.UpperStairFloor,-3.65f,ship.UpperStairFloor),offset);
            UpperPlan(route,route[route.Count-1],goal,ship);
        }
        return route;
    }
}
