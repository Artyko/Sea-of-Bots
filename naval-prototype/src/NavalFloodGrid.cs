using System;

// Height columns exchange volume only across shared edges. SI units for volume,
// area and hydraulic heads; ship coordinates for indexing and rendering.
public class NavalFloodGrid
{
    public readonly int Columns, Rows;
    public readonly float Width, Length, Scale, CellWidth, CellLength, CellArea;
    public readonly float MaxCellVolume;
    private readonly float[] water;
    private readonly bool[] activeCells;
    public readonly NavalFloodCells Water;
    private bool summaryDirty=true,knownDry=true;
    private float summaryTotal,summaryPort,summaryStarboard,summaryLong;
    public readonly float[] RightSill,ForwardSill;
    private readonly float[] delta,centerX,centerZ,worldColumn;
    public NavalFloodGrid(int columns, int rows, float width, float length, float scale, float maximumDepth = float.MaxValue,bool tapered=false)
    {
        Columns=columns; Rows=rows; Width=width; Length=length; Scale=scale;
        CellWidth=width/columns; CellLength=length/rows;
        CellArea=CellWidth*CellLength*scale*scale;
        MaxCellVolume=maximumDepth*CellArea*scale;
        water=new float[columns*rows];Water=new NavalFloodCells(water); delta=new float[water.Length];
        centerX=new float[water.Length];centerZ=new float[water.Length];worldColumn=new float[columns];
        for(int i=0;i<water.Length;i++){centerX[i]=((i%columns)+.5f)*CellWidth-width*.5f;centerZ[i]=((i/columns)+.5f)*CellLength-length*.5f;}
        if(tapered){activeCells=new bool[water.Length];for(int i=0;i<water.Length;i++)activeCells[i]=Math.Abs(centerX[i])<NavalHullOutline.HalfWidth(centerZ[i],true);}
        RightSill=new float[water.Length];ForwardSill=new float[water.Length];
        for(int i=0;i<water.Length;i++){RightSill[i]=ForwardSill[i]=float.NegativeInfinity;}
    }
    private void RefreshSummary()
    {
        if(!summaryDirty)return;
        float total=0,port=0,starboard=0,longitudinal=0;
        for(int i=0;i<water.Length;i++)
        {
            float value=water[i];total+=value;
            if(i%Columns<Columns/2)port+=value;else starboard+=value;
            longitudinal+=value*Z(i)/(Length*.5f);
        }
        knownDry=total<=0f;summaryTotal=total;summaryPort=port;summaryStarboard=starboard;summaryLong=longitudinal;summaryDirty=false;
    }
    public float Total {get{RefreshSummary();return summaryTotal;}}
    public float Side(bool port){RefreshSummary();return port?summaryPort:summaryStarboard;}
    public float LongitudinalMoment {get{RefreshSummary();return summaryLong;}}
    public int Index(float x,float z)
    {
        int col=Math.Max(0,Math.Min(Columns-1,(int)Math.Floor((x+Width*0.5f)/CellWidth)));
        int row=Math.Max(0,Math.Min(Rows-1,(int)Math.Floor((z+Length*0.5f)/CellLength)));
        if(activeCells!=null)for(int n=0;n<Columns&&!activeCells[row*Columns+col];n++){if(col<Columns/2)col++;else col--;}
        return row*Columns+col;
    }
    public float X(int i) { return centerX[i]; }
    public float Z(int i) { return centerZ[i]; }
    public void FillFloorHeights(float[] output,float slopeX,float slopeZ,float floorTerm,float translationY)
    {
        if(output==null||output.Length<water.Length)throw new ArgumentException("Floor buffer is too small");
        for(int x=0;x<Columns;x++)worldColumn[x]=slopeX*centerX[x];
        for(int z=0;z<Rows;z++)
        {
            int row=z*Columns;float worldZ=slopeZ*centerZ[row];
            for(int x=0;x<Columns;x++)output[row+x]=worldColumn[x]+floorTerm+worldZ+translationY;
        }
    }
    public bool IsActive(int i){return activeCells==null||activeCells[i];}
    private NavalFloodStorage storage;
    private float[] storageDepths,storageCapacity;
    public void ConfigureStorage(NavalShipDefinition ship)
    {if(ship.DeckCount>2){storage=new NavalFloodStorage(this,ship);storageDepths=new float[water.Length];storageCapacity=new float[water.Length];
      for(int i=0;i<water.Length;i++)storageCapacity[i]=VolumeAtDepth(i,MaxCellVolume/(CellArea*Scale));}}
    public float VolumeAtDepth(int i,float depth)
    {return IsActive(i)?(storage!=null?storage.FreeDepth(i,depth):depth)*CellArea*Scale:0;}
    public float CenterDepth(int i){float depth=Depth(i);return storage!=null?storage.Center(i,depth):depth*.5f;}
    public float CellCapacity(int i){return !IsActive(i)?0:storageCapacity!=null?storageCapacity[i]:MaxCellVolume;}
    public float Depth(int i) { float depth=water[i]/(CellArea*Scale);return storage!=null?storage.Depth(i,depth):depth; }
    public float Add(int i,float amount)
    { float actual=Math.Max(-water[i],Math.Min(CellCapacity(i)-water[i],amount)); water[i]+=actual;if(actual!=0){summaryDirty=true;if(actual>0)knownDry=false;} return actual; }
    public void Pump(float amount)
    {
        float total=Total; if(total<=0f)return;
        float fraction=Math.Max(0f,1f-amount/total);
        for(int i=0;i<water.Length;i++)water[i]*=fraction;
        if(fraction!=1)summaryDirty=true;
        if(fraction==0f)knownDry=true;
    }
    public void Spread(float dt,float[] floorHeights,float upCosine)
    {
        if(knownDry)return;
        summaryDirty=true;
        int steps=Math.Max(1,(int)Math.Ceiling(dt/0.02f));
        float step=dt/steps;
        for(int s=0;s<steps;s++)
        {
            Array.Clear(delta,0,delta.Length);
            if(storageDepths!=null)for(int i=0;i<water.Length;i++)storageDepths[i]=Depth(i)*Scale;
            for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++)
            {
                int a=z*Columns+x;
                if(x+1<Columns)Exchange(a,a+1,CellLength*Scale,step,floorHeights,upCosine,RightSill[a]);
                if(z+1<Rows)Exchange(a,a+Columns,CellWidth*Scale,step,floorHeights,upCosine,ForwardSill[a]);
            }
            for(int i=0;i<water.Length;i++)water[i]=Math.Max(0f,water[i]+delta[i]);
        }
    }
    private void Exchange(int a,int b,float edge,float dt,float[] floor,float up,float sill)
    {
        if(!IsActive(a)||!IsActive(b)||float.IsPositiveInfinity(sill)||(water[a]<=0f&&water[b]<=0f))return;
        up=Math.Max(0.2f,up);
        float headA=floor[a]+(storageDepths!=null?storageDepths[a]:water[a]/CellArea)*up, headB=floor[b]+(storageDepths!=null?storageDepths[b]:water[b]/CellArea)*up;
        int from=headA>headB?a:b, to=from==a?b:a;
        float high=Math.Max(headA,headB), low=Math.Max(Math.Min(headA,headB),Math.Max(sill,Math.Max(floor[a],floor[b])));
        float difference=Math.Max(0f,high-low);
        if(difference<=0f||water[from]<=0f)return;
        float depth=Math.Min(storageDepths!=null?storageDepths[from]:water[from]/CellArea,difference);
        float amount=0.9f*edge*depth*(float)Math.Sqrt(19.62f*difference)*dt;
        // At most four neighbours: these bounds preserve positivity even when
        // all edges request water from the same cell in a synchronous step.
        amount=Math.Min(amount,Math.Min(water[from]*0.249f,difference*CellArea/up*0.249f));
        amount=Math.Min(amount,Math.Max(0f,CellCapacity(to)-water[to])*0.249f);
        delta[from]-=amount; delta[to]+=amount;
    }
}

// Read-only indexed view: mutation is confined to Add/Pump/Spread so summary invalidation is guaranteed.
public sealed class NavalFloodCells
{
    private readonly float[] cells;
    internal NavalFloodCells(float[] source){cells=source;}
    public int Length {get{return cells.Length;}}
    public float this[int index] {get{return cells[index];}}
    public Enumerator GetEnumerator(){return new Enumerator(cells);}
    public struct Enumerator
    {
        private readonly float[] values;private int index;
        internal Enumerator(float[] source){values=source;index=-1;}
        public bool MoveNext(){return ++index<values.Length;}
        public float Current {get{return values[index];}}
    }
}
