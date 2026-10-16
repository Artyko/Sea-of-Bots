public static class NavalSessionPolicy
{
    public static bool Runs(bool spectating,bool playerPresent,bool playerAlive,bool inputEnabled,bool paused,bool sameScene)
    {return !paused&&sameScene&&(spectating||playerPresent&&playerAlive&&inputEnabled);}
}
