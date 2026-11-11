using System;
// Novik slabs are 0.2 real metres thick, unlike the legacy quarter-local-unit deck.
public static class NavalDestroyerRepairAim{
 public static float Height(float hole,float stand,float floor,float weather,float foredeck,float scale){
  float result=Edge(hole,stand,floor,scale);result=Edge(result,stand,weather,scale);return Edge(result,stand,foredeck,scale);
 }
 private static float Edge(float hole,float stand,float deck,float scale){
  if(hole<deck-.25f/scale||hole>deck+.05f/scale)return hole;
  return stand>=deck-.05f/scale?Math.Max(hole,deck+.03f/scale):Math.Min(hole,deck-.23f/scale);
 }
}
