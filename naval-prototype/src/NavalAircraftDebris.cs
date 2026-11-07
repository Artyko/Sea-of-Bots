using System.Collections.Generic;
using UnityEngine;
// Visual wreckage has no colliders and leaves the aircraft structural simulation immediately.
public sealed class NavalAircraftDebris:MonoBehaviour
{
    private Vector3 velocity;private List<Mesh> meshes;private NavalWater ocean;private float age;
    public void Initialize(Vector3 speed,List<Mesh> owned,NavalWater water){velocity=speed;meshes=owned;ocean=water;}
    private void Update(){float dt=Time.deltaTime;age+=dt;velocity+=Vector3.down*9.81f*dt;
        transform.position+=velocity*dt;transform.Rotate(17f*dt,9f*dt,31f*dt,Space.Self);
        if(age>12f||ocean!=null&&transform.position.y<ocean.Height(transform.position.x,transform.position.z)-4f)Destroy(gameObject);}
    private void OnDestroy(){if(meshes!=null)foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
}
