using System;
public static class NavalAssaultMath
{
 public static bool SameDeck(float a,float b,float scale){return Math.Abs(a-b)*scale<.8f;}
 public static bool Melee(float distance,float heightDifference,bool clear,bool alreadyFighting=false)
 {return distance<=(alreadyFighting?3.5f:2.7f)&&Math.Abs(heightDifference)<.8f&&clear;}
 public static float Floor(float y,NavalShipDefinition ship)
 {
  float floor=ship.HoldFloor;if(Math.Abs(y-.825f)<Math.Abs(y-floor))floor=.825f;
  if(Math.Abs(y-ship.WeatherDeck)<Math.Abs(y-floor))floor=ship.WeatherDeck;return floor;
 }
}
