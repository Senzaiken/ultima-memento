// Isolated integration checks against the real MUL files; never load or save a live world.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Atlantis;
using Server.Network;
using Server.Gumps;

public static class AtlantisCouncilTests
{
    private static string Root;
    private static int Checks;
    public static int Main(string[] args)
    {
        Root=Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e)
        { if(new AssemblyName(e.Name).Name=="World") return Assembly.LoadFrom(Path.Combine(Root,"World.exe")); return null; };
        try { Run(); Console.WriteLine("Passed "+Checks+" council checks."); return 0; }
        catch(Exception ex) { Console.WriteLine(ex); return 1; }
    }
    private static void Check(bool ok,string text) { if(!ok) throw new Exception("FAIL: "+text); ++Checks; Console.WriteLine("PASS: "+text); }
    private static void Approach(Mobile m,Item item)
    {
        Point3D p;
        if(!CouncilLayout.FindArrival(item.Location,out p)) throw new Exception("No arrival for "+item.Name);
        m.MoveToWorld(p,Map.Atlantis);
        if(!m.InRange(item.Location,3)) throw new Exception("Arrival not in interaction range: "+item.Name);
    }
    private static int Count<T>() { int n=0; foreach(Mobile m in World.Mobiles.Values) if(m is T && !m.Deleted) ++n; return n; }
    private static AtlantisWitness Witness(Mobile guide)
    { foreach(Mobile m in World.Mobiles.Values) { AtlantisWitness w=m as AtlantisWitness; if(w!=null && !w.Deleted && w.Guide==guide) return w; } return null; }
    private static void Read(Mobile m,AtlantisCouncilController c,string key) { Item i=c.Node(key); Approach(m,i); i.OnDoubleClick(m); }
    private static List<Direction> WalkingRoute(Mobile m,Point3D goal)
    {
        Point3D start=m.Location;
        Queue<Point3D> queue=new Queue<Point3D>(); queue.Enqueue(start);
        Dictionary<Point3D,Point3D> parents=new Dictionary<Point3D,Point3D>();
        Dictionary<Point3D,Direction> steps=new Dictionary<Point3D,Direction>(); parents[start]=start;
        Point3D end=Point3D.Zero; Rectangle2D bounds=new Rectangle2D(4358,2580,114,110);
        while(queue.Count>0)
        {
            Point3D p=queue.Dequeue();
            if(Utility.InRange(p,goal,1) && Math.Abs(p.Z-goal.Z)<=2) { end=p; break; }
            foreach(Direction d in new Direction[]{Direction.North,Direction.East,Direction.South,Direction.West})
            {
                int x=p.X,y=p.Y,z;
                Server.Movement.Movement.Offset(d,ref x,ref y);
                if(!bounds.Contains(new Point2D(x,y)) || !Server.Movement.Movement.CheckMovement(m,Map.Atlantis,p,d,out z) || z> -100) continue;
                Point3D q=new Point3D(x,y,z); if(parents.ContainsKey(q)) continue;
                parents[q]=p; steps[q]=d; queue.Enqueue(q);
            }
        }
        if(end==Point3D.Zero) throw new Exception("No legal walking route after "+parents.Count+" reachable cells from "+start+" to "+goal);
        List<Direction> path=new List<Direction>(); for(Point3D p=end;p!=start;p=parents[p]) path.Add(steps[p]); path.Reverse(); return path;
    }
    private static void Run()
    {
        Core.Assembly=typeof(Item).Assembly;
        Directory.SetCurrentDirectory(Root);
        Core.DataDirectories.Add(Path.Combine(Root,"Data/Files"));
        typeof(World).GetField("m_Items",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Dictionary<Serial,Item>());
        typeof(World).GetField("m_Mobiles",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new Dictionary<Serial,Mobile>());
        Server.Misc.MapDefinitions.Configure();
        Server.Movement.MovementImpl.Configure();
        Corpse.Initialize();
        List<CouncilPlacement> plan=CouncilLayout.Plan(null);
        Check(plan.Count==25,"all 25 furnishings, arena objects and citizen spawners fit the MUL map");
        Check(World.Items.Count==0 && World.Mobiles.Count==0,"dry run creates no entities");
        // Include the current authored city, not just the underlying map's empty rooms.
        Type decor=typeof(Server.Commands.AtlantisDecor);
        object groups=decor.GetMethod("ReadManifest",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Path.Combine(Root,"Data/Atlantis/Decorations.json")});
        decor.GetMethod("ApplyPreview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Map.Atlantis,groups,"all",false});
        int decorationCount=World.Items.Count;
        Check(decorationCount>4000,"test world includes the current furnished Atlantis preview");
        plan=CouncilLayout.Plan(null);
        Check(plan.Count==25,"all encounter placements also fit around current furniture");
        foreach(CouncilPlacement place in plan) Console.WriteLine("PLACEMENT "+place.Key+" "+place.Location);
        AtlantisCouncilController c=new AtlantisCouncilController(); c.Install(plan);
        Check(CouncilLayout.Plan(c).Count==0,"repeated setup is idempotent");
        int citizens=0;
        foreach(Mobile m in World.Mobiles.Values) if(m is AtlantisCitizen) citizens++;
        Check(citizens==6,"six named citizens spawn");
        int mobileCount=World.Mobiles.Count; c.Pulse(); c.Install(CouncilLayout.Plan(c));
        Check(World.Mobiles.Count==mobileCount,"spawner polling and repeated setup do not duplicate citizens");
        foreach(Mobile m in new List<Mobile>(World.Mobiles.Values))
        {
            AtlantisCitizen npc=m as AtlantisCitizen; if(npc==null) continue;
            Check(npc.Alive && npc.Blessed && npc.IsInvulnerable && npc.GetBuyInfo().Length==2,"spectral merchant "+npc.Name+" is alive for trade, protected, and stocked");
            foreach(IBuyItemInfo info in npc.GetBuyInfo())
            {
                Item stock=(Item)((GenericBuyInfo)info).GetEntity();
                Check(npc.GetSellInfo()[0].IsSellable(stock),npc.Name+" buys the type of supply it sells: "+stock.GetType().Name);
                stock.Delete();
            }
        }
        AtlantisCitizenSpawner sp=(AtlantisCitizenSpawner)c.Node("citizen1");
        sp.Citizen.Delete();
        typeof(AtlantisCitizenSpawner).GetField("m_Next",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(sp,DateTime.MinValue);
        sp.Pulse(); Check(Count<AtlantisCitizen>()==6,"a deleted citizen respawns once");

        PlayerMobile player=new PlayerMobile(); player.Player=true; player.Name="Council test investigator"; player.Body=400; player.InitStats(100,100,100); player.Hits=100;
        player.AddItem(new Backpack());
        PlayerMobile other=new PlayerMobile(); other.Player=true; other.Name="Other investigator"; other.Body=400; other.InitStats(100,100,100); other.AddItem(new Backpack());
        CouncilProgress progress=c.Progress(player);
        player.MoveToWorld(c.Node("shelf0").Location,Map.Sosaria); c.Node("shelf0").OnDoubleClick(player);
        Check(progress.Evidence==0,"shared-file Sosaria cannot interact with Atlantis clues");
        Check(!c.EnterArena(player),"unready characters cannot open the boss passage");
        Read(player,c,"shelf0"); Read(player,c,"shelf1");
        Check(progress.Evidence==3 && !c.SolveTruth(player,2),"the correct answer still requires every source document");
        Read(player,c,"shelf2");
        Check(!c.SolveTruth(player,1) && progress.Evidence==7 && !progress.Truth,"a wrong accusation preserves evidence without granting truth");
        Check(c.SolveTruth(player,2) && progress.Truth && !c.Progress(other).Truth,"a supported accusation grants personal progress only");
        // OnResponse reads only the sender; no socket or live connection is opened by this test.
        NetState reply=(NetState)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(NetState)); reply.Mobile=other;
        c.Progress(other).Evidence=7;
        CouncilGump stale=new CouncilGump(other,c,c.Node("truth"),"record","",new string[]{"one","two","three"});
        other.MoveToWorld(c.Node("truth").Location,Map.Sosaria);
        stale.OnResponse(reply,new RelayInfo(2,new int[0],new TextRelay[0]));
        Check(!c.Progress(other).Truth,"a stale clue dialog cannot submit an answer from another facet");
        Approach(other,c.Node("truth"));
        stale.OnResponse(reply,new RelayInfo(999,new int[0],new TextRelay[0]));
        Check(!c.Progress(other).Truth,"forged dialog buttons cannot grant progress");
        stale.OnResponse(reply,new RelayInfo(2,new int[0],new TextRelay[0]));
        Check(c.Progress(other).Truth,"a valid nearby dialog response resolves the puzzle");
        other.MoveToWorld(Point3D.Zero,Map.Internal); reply.Mobile=null;
        c.StartEscort(player); Check(Witness(player)==null,"the citizen needs her keepsake before an escort begins");
        Read(player,c,"keepsake"); Approach(player,c.Node("citizen0")); c.StartEscort(player); c.StartEscort(player);
        AtlantisWitness witness=Witness(player);
        Check(witness!=null && Count<AtlantisWitness>()==1,"one personal witness follows after finding the keepsake");
        witness.MoveToWorld(new Point3D(4418,2616,-127),Map.Atlantis); c.StartEscort(player);
        Check(witness.Deleted && Count<AtlantisWitness>()==1,"returning to Mira restarts a distant lost escort without duplicates");
        witness=Witness(player);
        Point3D memorialArrival;
        Check(CouncilLayout.FindArrival(c.Node("memorial").Location,out memorialArrival),"memorial has a clear approach");
        List<Direction> walk=WalkingRoute(player,memorialArrival);
        foreach(Direction d in walk) { player.SetDirection(d); if(!player.Move(d)) throw new Exception("Player blocked at "+player.Location+" moving "+d); witness.Pulse(); }
        for(int step=0;step<30;step++) witness.Pulse();
        Console.WriteLine("ESCORT guide="+player.Location+" witness="+witness.Location+" goal="+memorialArrival+" stamina="+player.Stam+" frozen="+player.Frozen);
        Check(player.InRange(memorialArrival,1) && witness.InRange(c.Node("memorial").Location,5),"player and witness path from the infirmary to the memorial through decorated rooms");
        Check(c.CompleteEscort(player) && progress.Love && witness.Deleted,"the arrived witness records testimony and cleans up its echo");
        c.StartWatch(player); Check(Count<CouncilWatchman>()==0,"the watch challenge requires remembering the standard");
        Read(player,c,"standard"); Approach(player,c.Node("oath")); c.StartWatch(player);
        Check(Count<CouncilWatchman>()==3,"raising the oath spawns two sentries and their captain");
        foreach(Mobile m in new List<Mobile>(World.Mobiles.Values)) if(m is CouncilWatchman) m.Damage(10000,player);
        Check(progress.Courage && progress.Ready && !c.Progress(other).Ready,"finishing the watch challenge unlocks only the investigator's council seal");
        BaseCreature pet=new BaseCreature(AIType.AI_Melee,FightMode.None,10,1,0.2,0.4); pet.Body=0xD9; pet.SetStr(100); pet.SetHits(1000); pet.Controlled=true; pet.ControlMaster=player; pet.ControlOrder=OrderType.Follow; pet.MoveToWorld(player.Location,Map.Atlantis);
        Check(c.EnterArena(player) && AtlantisCouncilController.InArena(player) && c.Herald!=null,"the solved council leads to the seabed and starts a Herald fight");
        Check(AtlantisCouncilController.InArena(pet) && c.Participant(pet),"a following pet travels with its owner and can participate");
        AtlantisHerald boss=c.Herald;
        other.MoveToWorld(player.Location,Map.Atlantis);
        int hp=boss.Hits; boss.Damage(20,other);
        Check(boss.Hits==hp,"an unqualified bystander cannot damage the Herald");
        boss.BeginDecree(1); boss.Damage(20,player);
        Check(boss.Hits==hp,"the false record shields the Herald before truth is invoked");
        Approach(player,c.Node("wardtruth"));
        Check(boss.UseWard(player,"wardtruth"),"an eligible nearby player can answer the truth decree");
        boss.Damage(20,player); Check(boss.Hits<hp,"answering truth opens the damage window"); boss.ResolveDecree();
        hp=boss.Hits; boss.Damage(10,pet); Check(boss.Hits<hp,"a participating pet can damage the Herald");
        pet.MoveToWorld(pet.Location,Map.Sosaria); hp=boss.Hits; boss.Damage(10,pet);
        Check(boss.Hits==hp,"a pet on the shared Sosaria geometry cannot damage the Atlantis encounter");
        pet.MoveToWorld(player.Location,Map.Atlantis);
        boss.BeginDecree(2); Approach(player,c.Node("wardlove"));
        Check(boss.UseWard(player,"wardlove"),"the love ward releases the witness's chains");
        hp=player.Hits; boss.ResolveDecree(); Check(player.Hits==hp,"answering the chains prevents their damage");
        Approach(player,c.Node("wardcourage")); boss.Combatant=player; boss.BeginDecree(3);
        Check(boss.UseWard(player,"wardcourage"),"the watch's ward establishes a rally shelter");
        hp=player.Hits; boss.ResolveDecree(); Check(player.Hits==hp,"the rally shelter protects a player in the marked surge lane");
        boss.BeginDecree(2); hp=player.Hits; boss.ResolveDecree();
        Check(player.Hits<hp,"ignoring an announced binding has a combat consequence");
        boss.Damage(100000,player);
        Check(progress.Won && !progress.RewardPending && c.Herald==null,"defeating the Herald records victory and delivers a memorial seal");
        int seals=player.Backpack.FindItemsByType(typeof(CouncilMemorialSeal),true).Length; c.Claim(player);
        Check(seals==1 && player.Backpack.FindItemsByType(typeof(CouncilMemorialSeal),true).Length==1,"claiming again cannot duplicate the reward");
        Read(player,c,"return"); Check(player.Map==Map.Atlantis && player.Z==-127,"the return conch leads back to the city");
        Check(pet.Map==Map.Atlantis && pet.Z==-127,"the return conch brings following pets back too");
        Approach(other,c.Node("return")); other.Body=402;
        c.Node("return").OnDoubleClickDead(other);
        Check(!other.Alive && other.Z==-127,"a ghost can use the return conch without completing the investigation");
        other.Body=400;
        c.ResetFight(); Check(c.EnterArena(player),"completed investigations allow another test fight");
        AtlantisHerald interrupted=c.Herald; c.Recover();
        Check(interrupted.Deleted && !AtlantisCouncilController.InArena(player) && progress.Ready,"restart recovery removes the interrupted boss and returns participants without erasing investigation progress");

        progress.RewardPending=true;
        MemoryStream stream=new MemoryStream(); BinaryFileWriter writer=new BinaryFileWriter(stream,true);
        c.Serialize(writer); writer.Flush(); stream.Position=0;
        AtlantisCouncilController restored=new AtlantisCouncilController(Serial.NewItem); World.AddItem(restored);
        restored.Deserialize(new BinaryFileReader(new BinaryReader(stream)));
        Check(restored.Progress(player).Ready && restored.Progress(player).Won && restored.Progress(player).RewardPending && restored.Node("shelf0")==c.Node("shelf0"),"controller serialization preserves progress, unclaimed rewards and owned object references");
        progress.RewardPending=false;
        restored.Delete(); stream.Dispose();
        Check(!c.Node("shelf0").Deleted,"cleanup checks ownership before deleting referenced scenery");
        AtlantisCitizen savedCitizen=((AtlantisCitizenSpawner)c.Node("citizen1")).Citizen;
        stream=new MemoryStream(); writer=new BinaryFileWriter(stream,true); savedCitizen.Serialize(writer); writer.Flush(); stream.Position=0;
        AtlantisCitizen loadedCitizen=new AtlantisCitizen(Serial.NewMobile); World.AddMobile(loadedCitizen);
        loadedCitizen.Deserialize(new BinaryFileReader(new BinaryReader(stream)));
        Check(loadedCitizen.Role==1 && loadedCitizen.Council==c && loadedCitizen.Alive && loadedCitizen.Blessed && loadedCitizen.Hue==0x4001 && ((GenericBuyInfo)loadedCitizen.GetBuyInfo()[0]).Type==typeof(BlankScroll),"citizen serialization restores identity, ownership, ghost appearance and merchant stock");
        loadedCitizen.Delete(); stream.Dispose();
        c.ResetInvestigation(player);
        Check(!c.Progress(player).Ready && c.Progress(player).Won,"resetme clears the investigation without renewing reward eligibility");

        Item moved=c.Node("shelf0"); moved.MoveToWorld(moved.Location,Map.Sosaria);
        int before=World.Items.Count; bool refused=false;
        try { CouncilLayout.Plan(c); } catch(InvalidOperationException) { refused=true; }
        Check(refused && World.Items.Count==before,"setup rejects a moved clue without duplicating or mutating placements");
        Static unrelated=new Static(0xF7A); unrelated.MoveToWorld(new Point3D(3275,3470,-20),Map.Sosaria);
        Console.WriteLine(c.Status());
        c.Delete();
        Check(moved.Deleted && !unrelated.Deleted && Count<AtlantisCitizen>()==0 && Count<AtlantisHerald>()==0,"removal follows owned entities across maps and preserves unrelated items");
        int remainingDecor=0; foreach(Item i in World.Items.Values) if(i is AtlantisPreviewItem) ++remainingDecor;
        Check(remainingDecor==decorationCount,"removal preserves every authored city preview item");
        Check(player.Backpack.FindItemsByType(typeof(CouncilMemorialSeal),true).Length==1,"removal preserves the player's earned souvenir");
    }
}
