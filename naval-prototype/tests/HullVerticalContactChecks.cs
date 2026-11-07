using System;
using UnityEngine;
class HullVerticalContactChecks{
 static void Main(){
  var a=NavalShipDefinition.Dreadnought;var b=NavalShipDefinition.Novik;
  float al,ah,bl,bh;NavalHullVerticalContact.Bounds(a,Vector3.zero,Quaternion.identity,out al,out ah);
  NavalHullVerticalContact.Bounds(b,new Vector3(0,-35,0),Quaternion.identity,out bl,out bh);
  if(!NavalHullVerticalContact.Separated(al,ah,bl,bh))throw new Exception("Deep wreck must not block surface hull");
  NavalHullVerticalContact.Bounds(b,Vector3.zero,Quaternion.identity,out bl,out bh);
  if(NavalHullVerticalContact.Separated(al,ah,bl,bh))throw new Exception("Same-height hull collision must remain");
  var roll=new Quaternion(0,0,(float)Math.Sin(Math.PI/4),(float)Math.Cos(Math.PI/4));
  NavalHullVerticalContact.Bounds(b,Vector3.zero,roll,out bl,out bh);
  if(bh-bl<b.HullHalfWidth(0)*b.Scale*1.9f)throw new Exception("Capsized beam must affect vertical extent");
  if(NavalHullVerticalContact.Separated(0,2,1,3))throw new Exception("Intersecting intervals");
  Console.WriteLine("PASS: deep wreck separated, surface contact preserved, capsize height included.");
 }
}
