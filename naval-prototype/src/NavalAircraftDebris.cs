using System.Collections.Generic;
using UnityEngine;
// Visual wreckage has no colliders and leaves the aircraft structural simulation immediately.
public sealed class NavalAircraftDebris:MonoBehaviour
{
    private Vector3 velocity;private List<Mesh> meshes;private NavalWater ocean;private float age;
    public void Initialize(Vector3 speed,List<Mesh> owned,NavalWater water){velocity=speed;meshes=owned;ocean=water;}
    private void Update(){float elapsed=Mathf.Max(0f,Time.deltaTime);age+=elapsed;
        while(elapsed>0f){float dt=Mathf.Min(.05f,elapsed);elapsed-=dt;velocity=NavalAircraftDebrisMath.StepVelocity(velocity,dt);
            transform.position+=velocity*dt;transform.Rotate(17f*dt,9f*dt,31f*dt,Space.Self);}
        if(age>20f||ocean!=null&&transform.position.y<ocean.Height(transform.position.x,transform.position.z)-4f)Destroy(gameObject);}
    private void OnDestroy(){if(meshes!=null)foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
}
