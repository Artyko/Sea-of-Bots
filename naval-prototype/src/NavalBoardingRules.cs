using System;
public static class NavalBoardingRules
{
    public static bool EnemyOnDeck(bool alive,bool neutral,bool sameTeam,float x,float y,float z)
    {return alive&&!neutral&&!sameTeam&&Math.Abs(x)<=3.2f&&Math.Abs(z)<=6.2f&&y>=0.65f&&y<=3.1f;}
    public static int ChooseGuard(bool alive0,bool alive1,bool repairing0,bool repairing1)
    {return alive0&&!repairing0?0:alive1&&!repairing1?1:alive0?0:alive1?1:-1;}
}
