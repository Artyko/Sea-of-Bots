using System;
class QueryBufferChecks
{
    sealed class Hit {public int Id;}
    sealed class Counter {public int Calls;}
    struct Query:INavalBufferQuery<Hit>
    {
        public Hit[] Results;public Counter Counter;
        public int Execute(Hit[] buffer)
        {
            Counter.Calls++;
            // Native query order is unspecified and may change when the buffer grows.
            int count=Math.Min(Results.Length,buffer.Length);
            for(int i=0;i<count;i++)buffer[i]=Results[Results.Length-1-i];
            return count;
        }
    }
    struct BadQuery:INavalBufferQuery<Hit>
    {public int Execute(Hit[] buffer){return buffer.Length+1;}}
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        NavalQueryBuffer<Hit> buffer=new NavalQueryBuffer<Hit>(4);Random random=new Random(8823);
        for(int scenario=0;scenario<500;scenario++)
        {
            int count=scenario==0?4:scenario==1?65:random.Next(0,800);
            Hit[] oracle=new Hit[count];for(int i=0;i<count;i++)oracle[i]=new Hit{Id=i};
            Counter counter=new Counter();int actual=buffer.Fill(new Query{Results=oracle,Counter=counter});
            Check(actual==count&&buffer.Count==count,"all results survive full-buffer retry");
            Check(buffer.Items.Length>count,"full result always retried, including exact capacity");
            bool[] seen=new bool[count];
            for(int i=0;i<actual;i++){Check(!seen[buffer.Items[i].Id],"no duplicate hits");seen[buffer.Items[i].Id]=true;}
            foreach(bool value in seen)Check(value,"late support candidate is never lost");
            Hit[] previous=buffer.Items;buffer.Clear();Check(buffer.Count==0,"count reset");
            foreach(Hit hit in previous)Check(hit==null,"no references retained after query");
            buffer.Fill(new Query{Results=oracle,Counter=counter});
            Check(object.ReferenceEquals(previous,buffer.Items),"steady-state scratch buffer reused");buffer.Clear();
        }
        bool rejected=false;try{buffer.Fill(new BadQuery());}catch(InvalidOperationException){rejected=true;}
        Check(rejected,"invalid provider cannot corrupt buffer");
        rejected=false;try{new NavalQueryBuffer<Hit>(0);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"zero capacity rejected");
        Console.WriteLine("PASS: 500 unsorted sparse/dense query results, exact/full retries, late support retention, reference clearing and buffer reuse.");
    }
}
