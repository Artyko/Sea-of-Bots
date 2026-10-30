using UnityEngine;
// One short-lived light per blast; all visible particles use the existing bounded batch.
public sealed class NavalPowderBlastVisual : MonoBehaviour
{
    private NavalSmokeBatch batch;
    private Material flame,smoke,charred;
    private Vector3 point;
    private float scale;
    private float age;
    private int stage;
    private Light flash;
    public void Initialize(NavalSmokeBatch effects,Vector3 centre,Material fire,Material cloud,Material charredFragments,float intensity)
    {
        batch=effects;point=centre;flame=fire;smoke=cloud;charred=charredFragments;scale=Mathf.Clamp(intensity,1f,2f);transform.position=point;
        flash=gameObject.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,.65f,.22f);
        flash.range=32*scale;flash.intensity=7*scale;flash.shadows=LightShadows.None;
        for(int i=0;i<Mathf.RoundToInt(8*scale);i++)batch.Emit(point+Random.insideUnitSphere*2.5f*scale,Random.insideUnitSphere*8+Vector3.up*3,flame,.5f,3.8f*scale);
        int rays=Mathf.RoundToInt(12*scale);
        for(int i=0;i<rays;i++)
        {
            float angle=i*Mathf.PI*2/rays;Vector3 ray=new Vector3(Mathf.Cos(angle),.35f,Mathf.Sin(angle));
            batch.Emit(point+ray*scale,ray*(13+3*scale),flame,.85f,1f*scale);
        }
    }
    private void Update()
    {
        if(batch==null){Destroy(gameObject);return;}
        age+=Time.deltaTime;flash.intensity=7*scale*Mathf.Clamp01(1-age/.45f);
        if(stage==0&&age>=.12f)
        {
            stage=1;
            // Small, bounded ballistic cubes read as scorched plating and
            // splintered internal fittings without creating physics objects.
            int fragments=Mathf.RoundToInt(24*scale);
            for(int i=0;i<fragments;i++)batch.EmitDebris(point+Random.insideUnitSphere*.7f*scale,
                Random.onUnitSphere*Random.Range(7f,15f)*scale+Vector3.up*Random.Range(5f,11f),
                i%7==0?flame:charred,Random.Range(.07f,.20f)*scale);
            // A dark rolling base, followed by a narrower buoyant core, gives
            // the cloud a mushroom shape instead of one uniform grey ball.
            int basePuffs=Mathf.RoundToInt(10*scale);
            for(int i=0;i<basePuffs;i++)
            {
                float angle=Random.value*Mathf.PI*2;Vector3 radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                batch.EmitPlume(point+radial*Random.Range(.3f,2.2f)*scale+Vector3.up*Random.Range(.4f,1.8f),
                    radial*Random.Range(1.2f,3.5f)+Vector3.up*Random.Range(2.8f,5.2f),smoke,Random.Range(1.2f,2.1f)*scale,5.4f,1.15f,.85f);
            }
            for(int i=0;i<Mathf.RoundToInt(7*scale);i++)batch.EmitPlume(point+Random.insideUnitSphere*.8f+Vector3.up*(1.2f+i*.35f),
                Vector3.up*Random.Range(5f,7.5f)+Random.insideUnitSphere*.7f,smoke,Random.Range(.9f,1.5f)*scale,6.2f,.78f,1.35f);
        }
        if(stage==1&&age>=.4f)
        {
            stage=2;
            for(int i=0;i<Mathf.RoundToInt(7*scale);i++)batch.EmitPlume(point+Vector3.up*(3+i*.85f)+Random.insideUnitSphere*.65f,
                Vector3.up*(4.5f+i*.35f)+Random.insideUnitSphere*.35f,smoke,1.45f*scale,6.8f,1.05f,1.55f);
        }
        if(age>=1.25f)Destroy(gameObject);
    }
}
