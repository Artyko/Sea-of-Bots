public static class NavalDestroyerCrewLayout {
 private static readonly float[] rows={-36f,-26f,-8f,8f,26f,34f};
 public static float X(int sailor){return sailor%2==0?-5.6f:5.6f;}
 public static float Z(int sailor){return rows[(sailor/2)%rows.Length];}
}
