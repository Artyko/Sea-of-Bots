public sealed class NavalCannonRepairBudget
{
 public const float Duration=60f;
 public bool Used {get;private set;}
 public bool Lost {get;private set;}
 public bool Ready(float seconds){return !Used&&!Lost&&seconds>=Duration;}
 public bool Complete(bool fullyRestored,float seconds){if(!Ready(seconds)||!fullyRestored)return false;Used=true;return true;}
 public void CatastrophicLoss(){Lost=true;Used=true;}
 public void Reset(){Used=Lost=false;}
}
