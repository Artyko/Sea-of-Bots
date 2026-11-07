using UnityEngine;
public static class NavalGunStationLayout {
 public const float ShieldHalfWidth=1.3f,ShieldThickness=.22f,RoofY=2.42f,RoofThickness=.22f;
 public const float RearDistance=3.6f,ApproachDistance=1.4f,OperatorHeightOffset=-.55f;
 public static Vector3 Offset(float yaw){double a=yaw*System.Math.PI/180;return new Vector3(-(float)System.Math.Sin(a)*RearDistance,OperatorHeightOffset,-(float)System.Math.Cos(a)*RearDistance);}
}
