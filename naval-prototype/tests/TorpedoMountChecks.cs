using System;
class TorpedoMountChecks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){Check(NavalTorpedoMountGeometry.ClearsPedestal(),"Rotating clearance box must be above its own pedestal");Check(NavalTorpedoMountGeometry.Axis-NavalTorpedoMountGeometry.TubeRadius>NavalTorpedoMountGeometry.PedestalTop,"Tube rings clear pedestal");foreach(float yaw in new[]{-125f,-90f,-55f,55f,90f,125f})Check(NavalTorpedoMountGeometry.InSector(yaw),"Both broadside launch arcs available");foreach(float yaw in new[]{-180f,-126f,-54f,0f,54f,126f,180f})Check(!NavalTorpedoMountGeometry.InSector(yaw),"Unsafe bow/stern launch excluded");Console.WriteLine("PASS: physical tube/pedestal separation and both independent torpedo attack sectors.");}
}
