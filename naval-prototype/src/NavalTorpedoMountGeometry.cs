public static class NavalTorpedoMountGeometry {
 public const float PedestalTop=1.2f,Axis=1.65f,ClearanceHalfHeight=.43f,TubeRadius=.38f;
 public const float MinimumAttackYaw=55f,MaximumAttackYaw=125f;
 public static bool InSector(float yaw){float a=System.Math.Abs(yaw);return a>=MinimumAttackYaw&&a<=MaximumAttackYaw;}
 public static bool ClearsPedestal(){return Axis-ClearanceHalfHeight>PedestalTop;}
}
