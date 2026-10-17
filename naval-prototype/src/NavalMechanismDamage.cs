// A destroyed mechanism cannot be rebuilt by successively repairing tiny patches.
public sealed class NavalMechanismDamage
{
    public const int RepairLimitPercent=25;
    public const float RepairLimit=RepairLimitPercent/100f;
    public bool Destroyed {get;private set;}
    public void Observe(int total,int damaged)
    {if(total>0&&(long)damaged*100>(long)total*RepairLimitPercent)Destroyed=true;}
    public void Reset(){Destroyed=false;}
}
