public static class NavalAmmoPriority {
 public static bool CanSupply(bool leaks,bool anotherRepairer,bool critical,bool trim,bool fire)
 {return !critical&&!trim&&!fire&&(!leaks||anotherRepairer);}
}
