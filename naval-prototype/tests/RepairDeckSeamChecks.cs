using System;
class RepairDeckSeamChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){int cases=0;foreach(var ship in NavalShipDefinition.Choices){
 float[] decks=ship.DeckCount>2?new float[]{.825f,ship.WeatherDeck}:new float[]{.825f};
 foreach(float deck in decks)foreach(float inset in new float[]{.01f,.1f,.23f}){
 float hole=deck-inset,upper=ship.RepairAimY(hole,deck);
 float lowerFloor=deck>.825f?.825f:ship.HoldFloor,lower=ship.RepairAimY(hole,lowerFloor);
 Check(upper>deck,"Repair from above targets exposed top edge rather than buried voxel centre");
 Check(lower<deck-.25f,"Repair from below targets underside of correct ceiling");
 Check(upper-deck<.3f/ship.Scale&&deck-.25f-lower<.3f/ship.Scale,"Edge offset remains small in world units");
 Check(Math.Abs(ship.RepairAimY(deck+.5f,deck)-(deck+.5f))<.001f,"Ordinary wall holes retain actual target height");cases++;}
 }
 Console.WriteLine("PASS: "+cases+" seam cases on all ship profiles and both galleon decks, upper/lower exposed edges and unchanged ordinary walls.");}
}
