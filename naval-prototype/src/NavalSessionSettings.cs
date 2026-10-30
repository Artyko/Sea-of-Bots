public static class NavalSessionSettings
{
    public const float SailSpawnDistance=250f,SteamSpawnDistance=800f;
    public static float EnemySpawnDistance {get{return Era==NavalEra.Steam?SteamSpawnDistance:SailSpawnDistance;}}
    public static NavalEra Era {get;private set;}
    public static NavalShipDefinition PlayerShip=NavalShipDefinition.Brigantine;
    public static NavalShipDefinition EnemyShip=NavalShipDefinition.Brigantine;
    public static NavalShipDefinition[] Choices {get{return Era==NavalEra.Steam?NavalShipDefinition.SteamChoices:NavalShipDefinition.Choices;}}
    public static void Configure(NavalEra era)
    {
        Era=era;
        if(era==NavalEra.Steam){PlayerShip=EnemyShip=NavalShipDefinition.Ironclad;}
        else
        {
            if(PlayerShip.SteamPowered)PlayerShip=NavalShipDefinition.Brigantine;
            if(EnemyShip.SteamPowered)EnemyShip=NavalShipDefinition.Brigantine;
        }
    }
}
