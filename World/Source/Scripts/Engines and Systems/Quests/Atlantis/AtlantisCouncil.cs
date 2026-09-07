using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Atlantis
{
    public sealed class CouncilProgress
    {
        public int Evidence;
        public bool Truth, Keepsake, Love, Standard, Courage, Won, RewardPending;
        public bool Ready { get { return Truth && Love && Courage; } }
        public void Write(GenericWriter w)
        {
            w.Write(Evidence); w.Write(Truth); w.Write(Keepsake); w.Write(Love);
            w.Write(Standard); w.Write(Courage); w.Write(Won); w.Write(RewardPending);
        }
        public static CouncilProgress Read(GenericReader r)
        {
            return new CouncilProgress { Evidence = r.ReadInt(), Truth = r.ReadBool(), Keepsake = r.ReadBool(),
                Love = r.ReadBool(), Standard = r.ReadBool(), Courage = r.ReadBool(), Won = r.ReadBool(), RewardPending = r.ReadBool() };
        }
    }

    // Only these encounter types can be removed by the setup controller.
    public interface ICouncilOwned { AtlantisCouncilController Council { get; } }

    public static class AtlantisCouncilCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("AtlantisCouncil", AccessLevel.Administrator, Command);
            CommandSystem.Register("AtlantisJournal", AccessLevel.Player, Journal);
        }
        public static AtlantisCouncilController Find()
        {
            foreach (Item i in World.Items.Values)
                if (i is AtlantisCouncilController && !i.Deleted) return (AtlantisCouncilController)i;
            return null;
        }
        private static void Journal(CommandEventArgs e)
        {
            AtlantisCouncilController c = Find();
            if (c == null) e.Mobile.SendMessage("The voices of Atlantis are silent.");
            else CouncilGump.Journal(e.Mobile, c);
        }
        private static void Command(CommandEventArgs e)
        {
            string verb = e.Length == 0 ? "help" : e.GetString(0).ToLowerInvariant();
            AtlantisCouncilController c = Find();
            try
            {
                if (verb == "check" || verb == "setup")
                {
                    List<CouncilPlacement> plan = CouncilLayout.Plan(c);
                    if (verb == "check") { e.Mobile.SendMessage("Atlantis council: {0} new placements fit. No changes made.", plan.Count); return; }
                    bool created = c == null;
                    if (created) c = new AtlantisCouncilController();
                    try { c.Install(plan); }
                    catch { if (created) c.Delete(); throw; }
                    e.Mobile.SendMessage("Atlantis council: installed {0} missing placements. The six citizens are spread across the city; go citizens visits Mira only.", plan.Count);
                    foreach(string line in c.CitizenStatus()) e.Mobile.SendMessage(line);
                    return;
                }
                if (c == null) { e.Mobile.SendMessage("Use [AtlantisCouncil check, then [AtlantisCouncil setup."); return; }
                if (verb == "status")
                {
                    e.Mobile.SendMessage(c.Status());
                    foreach(string line in c.CitizenStatus()) e.Mobile.SendMessage(line);
                    return;
                }
                if (verb == "remove") { c.Delete(); e.Mobile.SendMessage("Removed the council encounter and its owned entities. Existing city decorations remain."); return; }
                if (verb == "resetfight") { c.ResetFight(); e.Mobile.SendMessage("Fight reset; investigation progress retained."); return; }
                if (verb == "resetme") { c.ResetInvestigation(e.Mobile); e.Mobile.SendMessage("Your investigation was reset. Previously earned souvenir eligibility is retained."); return; }
                if (verb == "go")
                {
                    string key = e.Length > 1 ? e.GetString(1).ToLowerInvariant() : "citizens";
                    string node = VisitNode(key);
                    if (node == null) { e.Mobile.SendMessage("Visits: Mira, Edrin, Orin, Neris, Selene, Tovan; or citizens, archive, watch, council, arena."); return; }
                    Item target = c.Node(node);
                    if (target == null) throw new InvalidOperationException("That placement is missing; run setup to repair it.");
                    if(target.Map!=Map.Atlantis) throw new InvalidOperationException("That placement was moved off Atlantis; restore it before visiting.");
                    Point3D location=target.Location;
                    AtlantisCitizenSpawner spawner=target as AtlantisCitizenSpawner;
                    if(spawner!=null)
                    {
                        AtlantisCitizen npc=spawner.Citizen;
                        if(npc==null || npc.Deleted) throw new InvalidOperationException("That citizen has not spawned. Check status for its spawner location and allow clear floor for the next spawn attempt.");
                        if(npc.Map!=Map.Atlantis) throw new InvalidOperationException("That citizen was moved off Atlantis. Its spawner will attempt to return it.");
                        location=npc.Location;
                    }
                    Point3D p;
                    if (!CouncilLayout.FindArrival(location, out p)) throw new InvalidOperationException("No clear arrival tile near " + node);
                    e.Mobile.MoveToWorld(p, Map.Atlantis); return;
                }
                e.Mobile.SendMessage("[AtlantisCouncil check | setup | status | go <citizen name or area> | resetfight | resetme | remove");
                e.Mobile.SendMessage("Citizens: Mira, Edrin, Orin, Neris, Selene, Tovan. Areas: citizens (Mira), archive, watch, council, arena.");
                e.Mobile.SendMessage("[AtlantisJournal opens your investigation. Setup is explicit and repeatable; nothing is installed at startup.");
            }
            catch (Exception ex) { e.Mobile.SendMessage("Atlantis council: {0}", ex.Message); Console.WriteLine("AtlantisCouncil: {0}", ex); }
        }
        public static string VisitNode(string key)
        {
            if(key==null) return null;
            key=key.ToLowerInvariant();
            for(int role=0;role<AtlantisCitizen.Names.Length;role++)
                if(String.Equals(key,AtlantisCitizen.Names[role],StringComparison.OrdinalIgnoreCase)) return "citizen"+role;
            return key=="arena"?"return":key=="archive"?"truth":key=="watch"?"oath":key=="council"?"council":key=="citizens"?"citizen0":null;
        }
    }

    public class AtlantisCouncilController : Item
    {
        private List<Item> m_Items = new List<Item>();
        private List<Mobile> m_Mobiles = new List<Mobile>();
        private Dictionary<Mobile, CouncilProgress> m_Progress = new Dictionary<Mobile, CouncilProgress>();
        private List<Mobile> m_Participants = new List<Mobile>();
        private Dictionary<Mobile, DateTime> m_Watch = new Dictionary<Mobile, DateTime>();
        private Timer m_Timer;
        private CouncilArenaRegion m_Region;
        private DateTime m_EmptySince, m_NextFight;
        private bool m_Active;
        public AtlantisHerald Herald;
        public static readonly Rectangle3D Arena = new Rectangle3D(3255,3450,-25,41,41,25);
        public override bool Decays { get { return false; } }
        public AtlantisCouncilController() : base(0x1F14) { Name = "Atlantis Last Council controller"; Visible = false; Movable = false; }
        public AtlantisCouncilController(Serial s) : base(s) { }
        public bool Active { get { return !Deleted && m_Active; } }
        public CouncilProgress Progress(Mobile m)
        {
            CouncilProgress p;
            if (!m_Progress.TryGetValue(m, out p)) { p = new CouncilProgress(); m_Progress.Add(m,p); }
            return p;
        }
        public static bool InArena(Mobile m) { return m != null && !m.Deleted && m.Map == Map.Atlantis && Arena.Contains(m.Location); }
        public bool Participant(Mobile m)
        {
            BaseCreature pet = m as BaseCreature;
            if (pet != null) m = pet.ControlMaster ?? pet.SummonMaster;
            return m != null && m_Participants.Contains(m) && InArena(m);
        }
        public Item Node(string key)
        {
            foreach (Item i in m_Items)
            {
                CouncilObject o = i as CouncilObject;
                if (o != null && !o.Deleted && o.Key == key) return o;
                AtlantisCitizenSpawner s = i as AtlantisCitizenSpawner;
                if (s != null && !s.Deleted && s.Key == key) return s;
            }
            return null;
        }
        public void Own(Item i) { m_Items.Add(i); }
        public void Own(Mobile m) { m_Mobiles.Add(m); }
        public void Install(List<CouncilPlacement> plan)
        {
            List<Item> added = new List<Item>();
            bool wasActive=m_Active;
            try
            {
                foreach (CouncilPlacement p in plan)
                {
                    Item i = p.Role >= 0 ? (Item)new AtlantisCitizenSpawner(this,p.Key,p.Role) : new CouncilObject(this,p.Key,p.Art,p.Hue,p.Name);
                    added.Add(i); Own(i); i.MoveToWorld(p.Location,Map.Atlantis);
                }
                m_Active = true; Attach();
                foreach (Item i in m_Items) { AtlantisCitizenSpawner s = i as AtlantisCitizenSpawner; if (s != null && !s.Deleted) s.Pulse(); }
            }
            catch { foreach (Item i in added) i.Delete(); m_Active=wasActive; throw; }
        }
        private void Attach()
        {
            if (!Active) return;
            if (m_Region == null) { m_Region = new CouncilArenaRegion(this); m_Region.Register(); }
            if (m_Timer == null) m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1),Pulse);
        }
        public string Status()
        {
            int items=0,mobs=0; foreach (Item i in m_Items) if (!i.Deleted) ++items;
            foreach (Mobile m in m_Mobiles) if (!m.Deleted) ++mobs;
            return String.Format("Last Council: {0} owned items, {1} NPCs/creatures, {2} investigations. Herald: {3}.",items,mobs,m_Progress.Count,Herald != null && !Herald.Deleted ? "active" : "idle");
        }
        public IEnumerable<string> CitizenStatus()
        {
            for(int role=0;role<AtlantisCitizen.Names.Length;role++)
            {
                string name=AtlantisCitizen.Names[role];
                AtlantisCitizenSpawner spawner=Node("citizen"+role) as AtlantisCitizenSpawner;
                if(spawner==null) { yield return name+": MISSING SPAWNER; run setup to repair it."; continue; }
                AtlantisCitizen npc=spawner.Citizen;
                if(npc==null || npc.Deleted)
                    yield return String.Format("{0}: NOT SPAWNED. Spawner at {1} on {2}; retries every 30 seconds when floor is clear.",name,spawner.Location,spawner.Map);
                else
                    yield return String.Format("{0}: {1} at {2} on {3}. Visit: [AtlantisCouncil go {0}",name,npc.Map==Map.Atlantis?"present":"OFF ATLANTIS",npc.Location,npc.Map);
            }
        }
        public bool CanUse(Mobile from, IEntity target, bool alive)
        {
            return Active && from != null && !from.Deleted && (!alive || from.Alive) && from.Map == Map.Atlantis
                && target != null && target.Map == Map.Atlantis && from.InRange(target.Location,3)
                && Math.Abs(from.Z-target.Z) <= 8 && from.InLOS(target);
        }
        public void Notice(Mobile from, string text) { from.SendMessage(1153,text); }
        public bool SolveTruth(Mobile from, int answer)
        {
            CouncilProgress p=Progress(from);
            if (p.Evidence != 7) { Notice(from,"Read all three archive shelves before challenging the record."); return false; }
            if (answer != 2) { Notice(from,"The documents do not support that accusation. Read them again; no evidence is lost."); return false; }
            p.Truth=true; Notice(from,"Truth remembered: the council withdrew the ships before ordering the citizens to wait."); return true;
        }
        public void StartEscort(Mobile from)
        {
            CouncilProgress p=Progress(from);
            if (p.Love) { Notice(from,"Mira's testimony is already recorded at the memorial."); return; }
            if (!p.Keepsake) { Notice(from,"Mira remembers a little music box in her household, southeast of the infirmary. Find it first."); return; }
            foreach (Mobile m in m_Mobiles)
            {
                AtlantisWitness w=m as AtlantisWitness;
                if (w == null || w.Deleted || w.Guide != from) continue;
                if(w.Map==from.Map && w.InRange(from.Location,12)) { Notice(from,"Your remembering echo is still waiting nearby. Speak to it to recall it to the last safe point."); return; }
                // Returning to Mira can restart a lost escort without waiting for its expiry.
                w.Delete();
            }
            Point3D pos;
            if (!CouncilLayout.FindArrival(from.Location,out pos)) { Notice(from,"Make a little room for the spirit."); return; }
            AtlantisWitness witness=new AtlantisWitness(this,from); Own(witness); witness.MoveToWorld(pos,Map.Atlantis);
            Notice(from,"Mira's echo will follow you. Lead her to the memorial west of the music room, and double-click it together. Stay close; she waits if you leave her behind.");
        }
        public bool CompleteEscort(Mobile from)
        {
            Item memorial=Node("memorial");
            foreach (Mobile m in m_Mobiles)
            {
                AtlantisWitness w=m as AtlantisWitness;
                if (w != null && !w.Deleted && w.Guide == from && w.Map == Map.Atlantis && w.InRange(memorial.Location,5) && Math.Abs(w.Z-memorial.Z)<8)
                { Progress(from).Love=true; w.Delete(); Notice(from,"Love remembered: Mira can speak for the citizens now. The memorial answers with a blue light."); memorial.Hue=2319; return true; }
            }
            Notice(from,"Bring Mira's remembering echo close to the memorial, then touch it again."); return false;
        }
        public void StartWatch(Mobile from)
        {
            CouncilProgress p=Progress(from);
            if (p.Courage) { Notice(from,"The watch has already accepted your oath."); return; }
            if (!p.Standard) { Notice(from,"Recover the watch standard from the barracks east of the armory first."); return; }
            if (m_Watch.ContainsKey(from)) { Notice(from,"Defeat the oathbound sentries already challenging you."); return; }
            List<Point3D> points=new List<Point3D>();
            Item oath=Node("oath");
            for (int i=0;i<3;i++)
            {
                Point3D pos;
                if (!CouncilLayout.FindClear(new Point3D(oath.X+(i-1)*3,oath.Y+2,oath.Z),new Rectangle2D(4320,2552,24,23),16,points,out pos))
                { Notice(from,"The watch needs more clear floor nearby. Try again after moving away from the stand."); return; }
                points.Add(pos);
            }
            m_Watch[from]=DateTime.UtcNow+TimeSpan.FromMinutes(5);
            for (int i=0;i<3;i++) { CouncilWatchman g=new CouncilWatchman(this,from,i==2); Own(g); g.Home=points[i]; g.RangeHome=6; g.MoveToWorld(points[i],Map.Atlantis); g.Combatant=from; }
            Notice(from,"Protect the citizens, even against the council. Defeat the two sentries and their captain; remain near the armory.");
        }
        public void WatchDied(CouncilWatchman fallen)
        {
            Mobile from=fallen.Challenger;
            if (from == null || !m_Watch.ContainsKey(from)) return;
            foreach (Mobile m in m_Mobiles) { CouncilWatchman g=m as CouncilWatchman; if (g != null && g != fallen && !g.Deleted && g.Alive && g.Challenger==from) return; }
            m_Watch.Remove(from);
            if (from.Alive && from.Map==Map.Atlantis && from.InRange(fallen.Location,18))
            { Progress(from).Courage=true; Notice(from,"Courage remembered: the watch's duty is to the people. Bring the three testimonies to the council chamber."); }
            else Notice(from,"The challenge ended while you were away. Raise the standard to try again.");
        }
        public bool EnterArena(Mobile from)
        {
            if (!Progress(from).Ready) { Notice(from,"The council still demands truth, a citizen's testimony, and the watch's oath. Consult your Atlantis journal."); return false; }
            if (DateTime.UtcNow<m_NextFight && (Herald==null || Herald.Deleted)) { Notice(from,"The echoes are settling. The passage will answer in a minute."); return false; }
            Item exit=Node("return"); Point3D arrival,bossPoint;
            if (exit==null || !CouncilLayout.FindArrival(exit.Location,out arrival) || !CouncilLayout.FindArrival(new Point3D(3275,3470,-20),out bossPoint))
            { Notice(from,"The anchorage passage is obstructed. Ask a game master to check its placement."); return false; }
            if(Herald==null || Herald.Deleted) m_Participants.Clear();
            if (!m_Participants.Contains(from)) m_Participants.Add(from);
            Travel(from,arrival);
            if (Herald==null || Herald.Deleted)
            {
                Herald=new AtlantisHerald(this); Own(Herald); Herald.Home=bossPoint; Herald.RangeHome=15; Herald.MoveToWorld(bossPoint,Map.Atlantis); Herald.Combatant=from;
            }
            Notice(from,"The Unanswered Anchorage. The three wards repeat what you learned in the city. The return conch remains open, even in death.");
            return true;
        }
        public void Return(Mobile from)
        {
            Item council=Node("council"); Point3D p;
            if (council!=null && CouncilLayout.FindArrival(council.Location,out p)) Travel(from,p);
            else if (CouncilLayout.FindArrival(new Point3D(4366,2615,-127),out p)) Travel(from,p);
        }
        private void Travel(Mobile from, Point3D p)
        {
            if(InArena(from))
            {
                List<Mobile> pets=new List<Mobile>();
                IPooledEnumerable e=Map.Atlantis.GetMobilesInBounds(new Rectangle2D(3255,3450,41,41));
                try
                {
                    foreach(Mobile m in e)
                    {
                        BaseCreature pet=m as BaseCreature;
                        if(pet!=null && InArena(pet) && ((pet.Controlled && pet.ControlMaster==from) || (pet.Summoned && pet.SummonMaster==from))) pets.Add(pet);
                    }
                }
                finally { e.Free(); }
                foreach(Mobile pet in pets) { pet.Combatant=null; pet.MoveToWorld(p,Map.Atlantis); }
            }
            else BaseCreature.TeleportPets(from,p,Map.Atlantis);
            from.MoveToWorld(p,Map.Atlantis);
        }
        public void BossDefeated(AtlantisHerald boss)
        {
            if (boss!=Herald || !Active) return;
            Herald=null; m_NextFight=DateTime.UtcNow+TimeSpan.FromMinutes(1);
            foreach (Mobile m in m_Participants)
            {
                if (!InArena(m) || !Progress(m).Ready) continue;
                CouncilProgress p=Progress(m);
                if (!p.Won) { p.Won=true; p.RewardPending=true; }
                Claim(m);
                Notice(m,"The last decree is broken. Above you, the ship's bell rings once. The citizens are finally heard.");
            }
            // Unclaimed rewards are durable character progress, independent of the arena's cooldown.
        }
        public void Claim(Mobile from)
        {
            CouncilProgress p=Progress(from);
            if(!p.Won || !p.RewardPending) return;
            CouncilMemorialSeal seal=new CouncilMemorialSeal();
            if (from.AddToBackpack(seal)) p.RewardPending=false;
            else { seal.Delete(); Notice(from,"Make room in your backpack, then use the return conch or council seal to claim your memorial seal."); }
        }
        public void ResetFight()
        {
            if (Herald!=null) { Herald.Delete(); Herald=null; }
            foreach (Mobile m in m_Participants) if (InArena(m)) Return(m);
            m_Participants.Clear(); m_EmptySince=DateTime.MinValue; m_NextFight=DateTime.MinValue;
        }
        public void ResetInvestigation(Mobile from)
        {
            bool won=Progress(from).Won,pending=Progress(from).RewardPending;
            CancelWatch(from);
            foreach (Mobile m in m_Mobiles) { AtlantisWitness w=m as AtlantisWitness; if (w!=null && !w.Deleted && w.Guide==from) w.Delete(); }
            m_Participants.Remove(from); if (InArena(from)) Return(from);
            m_Progress[from]=new CouncilProgress { Won=won, RewardPending=pending };
        }
        private void CancelWatch(Mobile from)
        {
            m_Watch.Remove(from);
            foreach (Mobile m in m_Mobiles) { CouncilWatchman g=m as CouncilWatchman; if(g!=null && !g.Deleted && g.Challenger==from) g.Delete(); }
        }
        public void Pulse()
        {
            if (!Active) return;
            m_Items.RemoveAll(delegate(Item i){ return i==null || i.Deleted; });
            m_Mobiles.RemoveAll(delegate(Mobile m){ return m==null || m.Deleted; });
            foreach (Item i in m_Items.ToArray()) { AtlantisCitizenSpawner s=i as AtlantisCitizenSpawner; if(s!=null) s.Pulse(); }
            foreach (Mobile m in m_Mobiles.ToArray()) { AtlantisWitness w=m as AtlantisWitness; if(w!=null) w.Pulse(); }
            Item oath=Node("oath");
            foreach (Mobile m in new List<Mobile>(m_Watch.Keys))
                if (m.Deleted || !m.Alive || m.Map!=Map.Atlantis || oath==null || !m.InRange(oath.Location,24) || DateTime.UtcNow>m_Watch[m]) CancelWatch(m);
            foreach(Mobile m in new List<Mobile>(m_Progress.Keys)) if(m.Deleted) m_Progress.Remove(m);
            if (Herald!=null && !Herald.Deleted)
            {
                bool present=false;
                foreach (Mobile m in m_Participants) if(InArena(m) && m.Alive && m.NetState!=null) present=true;
                if (present) { m_EmptySince=DateTime.MinValue; Herald.Pulse(); }
                else if (m_EmptySince==DateTime.MinValue) m_EmptySince=DateTime.UtcNow;
                else if (DateTime.UtcNow-m_EmptySince>TimeSpan.FromSeconds(60)) ResetFight();
            }
        }
        public override void OnDelete()
        {
            m_Active=false;
            if(m_Timer!=null) { m_Timer.Stop(); m_Timer=null; }
            ResetFight();
            if(m_Region!=null) { m_Region.Unregister(); m_Region=null; }
            foreach(Mobile m in m_Mobiles.ToArray()) { ICouncilOwned o=m as ICouncilOwned; if(o!=null && o.Council==this && !m.Deleted) m.Delete(); }
            foreach(Item i in m_Items.ToArray()) { ICouncilOwned o=i as ICouncilOwned; if(o!=null && o.Council==this && !i.Deleted) i.Delete(); }
            base.OnDelete();
        }
        public override void Serialize(GenericWriter w)
        {
            base.Serialize(w); w.Write(0); w.Write(m_Active); w.WriteItemList(m_Items,true); w.WriteMobileList(m_Mobiles,true);
            w.Write(m_Progress.Count); foreach(KeyValuePair<Mobile,CouncilProgress> pair in m_Progress) { w.Write(pair.Key); pair.Value.Write(w); }
            w.WriteMobileList(m_Participants,true); w.Write(Herald); w.Write(m_NextFight);
        }
        public override void Deserialize(GenericReader r)
        {
            base.Deserialize(r); r.ReadInt(); m_Active=r.ReadBool(); m_Items=r.ReadStrongItemList(); m_Mobiles=r.ReadStrongMobileList();
            m_Progress=new Dictionary<Mobile,CouncilProgress>(); int count=r.ReadInt();
            for(int i=0;i<count;i++) { Mobile m=r.ReadMobile(); CouncilProgress p=CouncilProgress.Read(r); if(m!=null) m_Progress[m]=p; }
            m_Participants=r.ReadStrongMobileList(); Herald=r.ReadMobile() as AtlantisHerald; m_NextFight=r.ReadDateTime(); m_Watch=new Dictionary<Mobile,DateTime>();
            Timer.DelayCall(TimeSpan.FromSeconds(2),Recover);
        }
        public void Recover()
        {
            if(Deleted) return;
            ResetFight();
            foreach(Mobile m in m_Mobiles.ToArray()) if(m is CouncilWatchman || m is AtlantisWitness) m.Delete();
            Attach();
        }
    }

    public sealed class CouncilArenaRegion : Region
    {
        private AtlantisCouncilController m_Council;
        public CouncilArenaRegion(AtlantisCouncilController c) : base("the Unanswered Anchorage",Map.Atlantis,150,AtlantisCouncilController.Arena) { m_Council=c; }
        public override bool AllowHousing(Mobile m,Point3D p) { return false; }
        public override void OnEnter(Mobile m) { base.OnEnter(m); if(m.Player) m.SendMessage(1153,"Above the water, a rescue ship waits forever. The Last Decree still holds this anchorage."); }
        public override bool AllowHarmful(Mobile from,Mobile target)
        {
            if(from is AtlantisHerald) return m_Council.Participant(target);
            if(target is AtlantisHerald) return m_Council.Participant(from);
            return base.AllowHarmful(from,target);
        }
    }
}
