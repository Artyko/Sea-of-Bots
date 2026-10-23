public sealed class NavalCannonRepairBudget
{
 public const float Duration=60f;
 public bool Used {get;private set;}
 public bool Ready(float seconds){return !Used&&seconds>=Duration;}
 public bool Complete(bool fullyRestored,float seconds){if(!Ready(seconds)||!fullyRestored)return false;Used=true;return true;}
 public void Reset(){Used=false;}
}
