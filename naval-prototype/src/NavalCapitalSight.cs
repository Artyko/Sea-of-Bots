using UnityEngine;
[DefaultExecutionOrder(710)]
public sealed class NavalCapitalSight:MonoBehaviour
{
    internal NavalStations Stations;private Camera sight;private Vector3 position;private Quaternion rotation;private float clip;
    private void LateUpdate(){Vector3 point;Quaternion aim;var world=NavalTestWorld.Active;
        if(Stations==null||world==null||!Stations.CapitalSight(out point,out aim)){Restore();return;}
        var view=world.ViewCamera;if(view==null)return;
        if(sight!=view){Restore();sight=view;position=view.transform.localPosition;rotation=view.transform.localRotation;clip=view.nearClipPlane;}
        sight.transform.SetPositionAndRotation(point,aim);sight.nearClipPlane=.05f;
    }
    private void Restore(){if(sight==null)return;sight.transform.localPosition=position;sight.transform.localRotation=rotation;sight.nearClipPlane=clip;sight=null;}
    private void OnDisable(){Restore();}private void OnDestroy(){Restore();}
}
