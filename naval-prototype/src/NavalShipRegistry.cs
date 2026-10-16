using System.Collections.Generic;

// Ship transforms and their controlling components need not share a GameObject.
// Resolve the exact registered hull, not an arbitrary parent or sibling component.
public class NavalShipRegistry<TKey,TValue> where TValue:class
{
    private readonly Dictionary<TKey,TValue> entries=new Dictionary<TKey,TValue>();
    public void Register(TKey ship,TValue controls){entries[ship]=controls;}
    public bool TryGet(TKey ship,out TValue controls){return entries.TryGetValue(ship,out controls);}
    public void Remove(TKey ship,TValue controls)
    {
        TValue current;
        if(entries.TryGetValue(ship,out current)&&object.ReferenceEquals(current,controls))entries.Remove(ship);
    }
}
