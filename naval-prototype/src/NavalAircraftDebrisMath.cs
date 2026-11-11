using System;
using UnityEngine;
public static class NavalAircraftDebrisMath
{
 public static float Elapsed(float dt){return Math.Max(0,Math.Min(dt,.05f));}
 public static Vector3 StepVelocity(Vector3 velocity,float dt){float damping=(float)Math.Exp(-.55f*dt);return new Vector3(velocity.x*damping,velocity.y*damping-9.81f*(1f-damping)/.55f,velocity.z*damping);}
}
