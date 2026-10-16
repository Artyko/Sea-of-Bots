using UnityEngine;

public partial class NavalStations : MonoBehaviour
{
    private Transform boat;
    private NavalHull hull;
    private FirstPersonMover player;
    private NavalWater ocean;
    private Transform helm;
    private Transform[] guns;
    private Transform[] barrels;
    private float[] yaw;
    private float[] pitch;
    private float[] readyAt;
    private float[] shotAt;
    private System.Collections.Generic.List<NavalHullVoxel>[] mechanisms;
    private Material iron;
    private Material flash;
    private Material smoke;
    private Material wheelAccent;
    private AudioClip sound;
    private readonly NavalStationState state = new NavalStationState();
    private int active { get { return state.Active; } } // 0 helm, 1 port, 2 starboard
    private int nearest = -1;
    public bool EnemyControlled;
    private Character firingRobot;
    public bool IsFunctional(int station) { return Functional(station); }
    public int PlayerStation { get { return EnemyControlled?-1:active; } }
    public bool RecentlyFired(int gun) { return Time.time-shotAt[gun]<2f; }
    public int TargetSide(Vector3 point)
    {return boat.InverseTransformPoint(point).x<0f?1:2;}
    public int GunCount {get{return hull.Definition.GunCount;}}
    public int WorkingGuns {get{int n=0;for(int i=1;i<=GunCount;i++)if(Functional(i))n++;return n;}}
    public void PauseInput(){ForwardInput=0f;TurnInput=0f;}
    public bool AllGunsBroken {get{for(int i=1;i<=GunCount;i++)if(Functional(i))return false;return true;}}
    public Vector3 Post(int station)
    {return station==0?new Vector3(0f,.825f,NavalShipDefinition.HelmZ(hull.Definition)):new Vector3(NavalShipDefinition.GunSide(station-1)*1.35f,.825f,NavalShipDefinition.GunZ(station-1));}
    public Vector3 CrewPosition(int station){return boat.TransformPoint(Post(station));}
    public Vector3 CrewForward(int station) { return station==0?boat.forward:barrels[station-1].forward; }
    public bool AimAndFire(int gun,Vector3 target,float dt,Character sailor)
    {
        if(PlayerStation==gun+1)return false;
        if(!Functional(gun+1)||sailor==null||!sailor.IsAlive())return false;
        float wantedYaw,angle;
        if(!TryAimSolution(gun,target,out wantedYaw,out angle))return false;
        yaw[gun]=Mathf.MoveTowards(yaw[gun],wantedYaw,28f*dt);
        pitch[gun]=Mathf.MoveTowards(pitch[gun],angle,24f*dt);Aim(gun);
        if(Time.time>=readyAt[gun]&&Mathf.Abs(yaw[gun]-wantedYaw)<0.6f&&Mathf.Abs(pitch[gun]-angle)<0.6f)
        { firingRobot=sailor;Fire(gun);firingRobot=null;return true; }
        return false;
    }
    public bool Operating { get { return active >= 0; } }
    public bool AtHelm { get { return active == 0; } }
    public float ForwardInput { get; private set; }
    public float TurnInput { get; private set; }
    public Vector3 OperatorPosition {get{return CrewPosition(active);}}
    public void Initialize(Transform ship, NavalHull owner, FirstPersonMover robot, NavalWater sea,
        Material wood, Material trim, Material black)
    {
        boat = ship; RegisterShip(); hull = owner; player = robot; ocean = sea; iron = black;
        guns=new Transform[GunCount];barrels=new Transform[GunCount];yaw=new float[GunCount];pitch=new float[GunCount];readyAt=new float[GunCount];shotAt=new float[GunCount];
        for(int i=0;i<GunCount;i++)shotAt[i]=-100f;
        mechanisms=new System.Collections.Generic.List<NavalHullVoxel>[GunCount+1];InitializeGunnery(GunCount);
        flash = MakeMaterial(new Color(1f, 0.65f, 0.08f));
        smoke = MakeMaterial(new Color(0.55f, 0.58f, 0.60f));
        wheelAccent = MakeMaterial(new Color(0.58f, 0.405f, 0.225f));
        helm = Root("ShipWheel", new Vector3(0f, hull.Definition.AboveDeckY(1.4f), NavalShipDefinition.HelmZ(hull.Definition)+.8f*hull.Definition.FixtureScale));
        helm.localScale=Vector3.one*hull.Definition.FixtureScale;
        hull.AddDecorationBox(helm, wood, new Vector3(0f, -0.3f, 0f), new Vector3(0.2f, 0.6f, 0.25f), true);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6f;
            hull.AddDecorationBox(helm, wheelAccent, new Vector3(Mathf.Sin(angle) * 0.4f, Mathf.Cos(angle) * 0.4f, 0f),
                new Vector3(0.17f, 0.17f, 0.14f), true);
        }
        hull.AddDecorationBox(helm, wood, Vector3.zero, new Vector3(0.75f, 0.10f, 0.12f), true);
        hull.AddDecorationBox(helm, wood, Vector3.zero, new Vector3(0.10f, 0.75f, 0.12f), true);
        hull.AddDecorationBox(helm, wheelAccent, new Vector3(0f, 0f, -0.09f), new Vector3(0.22f, 0.22f, 0.12f), true);
        for (int i = 0; i < GunCount; i++)
        {
            float side = NavalShipDefinition.GunSide(i);
            guns[i] = Root((side<0f?"PortCannon":"StarboardCannon")+i, new Vector3(side * 2.25f, 1f, NavalShipDefinition.GunZ(i)));
            hull.AddDecorationBox(guns[i], wood, new Vector3(0f, -0.03f, 0f), new Vector3(0.75f, 0.3f, 0.9f), true);
            for (int wheel = -1; wheel <= 1; wheel += 2)
                hull.AddDecorationBox(guns[i], black, new Vector3(wheel * 0.4f, -0.05f, 0f), new Vector3(0.15f, 0.35f, 0.65f), true);
            barrels[i] = new GameObject("CannonBarrel").transform;
            barrels[i].SetParent(guns[i], false); barrels[i].localPosition = new Vector3(0f, 0.35f, 0f);
            hull.AddDecorationBox(barrels[i], black, new Vector3(0f, 0f, 0.55f), new Vector3(0.38f, 0.38f, 1.3f), true);
            hull.AddDecorationBox(barrels[i], trim, new Vector3(0f, 0f, 0.9f), new Vector3(0.42f, 0.42f, 0.1f), true);
            pitch[i] = 5f; Aim(i);
        }
        sound = AudioClip.Create("NavalCannonBoom", 11025, 1, 22050, false);
        float[] samples = new float[11025]; System.Random random = new System.Random(14);
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / 22050f;
            samples[i] = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 14f) * 0.5f +
                Mathf.Sin(t * 2f * Mathf.PI * (65f - t * 40f)) * Mathf.Exp(-t * 9f) * 0.45f;
        }
        sound.SetData(samples, 0);
        for (int i = 0; i <= GunCount; i++)
        {
            mechanisms[i] = new System.Collections.Generic.List<NavalHullVoxel>();
            Transform mechanism = i == 0 ? helm : barrels[i - 1];
            foreach (NavalHullVoxel voxel in hull.Voxels)
                if (voxel.transform.IsChildOf(mechanism)) mechanisms[i].Add(voxel);
        }
    }
    private Transform Root(string name, Vector3 position)
    {
        Transform root = new GameObject("NavalPrototype_" + name).transform;
        root.SetParent(boat, false); root.localPosition = position; return root;
    }
    public void Tick(float dt, float feetOffset, bool sunk, bool swimming)
    {
        if (sunk || swimming || (active >= 0 && !Functional(active))) Exit();
        nearest = -1; float distance = 3f;
        if (active < 0 && !sunk && !swimming)
        {
            Vector3 feet = player.transform.position - Vector3.up * feetOffset;
            for (int i = 0; i <= GunCount; i++)
            {
                Vector3 anchor = CrewPosition(i);
                float d = Vector3.Distance(feet, anchor);
                if (d < distance) { distance = d; nearest = i; }
            }
        }
        if (Input.GetKeyDown(KeyCode.E)&&!(hull.Bilge!=null&&hull.Bilge.PlayerInteracting))
        {
            state.PressInteract(nearest, sunk || swimming || (nearest >= 0 && !Functional(nearest)),GunCount);
            if (active >= 0)
            {
                Vector3 facing = active == 0 ? boat.forward : barrels[active - 1].forward;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.001f) player.transform.rotation = Quaternion.LookRotation(facing);
            }
        }
        ForwardInput = 0f; TurnInput = 0f;
        if (active == 0)
        {
            ForwardInput = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
            TurnInput = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        }
        else if (active > 0)
        {
            int gun = active - 1;
            yaw[gun] = Mathf.Clamp(yaw[gun] + ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f)) * 28f * dt, -30f, 30f);
            pitch[gun] = Mathf.Clamp(pitch[gun] + ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f)) * 24f * dt, -5f, 45f);
            Aim(gun);
            if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && Time.time >= readyAt[gun]) Fire(gun);
        }
        UpdateRecoil();
    }
    public void UpdateRecoil()
    {
        for (int i = 0; i < GunCount; i++)
        {
            float recoil = Mathf.Clamp01(1f-(Time.time-shotAt[i])/0.35f) * 0.14f;
            barrels[i].localPosition = new Vector3(0f, 0.35f, 0f) - barrels[i].localRotation * Vector3.forward * recoil;
        }
    }
    private void Aim(int gun) { barrels[gun].localRotation = Quaternion.Euler(-pitch[gun], (gun % 2 == 0 ? -90f : 90f) + yaw[gun], 0f); }
    private bool Functional(int station)
    {
        if (mechanisms==null||station<0||station>=mechanisms.Length||mechanisms[station] == null) return false;
        if (mechanisms[station].Count == 0) return false;
        foreach (NavalHullVoxel voxel in mechanisms[station]) if (voxel == null || voxel.Missing || voxel.Detached) return false;
        return true;
    }
    private void Fire(int gun)
    {
        if (!Functional(gun + 1)) return;
        shotAt[gun]=Time.time;
        readyAt[gun] = Time.time + NavalGunneryMath.ReloadSeconds;
        Vector3 muzzle = barrels[gun].TransformPoint(new Vector3(0f, 0f, 1.38f));
        Vector3 direction = ShotDirection(gun,barrels[gun].forward);
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "NavalPrototype_Cannonball"; ball.transform.SetParent(transform, false);
        ball.transform.position = muzzle; ball.transform.localScale = Vector3.one * 0.3f;
        Collider collider = ball.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        ball.GetComponent<Renderer>().sharedMaterial = iron;
        ball.AddComponent<NavalCannonball>().Initialize(direction * 32f, boat, hull, player, ocean, smoke, Time.time + 10f,EnemyControlled,firingRobot);
        AudioSource.PlayClipAtPoint(sound, muzzle, 0.7f);
        NavalShotPuff.Create(transform, muzzle, direction * 2f, flash, 0.12f, 0.6f);
        for (int i = 0; i < 20; i++)
            NavalShotPuff.Create(transform, muzzle + Random.insideUnitSphere * 0.18f,
                direction * Random.Range(0.7f, 2.6f) + Vector3.up * Random.Range(0.6f, 1.3f) + Random.insideUnitSphere * 0.7f,
                smoke, Random.Range(1.1f, 2.1f), Random.Range(0.16f, 0.34f));
        Debug.Log("[NavalPrototype] Cannon fired: " + (gun % 2 == 0 ? "port" : "starboard"));
    }
    public void Exit() { state.Exit(); ForwardInput = 0f; TurnInput = 0f; }
    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color"); if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader); material.color = color; return material;
    }
    private void OnGUI()
    {
        DrawReloadBars();
        if(EnemyControlled)return;
        string text = active == 0 ? "HELM: W/S or arrows = sail; A/D = turn; E = leave" :
            (active > 0 ? "CANNON: A/D or arrows = aim +/-30; W/S = elevation -5..45; LMB/Space = fire; E = leave\nReload: " +
                Mathf.Max(0f, readyAt[active - 1] - Time.time).ToString("F1") + " s" :
                (nearest >= 0 ? "Press E: " + (nearest == 0 ? "ship wheel" : ((nearest-1)%2==0?"port cannon ":"starboard cannon ")+((nearest-1)/2+1)) :
                    "Approach a cannon or the stern wheel and press E"));
        if(active < 0 && nearest >= 0 && !Functional(nearest))
            text = (nearest == 0 ? "HELM DAMAGED" : "CANNON BARREL DAMAGED") + " - press R to repair";
        GUI.Box(new Rect(Screen.width * 0.5f - 350f, Screen.height - 90f, 700f, 70f), text);
    }
    private void OnDestroy()
    { UnregisterShip(); Exit(); if (flash != null) Destroy(flash); if (smoke != null) Destroy(smoke); if (wheelAccent != null) Destroy(wheelAccent); if (sound != null) Destroy(sound); }
}

public class NavalCannonball : MonoBehaviour
{
    private Vector3 velocity;
    private bool submerged;
    private float underwaterTravel;
    private float RadiusScale {get{return submerged?NavalProjectileWaterMath.DamageRadiusScale:1f;}}
    private int ImpactStrength {get{return submerged?Mathf.RoundToInt(4f*NavalProjectileWaterMath.DamagePower):4;}}
    private Transform boat;
    private NavalHull hull;
    private FirstPersonMover owner;
    private NavalWater ocean;
    private Material splash;
    private static int nextAttackId = 1800000000;
    private int attackId;
    private float expires;
    private float born;
    private bool hostile;
    private Character attacker;
    public void Initialize(Vector3 speed, Transform ship, NavalHull shell, FirstPersonMover player, NavalWater sea, Material material, float end,bool enemy=false,Character robot=null)
    { velocity = speed; boat = ship; hull = shell; owner = player; ocean = sea; splash = material; expires = end; born = Time.time; attackId = nextAttackId++; hostile=enemy;attacker=robot!=null?robot:player; }
    private void Update()
    {
        if (boat == null || owner == null || Time.time >= expires) { Destroy(gameObject); return; }
        if (Time.timeScale == 0f || !owner.IsPlayerInputEnabled()) return;
        float remaining=Mathf.Min(Time.deltaTime,0.05f);
        while(remaining>0.00001f)
        {
            float dt=Mathf.Min(remaining,0.008f);
            velocity+=Vector3.down*9.81f*dt;
            if(submerged)velocity*=NavalProjectileWaterMath.Damping(dt);
            Vector3 step=velocity*dt;
            Vector3 start=transform.position,next=start+step;
            if(!submerged&&(start.y<=ocean.Height(start.x,start.z)||next.y<=ocean.Height(next.x,next.z)))
            {
                float low=0f,high=1f;
                if(start.y>ocean.Height(start.x,start.z))
                    for(int i=0;i<10;i++)
                    {
                        float middle=(low+high)*0.5f;Vector3 p=start+step*middle;
                        if(p.y>ocean.Height(p.x,p.z))low=middle;else high=middle;
                    }
                else high=0f;
                if(HitSolid(step*high))return;
                transform.position=start+step*high;
                Vector3 surface=transform.position;surface.y=ocean.Height(surface.x,surface.z);
                NavalWaterSplash.Create(transform.parent,surface,ocean);
                submerged=true;velocity*=NavalProjectileWaterMath.EntrySpeedScale;
                remaining-=dt*high;
                continue;
            }
            if(submerged)step=Vector3.ClampMagnitude(step,NavalProjectileWaterMath.MaxTravel-underwaterTravel);
            if(HitSolid(step))return;
            transform.position+=step;
            if(submerged)
            {
                underwaterTravel+=step.magnitude;
                if(underwaterTravel>=NavalProjectileWaterMath.MaxTravel-0.001f||velocity.magnitude<0.6f)
                {Destroy(gameObject);return;}
            }
            remaining-=dt;
        }
    }
    private bool HitSolid(Vector3 step)
    {
        if(step.sqrMagnitude<0.00000001f)return false;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.12f, step.normalized, step.magnitude, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
        foreach (RaycastHit hit in hits)
        {
            Vector3 hitPoint=hit.distance<=0.0001f?hit.collider.ClosestPoint(transform.position):hit.point;
            Character hitRobot=hit.collider.GetComponentInParent<Character>();
            if (hitRobot == attacker || (!hostile && hitRobot == owner)) continue;
            if (hit.collider.transform.IsChildOf(boat) && Time.time < born + 0.12f) continue;
            NavalHullVoxel voxel = hit.collider.GetComponent<NavalHullVoxel>();
            if (voxel != null && voxel.Owner != null)
            {
                Vector3 local=voxel.transform.InverseTransformPoint(hit.point);
                if(!NavalCannonImpactMath.ValidContact(hit.distance,local.x,local.y,local.z))
                    hitPoint=hit.collider.ClosestPoint(transform.position);
                // Initial sphere overlaps can report a zero/invalid contact. Recover the
                // actual surface, then place the compact damage sphere just inside the plank.
                Vector3 toCentre=voxel.transform.position-hitPoint;
                Vector3 damagePoint=hitPoint+toCentre*NavalCannonImpactMath.InsetFraction(toCentre.magnitude);
                voxel.Owner.Damage(voxel.Owner.Root.InverseTransformPoint(damagePoint), NavalRigImpactMath.CannonRadius*RadiusScale);
                if (!voxel.Missing) voxel.SetMissing(true);
            }
            else
            {
                BaseBodyPart part = hit.collider.GetComponentInParent<BaseBodyPart>();
                if(part==null&&hitRobot!=null)continue;
                Character target = part != null ? part.GetOwner() : null;
                if (attacker != null && part != null && target != null && target != attacker && target.IsAlive() &&
                    NavalBattleMath.CanDamageCrew(hostile,target.IsPlayerTeam,target.IsNeutral) && !target.IsImmuneToDamage() && part.CanBeDamaged)
                    part.TryStabVolume(transform.position, hitPoint + step.normalized * 0.3f*RadiusScale, attackId, ImpactStrength, attacker, FireType.None, false, DamageSourceType.None);
                else if (hit.collider.isTrigger) continue;
            }
            Burst(hitPoint); Destroy(gameObject); return true;
        }
        return false;
    }
    private void Burst(Vector3 point)
    {
        // The projectile also damages native robot body voxels near its impact,
        // including sailors struck beside a deck or cannon. A stable attack ID
        // prevents a body part receiving duplicate hits from several colliders.
        System.Collections.Generic.HashSet<BaseBodyPart> damaged=new System.Collections.Generic.HashSet<BaseBodyPart>();
        foreach(Collider collider in Physics.OverlapSphere(point,0.9f*RadiusScale,~0,QueryTriggerInteraction.Collide))
        {
            BaseBodyPart part=collider.GetComponentInParent<BaseBodyPart>();
            Character target=part!=null?part.GetOwner():null;
            if(attacker!=null&&part!=null&&damaged.Add(part)&&target!=null&&target!=attacker&&target.IsAlive()&&
                NavalBattleMath.CanDamageCrew(hostile,target.IsPlayerTeam,target.IsNeutral)&&!target.IsImmuneToDamage()&&part.CanBeDamaged)
            {
                Vector3 hit=collider.ClosestPoint(point);
                if(part.TryStabVolume(transform.position,hit+velocity.normalized*0.35f*RadiusScale,attackId,ImpactStrength,attacker,FireType.None,false,DamageSourceType.None))
                    Debug.Log("[NavalPrototype] Cannon impact damaged robot body: "+target.name);
            }
        }
        if(submerged)return;
        for (int i = 0; i < 16; i++)
            NavalShotPuff.Create(transform.parent, point + Random.insideUnitSphere * 0.15f,
                Random.insideUnitSphere * 1.7f + Vector3.up * 1.5f, splash,
                Random.Range(0.7f, 1.4f), Random.Range(0.14f, 0.3f));
    }
}

public class NavalShotPuff : MonoBehaviour
{
    private Vector3 velocity;
    private float life;
    private float total;
    private float size;
    private Material ownMaterial;
    public static void Create(Transform parent, Vector3 position, Vector3 speed, Material material, float duration, float radius)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = "NavalPrototype_PixelSmoke";
        Collider collider = obj.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
        obj.transform.SetParent(parent, false); obj.transform.position = position;
        NavalShotPuff puff = obj.AddComponent<NavalShotPuff>(); puff.velocity = speed; puff.life = duration; puff.total = duration; puff.size = radius;
        // Solid, axis-aligned blocks keep crisp voxel edges and avoid the
        // transparent overlapping spheres of the previous smoke effect.
        puff.ownMaterial = new Material(material);
        Color tint = material.color * Random.Range(0.8f, 1.18f); tint.a = 1f;
        puff.ownMaterial.color = tint;
        obj.GetComponent<Renderer>().sharedMaterial = puff.ownMaterial;
        obj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        obj.GetComponent<Renderer>().receiveShadows = false;
        obj.transform.localScale = Vector3.one * radius;
    }
    private void Update()
    {
        life -= Time.deltaTime; if (life <= 0f) { Destroy(gameObject); return; }
        transform.position += velocity * Time.deltaTime;
        velocity = Vector3.Lerp(velocity, Vector3.up * 0.5f, 1f - Mathf.Exp(-1.6f * Time.deltaTime));
        float blockSize = size * (1f + (1f - life / total) * 1.3f) * Mathf.Min(1f, life / (total * 0.3f));
        transform.localScale = Vector3.one * Mathf.Max(0.04f, Mathf.Round(blockSize / 0.04f) * 0.04f);
    }
    private void OnDestroy() { if (ownMaterial != null) Destroy(ownMaterial); }
}
