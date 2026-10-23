using ModLibrary;
using UnityEngine;
public sealed partial class NavalFireSystem
{
    private GlobalFireParticleSystem nativeFire;
    private ParticleSystem shipFlames;
    private readonly ParticleSystem.Particle[] flameParticles=new ParticleSystem.Particle[256];
    private float nextNativeFireLookup,flameMeshExtent=1f;
    private void EmitFlame(Vector3 point)
    {
        if(WaterTouches(point))return;
        if(shipFlames==null&&Time.time>=nextNativeFireLookup)
        {
            nextNativeFireLookup=Time.time+5f;nativeFire=Singleton<GlobalFireParticleSystem>.Instance;
            if(nativeFire!=null&&nativeFire.FireSystem!=null)
            {
                shipFlames=CreateShipFlames(nativeFire.FireSystem);
                shipFlames.transform.SetParent(NavalTestWorld.Active.transform,false);shipFlames.transform.position=Vector3.zero;shipFlames.transform.rotation=Quaternion.identity;shipFlames.transform.localScale=Vector3.one;
                shipFlames.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=shipFlames.main;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=false;main.maxParticles=flameParticles.Length;
                var emission=shipFlames.emission;emission.enabled=false;
                shipFlames.Play(false);
            }
        }
        if(shipFlames!=null)
        {
            // Emit in world metres, independent of native prefab scale and ship size.
            var emit=new ParticleSystem.EmitParams();emit.position=point;emit.startSize=Random.Range(.65f,.95f)/flameMeshExtent;
            emit.startLifetime=Random.Range(.6f,.95f);emit.velocity=Vector3.up*.8f;
            shipFlames.Emit(emit,1);
        }
    }
    private ParticleSystem CreateShipFlames(ParticleSystem source)
    {
        // Copy visual modules only: cloning the native GameObject could also
        // clone its singleton controller or native damage scripts.
        var copy=new GameObject("Naval owned fire").AddComponent<ParticleSystem>();
        copy.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var a=source.main;var b=copy.main;
        b.startLifetime=a.startLifetime;b.startSpeed=a.startSpeed;b.startColor=a.startColor;b.startSize3D=a.startSize3D;
        b.startSizeX=a.startSizeX;b.startSizeY=a.startSizeY;b.startSizeZ=a.startSizeZ;
        b.startRotation3D=a.startRotation3D;b.startRotationX=a.startRotationX;b.startRotationY=a.startRotationY;b.startRotationZ=a.startRotationZ;b.gravityModifier=a.gravityModifier;
        var sourceRenderer=source.GetComponent<ParticleSystemRenderer>();var targetRenderer=copy.GetComponent<ParticleSystemRenderer>();
        if(sourceRenderer!=null){targetRenderer.sharedMaterials=sourceRenderer.sharedMaterials;targetRenderer.renderMode=sourceRenderer.renderMode;targetRenderer.alignment=sourceRenderer.alignment;
            if(sourceRenderer.renderMode==ParticleSystemRenderMode.Mesh)
            {
                targetRenderer.mesh=sourceRenderer.mesh;
                if(targetRenderer.mesh!=null){Vector3 size=targetRenderer.mesh.bounds.size;flameMeshExtent=Mathf.Max(.01f,Mathf.Max(size.x,Mathf.Max(size.y,size.z)));}
            }
            var streams=new System.Collections.Generic.List<ParticleSystemVertexStream>();sourceRenderer.GetActiveVertexStreams(streams);targetRenderer.SetActiveVertexStreams(streams);
        }
        var va=source.velocityOverLifetime;var vb=copy.velocityOverLifetime;vb.enabled=false;vb.space=va.space;vb.x=va.x;vb.y=va.y;vb.z=va.z;
        var ca=source.colorOverLifetime;var cb=copy.colorOverLifetime;cb.enabled=ca.enabled;cb.color=ca.color;
        var sa=source.sizeOverLifetime;var sb=copy.sizeOverLifetime;sb.enabled=true;sb.separateAxes=false;sb.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0f,.5f),new Keyframe(.3f,1f),new Keyframe(1f,0f)));
        var ta=source.textureSheetAnimation;var tb=copy.textureSheetAnimation;tb.enabled=ta.enabled;tb.mode=ta.mode;tb.numTilesX=ta.numTilesX;tb.numTilesY=ta.numTilesY;tb.frameOverTime=ta.frameOverTime;tb.startFrame=ta.startFrame;tb.cycleCount=ta.cycleCount;
        if(ta.mode==ParticleSystemAnimationMode.Sprites)for(int i=0;i<ta.spriteCount;i++)tb.AddSprite(ta.GetSprite(i));
        return copy;
    }
    private void HideUnderwaterFlames()
    {
        if(shipFlames==null||shipFlames.particleCount==0)return;
        if(!HasFire){shipFlames.Clear(false);return;}
        int count=shipFlames.GetParticles(flameParticles);bool changed=false;
        for(int i=0;i<count;i++)if(WaterTouches(flameParticles[i].position)){flameParticles[i].remainingLifetime=0;changed=true;}
        if(changed)shipFlames.SetParticles(flameParticles,count);
    }
}
