using UnityEngine;
public static class NavalRiderCarry {
 // The character remains upright: rotate its support point, never the foot-to-root offset.
 public static Vector3 Position(Transform ship,Vector3 localFoot,Vector3 uprightOffset)
 {return ship.TransformPoint(localFoot)+uprightOffset;}
}
