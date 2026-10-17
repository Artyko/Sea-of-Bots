using System;

public interface INavalBufferQuery<T> {int Execute(T[] buffer);}
// A full NonAlloc result is ambiguous: grow and retry, never silently truncate.
public sealed class NavalQueryBuffer<T>
{
    public T[] Items {get;private set;}
    public int Count {get;private set;}
    public NavalQueryBuffer(int capacity)
    {if(capacity<1)throw new ArgumentOutOfRangeException("capacity");Items=new T[capacity];}
    public int Fill<TQuery>(TQuery query) where TQuery:struct,INavalBufferQuery<T>
    {
        Count=0;
        while(true)
        {
            int count=query.Execute(Items);
            if(count<0||count>Items.Length)throw new InvalidOperationException("Invalid query result count");
            Count=count;
            if(count<Items.Length)return count;
            Items=new T[checked(Items.Length*2)];
        }
    }
    public void Clear(){Array.Clear(Items,0,Count);Count=0;}
}
