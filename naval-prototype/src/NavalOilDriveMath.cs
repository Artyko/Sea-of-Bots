using System;
public static class NavalOilDriveMath {
 public static float Branch(float first,float second,float turbine,float screw,bool shaftConnected){return shaftConnected?(first+second)*.5f*Math.Max(0,Math.Min(1,turbine))*Math.Max(0,Math.Min(1,screw)):0;}
}
