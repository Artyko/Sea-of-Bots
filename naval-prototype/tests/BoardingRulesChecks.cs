using System;
class BoardingRulesChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        Check(NavalBoardingRules.EnemyOnDeck(true,false,false,2f,1f,3.8f),"Boarder on deck");
        Check(!NavalBoardingRules.EnemyOnDeck(true,false,true,2f,1f,3.8f),"Ally excluded");
        Check(!NavalBoardingRules.EnemyOnDeck(true,true,false,2f,1f,3.8f),"Neutral excluded");
        Check(!NavalBoardingRules.EnemyOnDeck(false,false,false,2f,1f,3.8f),"Dead excluded");
        Check(!NavalBoardingRules.EnemyOnDeck(true,false,false,3.7f,0f,3.8f),"Swimmer outside hull excluded");
        Check(!NavalBoardingRules.EnemyOnDeck(true,false,false,2f,-1f,3.8f),"Hold is not upper deck");
        Check(NavalBoardingRules.ChooseGuard(true,true,false,false)==0,"Only one guard selected");
        Check(NavalBoardingRules.ChooseGuard(true,true,true,false)==1,"Free sailor before repairer");
        Check(NavalBoardingRules.ChooseGuard(false,true,false,false)==1,"Dead sailor cannot guard");
        Check(NavalBoardingRules.ChooseGuard(false,false,false,false)==-1,"No surviving defenders");
        Console.WriteLine("PASS: deck threat bounds, team filtering and single guard selection.");
    }
}
