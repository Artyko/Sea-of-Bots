using System;
class FloodRenderChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
        {
            var grid=new NavalFloodGrid(12,24,BoatDimensions.HoldWidth,BoatDimensions.HoldLength,ship.Scale,ship.RimY-ship.HoldFloor);
            var view=new NavalFloodRenderState(grid.Water.Length);
            Check(!view.Refresh(grid)&&view.WetCount==0,"Dry ship needs no mesh upload");
            float volume=grid.CellArea*ship.Scale*.4f;grid.Add(17,volume);
            Check(view.Refresh(grid)&&view.ChangedCount==1&&view.ChangedCells[0]==17&&view.WetCount==1,"Only newly wet cell changes");
            Check(Math.Abs(view.Depths[17]-.4f)<.00001f,"Mesh uses local depth at each ship scale");
            Check(!view.Refresh(grid),"Stable water needs no mesh upload");
            grid.Add(17,grid.CellArea*ship.Scale*.00005f);
            Check(!view.Refresh(grid),"Sub-threshold change defers cosmetic upload");
            grid.Add(17,grid.CellArea*ship.Scale*.0001f);
            Check(view.Refresh(grid),"Deferred depth changes accumulate");
            grid.Add(18,volume);Check(view.Refresh(grid)&&view.WetCount==2,"Neighbour has separate water column");
            float before=grid.Total;view.Refresh(grid);Check(grid.Total==before,"Rendering never modifies hydraulic mass");
            grid.Pump(grid.Total);Check(view.Refresh(grid)&&view.WetCount==0&&view.Depths[17]==0f&&view.Depths[18]==0f,"Drained columns collapse and renderer can hide");
            grid.Add(0,grid.MaxCellVolume);Check(view.Refresh(grid)&&view.WetCount==1,"Flood can reappear after draining");
            Check(view.Depths[0]<=ship.RimY-ship.HoldFloor+.00001f,"Mesh stays within maximum flood bounds");
        }
        Console.WriteLine("PASS: dry/stable upload suppression, local depth, changed cells, cumulative threshold, mass conservation, draining and restoration on both ships.");
    }
}
