using System;
class BowBoardingChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){
 foreach(var ship in NavalShipDefinition.Choices){
 float scale=ship.Scale,end=NavalBoardingContactMath.EndStand(scale),centers=NavalShipContactMath.HalfLength*scale;
 float gap=centers-end*scale*2;
 Check(gap<NavalBoardingJumpMath.MaxDistance,"Bow-to-bow contact must offer a reachable end crossing: "+ship.Name);
 Check(NavalCrewDeckSafety.Inside(0,end,ship.HullHalfWidth(end),scale,.45f),"Takeoff point leaves full body margin inside real deck");
 float deck=(ship.WeatherDeck+.055f)*scale,rail=(ship.AboveDeckY(2.25f)+ship.UpperDeckOffset)*scale;
 float height=NavalBoardingJumpMath.Height(deck,deck,rail,rail);
 Check(NavalBoardingJumpMath.Reachable(gap,height),"Native deck/rail profile permits planned bow jump: "+ship.Name);
 float physicalGap=NavalBoardingContactMath.Gap(0,0,0,scale,0,centers,180,scale);
 Check(physicalGap<.001f,"Nose contact recognized geometrically");
 Check(NavalBoardingContactMath.Allows(true,30,physicalGap),"Matros coming from hold retains authorization while ships touch");
 Check(NavalBoardingContactMath.Gap(0,0,0,scale,0,centers+4,180,scale)>3.9f,"Separated ships lose extended contact");
 }
 Check(!NavalBoardingContactMath.Allows(false,0,0),"Nearby ship without prior collision cannot trigger boarding");
 Check(!NavalBoardingContactMath.Allows(true,float.PositiveInfinity,0),"Missing contact cannot be inferred from proximity");
 Check(!NavalBoardingContactMath.Allows(true,30,4),"Old contact with distant target does not authorize departure");
 Check(!NavalBoardingContactMath.Allows(true,91,0),"Old stale contact eventually expires");
 Check(NavalBoardingContactMath.Allows(true,2,4),"Short normal post-impact window preserved");
 float oldGalleonGap=NavalShipContactMath.HalfLength*4-5.1f*8;
 Check(oldGalleonGap>NavalBoardingJumpMath.MaxDistance,"Regression catches former unreachable galleon bow launch positions");
 Console.WriteLine("PASS: all ship bow-to-bow distances and rail heights, full-size takeoff margin, maintained collision, hold ascent time and stale/distant/no-contact rejection.");
 }
}
