using System;
class SubmersionChecks
{
    static void Check(float camera,float head,float expected)
    {float actual=NavalSubmersionMath.VisualDepth(camera,head);if(Math.Abs(actual-expected)>.0001f)throw new Exception(actual+" != "+expected);}
    static void Main()
    {
        Check(-2f,4f,4f); // Third-person camera above sea, head in submerged hold.
        Check(3f,-.5f,3f); // Camera underwater while head has surfaced.
        Check(-1f,-.5f,0f);
        Check(float.NegativeInfinity,float.NegativeInfinity,0f); // Dry sealed compartment.
        Check(float.NegativeInfinity,5f,5f);
        Check(2f,float.NegativeInfinity,2f);
        Console.WriteLine("PASS: submerged hold/head, third-person camera, surfacing and dry interior visual depth.");
    }
}
