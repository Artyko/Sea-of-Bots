using System;
public static class NavalLedgeRecoveryMath
{
    public static float DeckBelow(float feetY,float holdFloor,float weatherDeck,int deckCount)
    {
        if(deckCount>2&&feetY>(weatherDeck+.825f)*.5f)return weatherDeck;
        if(feetY>(holdFloor+.825f)*.5f)return .825f;
        return holdFloor;
    }
    // The native CharacterController normally leaves the measured feet about
    // 0.20 world metres above a moving deck.  Keep enough hysteresis above
    // that skin gap so ordinary walking cannot be mistaken for standing on a
    // gun, rail or cargo crate.
    // Runtime logs from the large steam hull show a stable 0.40--0.52 m
    // controller gap while the deck pitches.  That is still ordinary deck
    // contact, not a sailor standing on equipment.  Leave a real hysteresis
    // band above it; cargo tops and bulwarks are more than 0.65 m high.
    public static bool Needed(float feetY,float scale){return (feetY-.825f)*scale>.65f;}
    public static float InwardX(float x){return Math.Max(-2.1f,Math.Min(2.1f,x));}
    public static float InwardZ(float z){return Math.Max(-5f,Math.Min(5f,z));}
    public static bool WalkedOff(float dx,float dy,float dz,float scale)
    {return (dx*dx+dz*dz)*scale*scale<.2f*.2f&&dy*scale<=.2f;}
    public static bool Arrived(float dx,float dy,float dz,float scale)
    {return (dx*dx+dz*dz)*scale*scale<.12f*.12f&&dy*scale>=-.18f&&dy*scale<.65f;}
    public static bool Detached(float x,float y,float z,float halfWidth,float weatherDeck,float scale)
    {
        if(y<weatherDeck-.15f)return false;
        // A body at z=-6.25 is only brushing the tapered stern.  Treating it
        // as a lost moving-deck attachment teleported ammunition runners from
        // the stern to the bow.  This guard is only for catastrophic platform
        // separation; ordinary edge errors remain owned by deck safety.
        float margin=2.0f/Math.Max(.01f,scale);
        return Math.Abs(z)>6f+margin||Math.Abs(x)>halfWidth+margin;
    }
    public static float RecoveryZ(float z){return Math.Max(-5f,Math.Min(5f,z));}
    public static float RecoveryX(float x,float halfWidth,float scale)
    {
        float safe=Math.Max(.15f,halfWidth-.85f/Math.Max(.01f,scale));
        return Math.Sign(x==0?1:x)*Math.Min(.58f,safe);
    }
}
