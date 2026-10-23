using UnityEngine;
// One short-lived light per blast; all visible particles use the existing bounded batch.
public sealed class NavalPowderBlastVisual : MonoBehaviour
{
    private NavalSmokeBatch batch;
    private Material flame,smoke,wood;
    private Vector3 point;
    private float age;
    private int stage;
    private Light flash;
    public void Initialize(NavalSmokeBatch effects,Vector3 centre,Material fire,Material cloud,Material timber)
    {
        batch=effects;point=centre;flame=fire;smoke=cloud;wood=timber;transform.position=point;
        flash=gameObject.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,.65f,.22f);
        flash.range=32;flash.intensity=7;flash.shadows=LightShadows.None;
        for(int i=0;i<8;i++)batch.Emit(point+Random.insideUnitSphere*2.5f,Random.insideUnitSphere*8+Vector3.up*3,flame,.5f,3.8f);
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI*2/12;Vector3 ray=new Vector3(Mathf.Cos(angle),.35f,Mathf.Sin(angle));
            batch.Emit(point+ray*1f,ray*14,flame,.85f,1f);
        }
    }
    private void Update()
    {
        if(batch==null){Destroy(gameObject);return;}
        age+=Time.deltaTime;flash.intensity=7*Mathf.Clamp01(1-age/.45f);
        if(stage==0&&age>=.12f)
        {
            stage=1;
            for(int i=0;i<16;i++)batch.EmitDebris(point+Random.insideUnitSphere*.6f,
                Random.insideUnitSphere*11+Vector3.up*9,i%4==0?flame:wood);
            for(int i=0;i<8;i++)batch.EmitPlume(point+Random.insideUnitSphere*3.2f+Vector3.up*2.4f,
                Vector3.up*4+Random.insideUnitSphere,smoke,1.9f);
        }
        if(stage==1&&age>=.4f)
        {
            stage=2;
            for(int i=0;i<6;i++)batch.EmitPlume(point+Vector3.up*(3+i*1f)+Random.insideUnitSphere,
                Vector3.up*(4+i*.3f),smoke,2.1f);
        }
        if(age>=.8f)Destroy(gameObject);
    }
}
