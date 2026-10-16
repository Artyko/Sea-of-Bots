using System;
class ShipRegistryChecks
{
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);}
    static void Main()
    {
        var registry=new NavalShipRegistry<object,object>();
        object playerHull=new object(),enemyHull=new object(),playerControls=new object(),enemyControls=new object(),result;
        registry.Register(playerHull,playerControls);registry.Register(enemyHull,enemyControls);
        Check(registry.TryGet(enemyHull,out result)&&object.ReferenceEquals(result,enemyControls),"Player gunner resolves enemy controls from hull");
        Check(registry.TryGet(playerHull,out result)&&object.ReferenceEquals(result,playerControls),"Enemy gunner resolves player controls from hull");
        Check(!registry.TryGet(new object(),out result),"Unregistered hull cannot select unrelated controls");
        registry.Remove(enemyHull,enemyControls);Check(!registry.TryGet(enemyHull,out result),"Despawn removes target");
        object respawnedControls=new object();registry.Register(enemyHull,respawnedControls);registry.Remove(enemyHull,enemyControls);
        Check(registry.TryGet(enemyHull,out result)&&object.ReferenceEquals(result,respawnedControls),"Old cleanup cannot unregister new controls");
        Console.WriteLine("PASS: distinct hull/controller ownership, both firing teams, despawn and respawn registration.");
    }
}
