// Frozen v111 reference used only by regression checks.
using System;

// Height columns exchange volume only across shared edges. SI units for volume,
// area and hydraulic heads; ship coordinates for indexing and rendering.
public class ReferenceFloodGridV111
{
    public readonly int Columns, Rows;
    public readonly float Width, Length, Scale, CellWidth, CellLength, CellArea;
    public readonly float MaxCellVolume;
    public readonly float[] Water;
    public readonly float[] RightSill,ForwardSill;
    private readonly float[] delta,centerX,centerZ,worldColumn;
    public ReferenceFloodGridV111(int columns, int rows, float width, float length, float scale, float maximumDepth = float.MaxValue)
    {
        Columns=columns; Rows=rows; Width=width; Length=length; Scale=scale;
        CellWidth=width/columns; CellLength=length/rows;
        CellArea=CellWidth*CellLength*scale*scale;
        MaxCellVolume=maximumDepth*CellArea*scale;
        Water=new float[columns*rows]; delta=new float[Water.Length];
        centerX=new float[Water.Length];centerZ=new float[Water.Length];worldColumn=new float[columns];
        for(int i=0;i<Water.Length;i++){centerX[i]=((i%columns)+.5f)*CellWidth-width*.5f;centerZ[i]=((i/columns)+.5f)*CellLength-length*.5f;}
        RightSill=new float[Water.Length];ForwardSill=new float[Water.Length];
        for(int i=0;i<Water.Length;i++){RightSill[i]=ForwardSill[i]=float.NegativeInfinity;}
    }
    public float Total { get { float sum=0f; foreach(float value in Water) sum+=value; return sum; } }
    public float Side(bool port)
    {
        float sum=0f;
        for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++)
            if((x<Columns/2)==port)sum+=Water[z*Columns+x];
        return sum;
    }
    public float LongitudinalMoment
    {
        get { float sum=0f; for(int i=0;i<Water.Length;i++)sum+=Water[i]*Z(i)/(Length*0.5f); return sum; }
    }
    public int Index(float x,float z)
    {
        int col=Math.Max(0,Math.Min(Columns-1,(int)Math.Floor((x+Width*0.5f)/CellWidth)));
        int row=Math.Max(0,Math.Min(Rows-1,(int)Math.Floor((z+Length*0.5f)/CellLength)));
        return row*Columns+col;
    }
    public float X(int i) { return centerX[i]; }
    public float Z(int i) { return centerZ[i]; }
    public void FillFloorHeights(float[] output,float slopeX,float slopeZ,float floorTerm,float translationY)
    {
        if(output==null||output.Length<Water.Length)throw new ArgumentException("Floor buffer is too small");
        for(int x=0;x<Columns;x++)worldColumn[x]=slopeX*centerX[x];
        for(int z=0;z<Rows;z++)
        {
            int row=z*Columns;float worldZ=slopeZ*centerZ[row];
            for(int x=0;x<Columns;x++)output[row+x]=worldColumn[x]+floorTerm+worldZ+translationY;
        }
    }
    public float Depth(int i) { return Water[i]/(CellArea*Scale); }
    public float Add(int i,float amount)
    { float actual=Math.Max(-Water[i],Math.Min(MaxCellVolume-Water[i],amount)); Water[i]+=actual; return actual; }
    public void Pump(float amount)
    {
        float total=Total; if(total<=0f)return;
        float fraction=Math.Max(0f,1f-amount/total);
        for(int i=0;i<Water.Length;i++)Water[i]*=fraction;
    }
    public void Spread(float dt,float[] floorHeights,float upCosine)
    {
        int steps=Math.Max(1,(int)Math.Ceiling(dt/0.02f));
        float step=dt/steps;
        for(int s=0;s<steps;s++)
        {
            Array.Clear(delta,0,delta.Length);
            for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++)
            {
                int a=z*Columns+x;
                if(x+1<Columns)Exchange(a,a+1,CellLength*Scale,step,floorHeights,upCosine,RightSill[a]);
                if(z+1<Rows)Exchange(a,a+Columns,CellWidth*Scale,step,floorHeights,upCosine,ForwardSill[a]);
            }
            for(int i=0;i<Water.Length;i++)Water[i]=Math.Max(0f,Water[i]+delta[i]);
        }
    }
    private void Exchange(int a,int b,float edge,float dt,float[] floor,float up,float sill)
    {
        if(float.IsPositiveInfinity(sill)||(Water[a]<=0f&&Water[b]<=0f))return;
        up=Math.Max(0.2f,up);
        float headA=floor[a]+Water[a]/CellArea*up, headB=floor[b]+Water[b]/CellArea*up;
        int from=headA>headB?a:b, to=from==a?b:a;
        float high=Math.Max(headA,headB), low=Math.Max(Math.Min(headA,headB),Math.Max(sill,Math.Max(floor[a],floor[b])));
        float difference=Math.Max(0f,high-low);
        if(difference<=0f||Water[from]<=0f)return;
        float depth=Math.Min(Water[from]/CellArea,difference);
        float amount=0.9f*edge*depth*(float)Math.Sqrt(19.62f*difference)*dt;
        // At most four neighbours: these bounds preserve positivity even when
        // all edges request water from the same cell in a synchronous step.
        amount=Math.Min(amount,Math.Min(Water[from]*0.249f,difference*CellArea/up*0.249f));
        amount=Math.Min(amount,Math.Max(0f,MaxCellVolume-Water[to])*0.249f);
        delta[from]-=amount; delta[to]+=amount;
    }
}
