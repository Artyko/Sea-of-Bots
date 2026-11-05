// All dimensions are metres, independent of hull/root scale. Bore is not outside diameter.
public static class NavalGunDimensions {
 public const int DestroyerCalibre=102,CasemateCalibre=102,IroncladMainCalibre=203,CapitalCalibre=305;
 public static float Bore(int mm){return mm/1000f;}
 public static float TubeDiameter(int mm){return Bore(mm)*(mm<=152?5.3f:4f);}
 public static float BreechDiameter(int mm){return Bore(mm)*(mm<=152?6f:4.5f);}
 public static float Muzzle(int mm){return Bore(mm)*(mm<=152?33.333334f:40f);}
}
