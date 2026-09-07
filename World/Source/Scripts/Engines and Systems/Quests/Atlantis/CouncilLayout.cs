using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.Atlantis
{
    public sealed class CouncilPlacement
    {
        public string Key,Name;
        public int Art,Hue,Role=-1;
        public Point3D Location;
        public Rectangle2D Room;
        public CouncilPlacement(string key,string name,int art,int hue,int x,int y,int z,int rx,int ry,int width,int height)
        { Key=key; Name=name; Art=art; Hue=hue; Location=new Point3D(x,y,z); Room=new Rectangle2D(rx,ry,width,height); }
    }
    public static class CouncilLayout
    {
        public static List<CouncilPlacement> Definitions()
        {
            List<CouncilPlacement> p=new List<CouncilPlacement>();
            p.Add(new CouncilPlacement("shelf0","archive: the public proclamation",0xA97,2764,4323,2525,-127,4320,2521,31,13));
            p.Add(new CouncilPlacement("shelf1","archive: the harbor register",0xA98,2764,4335,2525,-127,4320,2521,31,13));
            p.Add(new CouncilPlacement("shelf2","archive: the sealed evacuation order",0xA99,2764,4347,2525,-127,4320,2521,31,13));
            p.Add(new CouncilPlacement("truth","lectern of the disputed record",0x2D4B,2722,4330,2531,-127,4320,2521,31,13));
            p.Add(new CouncilPlacement("keepsake","Mira's waterlogged music box",0x2AF9,2767,4448,2669,-127,4440,2665,15,7));
            p.Add(new CouncilPlacement("standard","the folded watch standard",0x175E,2831,4368,2567,-127,4353,2553,22,22));
            p.Add(new CouncilPlacement("oath","the watch's oath stone",0xED4,2748,4332,2567,-127,4320,2553,24,22));
            p.Add(new CouncilPlacement("memorial","the memorial of the unnamed",0xED4,2826,4419,2616,-127,4416,2607,7,16));
            p.Add(new CouncilPlacement("council","the Last Council's seal",0x1F14,2319,4366,2617,-127,4360,2609,18,14));
            p.Add(new CouncilPlacement("return","conch of return",0xFC4,2319,3263,3458,-20,3257,3452,10,11));
            p.Add(new CouncilPlacement("wardtruth","TRUTH: the broken record",0xFBE,2319,3266,3467,-20,3262,3463,8,9));
            p.Add(new CouncilPlacement("wardlove","LOVE: the witness's binding anchor",0x897,2319,3282,3464,-20,3278,3460,9,9));
            p.Add(new CouncilPlacement("wardcourage","COURAGE: the watch's rally stone",0xED4,2319,3278,3480,-20,3274,3476,9,9));
            // Low scenery fits below the water. Never lay tall columns through the overhead surface.
            int[,] rubble={ {3260,3472},{3264,3484},{3287,3477},{3287,3456},{3274,3454},{3259,3480} };
            for(int i=0;i<rubble.GetLength(0);i++) p.Add(new CouncilPlacement("ruin"+i,"a fragment of the council's monument",0x1363,2827,rubble[i,0],rubble[i,1],-20,rubble[i,0]-2,rubble[i,1]-2,5,5));
            int[,] citizens={ {4436,2642,4432,2637,23,10},{4339,2529,4320,2521,31,13},{4326,2558,4320,2553,24,22},
                {4331,2588,4322,2585,29,6},{4418,2564,4412,2562,23,4},{4412,2548,4400,2537,23,22} };
            for(int i=0;i<citizens.GetLength(0);i++)
            {
                CouncilPlacement s=new CouncilPlacement("citizen"+i,"Atlantis citizen spawner",0x1F13,0,citizens[i,0],citizens[i,1],-127,citizens[i,2],citizens[i,3],citizens[i,4],citizens[i,5]);
                s.Role=i; p.Add(s);
            }
            return p;
        }
        public static List<CouncilPlacement> Plan(AtlantisCouncilController c)
        {
            if(Map.Atlantis==null || !Map.Atlantis.Tiles.Exists) throw new InvalidOperationException("Atlantis map files are unavailable.");
            List<CouncilPlacement> result=new List<CouncilPlacement>(); List<Point3D> reserved=new List<Point3D>();
            foreach(CouncilPlacement p in Definitions())
            {
                Item existing=c==null?null:c.Node(p.Key);
                if(existing!=null)
                {
                    if(existing.Map!=Map.Atlantis || !p.Room.Contains(existing.Location)) throw new InvalidOperationException("Existing "+p.Key+" was moved out of its room. Restore it before setup.");
                    reserved.Add(existing.Location); continue;
                }
                Point3D pos;
                int height=p.Role>=0?16:Math.Max(16,TileData.ItemTable[p.Art].CalcHeight);
                if(!FindClear(p.Location,p.Room,height,reserved,out pos)) throw new InvalidOperationException("No unobstructed placement for "+p.Key+" in its room. No placements were made.");
                p.Location=pos; reserved.Add(pos); result.Add(p);
            }
            return result;
        }
        public static bool FindClear(Point3D center,Rectangle2D room,int height,List<Point3D> reserved,out Point3D found)
        {
            int radius=Math.Max(room.Width,room.Height);
            for(int r=0;r<=radius;r++) for(int dx=-r;dx<=r;dx++) for(int dy=-r;dy<=r;dy++)
            {
                if(Math.Max(Math.Abs(dx),Math.Abs(dy))!=r) continue;
                int x=center.X+dx,y=center.Y+dy,z=center.Z;
                if(!room.Contains(new Point2D(x,y))) continue;
                if(z>-100) { z=Map.Atlantis.GetAverageZ(x,y); if(z != -20 && z != -19) continue; }
                Point3D p=new Point3D(x,y,z); bool used=false;
                foreach(Point3D q in reserved) if(q.X==x && q.Y==y && Math.Abs(q.Z-z)<20) used=true;
                if(used || !Map.Atlantis.CanFit(x,y,z,height,true,true,true) || Occupied(p)) continue;
                // Preserve a way to approach the furnishing. Two cardinal neighbors must fit a person.
                int open=0; int[] ox={-1,1,0,0},oy={0,0,-1,1};
                for(int n=0;n<4;n++)
                {
                    int nz=z>-100?Map.Atlantis.GetAverageZ(x+ox[n],y+oy[n]):z;
                    if(Math.Abs(nz-z)<=2 && Map.Atlantis.CanFit(x+ox[n],y+oy[n],nz,16,true,false,true)) ++open;
                }
                if(open<2) continue;
                found=p; return true;
            }
            found=Point3D.Zero; return false;
        }
        private static bool FloorArt(int id,ItemData d)
        {
            return d.Surface && d.CalcHeight==0 && (id<0x600 || (id>=0xAEE && id<=0xAFA) || (id>=0x56B8 && id<=0x56E2));
        }
        public static bool Occupied(Point3D p)
        {
            foreach(StaticTile s in Map.Atlantis.Tiles.GetStaticTiles(p.X,p.Y,true))
            {
                if(s.Z<p.Z-2 || s.Z>=p.Z+35) continue;
                ItemData d=TileData.ItemTable[s.ID];
                // The seabed's water is overhead, not a floor decoration to replace.
                if(p.Z>-100 && s.Z>=0) continue;
                if(!FloorArt(s.ID,d)) return true;
            }
            IPooledEnumerable e=Map.Atlantis.GetItemsInRange(p,0);
            try { foreach(Item i in e) if(i.Z>=p.Z-2 && i.Z<p.Z+35 && i.Visible && !FloorArt(i.ItemID,i.ItemData)) return true; }
            finally { e.Free(); }
            return false;
        }
        public static bool FindArrival(Point3D center,out Point3D p)
        {
            return FindClear(center,new Rectangle2D(center.X-4,center.Y-4,9,9),16,new List<Point3D>(),out p);
        }
    }
}
