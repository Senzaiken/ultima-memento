using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.ContextMenus;

namespace Server.Engines.Atlantis
{
    public class AtlantisCitizenSpawner : Item,ICouncilOwned
    {
        public AtlantisCouncilController Council { get; private set; }
        public string Key { get; private set; }
        public int Role { get; private set; }
        public AtlantisCitizen Citizen { get; private set; }
        private DateTime m_Next;
        public override bool Decays { get { return false; } }
        public AtlantisCitizenSpawner(AtlantisCouncilController c,string key,int role) : base(0x1F13)
        { Council=c; Key=key; Role=role; Visible=false; Movable=false; Name="Atlantis citizen spawner: "+AtlantisCitizen.Names[role]; }
        public AtlantisCitizenSpawner(Serial s) : base(s) { }
        public void Pulse()
        {
            if(Deleted || Council==null || !Council.Active || Map!=Map.Atlantis || DateTime.UtcNow<m_Next) return;
            m_Next=DateTime.UtcNow+TimeSpan.FromSeconds(30);
            if(Citizen!=null && !Citizen.Deleted)
            {
                if(Citizen.Map!=Map.Atlantis || !Citizen.InRange(Location,4))
                { Point3D home; if(CouncilLayout.FindArrival(Location,out home)) Citizen.MoveToWorld(home,Map.Atlantis); }
                return;
            }
            Point3D p;
            if(!CouncilLayout.FindArrival(Location,out p)) return;
            Citizen=new AtlantisCitizen(Council,Role); Council.Own(Citizen); Citizen.Home=p; Citizen.RangeHome=2; Citizen.MoveToWorld(p,Map.Atlantis);
        }
        public override void OnDelete() { if(Citizen!=null && Citizen.Council==Council && !Citizen.Deleted) Citizen.Delete(); base.OnDelete(); }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Council); w.Write(Key); w.Write(Role); w.Write(Citizen); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Council=r.ReadItem() as AtlantisCouncilController; Key=r.ReadString(); Role=r.ReadInt(); Citizen=r.ReadMobile() as AtlantisCitizen; }
    }
    public class AtlantisCitizen : BaseVendor,ICouncilOwned
    {
        public static readonly string[] Names={"Mira","Edrin","Orin","Neris","Selene","Tovan"};
        private static readonly string[] Titles={"the drowned infirmary keeper","the drowned archivist","the oathbound armorer","the drowned provisioner","the drowned lamplighter","the drowned innkeeper"};
        private static readonly string[] Stories={
            "I kept the sick warm while the harbor bell was silent. There was a little tune I played for them. Now I cannot remember how it ends. A music box waits in my household, southeast of this infirmary. If you find its tune, perhaps I can leave an echo of this room and bear witness at the memorial to the northwest.",
            "I copied the council's proclamation. I believed every word. The three archive shelves still hold the proclamation, the harbor register, and a sealed order. Read them all, then challenge the record at the lectern. Ask which order came first, and who was aboard the ships.",
            "We were told to hold the gates. We did. You can find our torn standard in the barracks east of my armory. Beneath the new command are the words of an older oath. Raise those words at the oath stone here, and be prepared to face the captain.",
            "Bread, bandages, a little thread. People still come to my stall as if supper is waiting at home. You will need more than a sword to answer the council. Edrin knows the record, Mira remembers the people, and Orin remembers the watch. Their testimonies belong at the council seal.",
            "Every lamp I light is blue now. Beyond the council's seal is the sand beneath the ship. There are three wards there: a broken record, a witness's chain, and the watch's rally stone. The Herald speaks before it strikes. Listen, and answer it with what the city taught you.",
            "I laid the officers' tables for a celebration. They never sat down. Some days I still set out the cups. Stay a moment, trade if you need supplies, and speak with the others. The council chamber lies southwest of this hall. No one has answered its last decree." };
        private List<SBInfo> m_Stock=new List<SBInfo>();
        protected override List<SBInfo> SBInfos { get { return m_Stock; } }
        public AtlantisCouncilController Council { get; private set; }
        public int Role { get; private set; }
        public override bool IsInvulnerable { get { return true; } }
        public override bool BardImmune { get { return true; } }
        public override bool Unprovokable { get { return true; } }
        public override bool Uncalmable { get { return true; } }
        public override bool CanTeach { get { return false; } }
        // Living human bodies support normal conversation and trade; spectral hue conveys the deceased citizen.
        public override void InitBody() { InitStats(100,100,100); Body=400; Hue=0x4001; Name="a remembering citizen"; }
        public override void InitOutfit() { Robe r=new Robe(2319); r.Movable=false; AddItem(r); }
        public AtlantisCitizen(AtlantisCouncilController c,int role) : base("the drowned citizen")
        {
            Council=c; Role=role; Name=Names[role]; Title=Titles[role]; Female=role==0 || role==3 || role==4; Body=Female?401:400;
            Blessed=true; FightMode=FightMode.None; CantWalk=true; LoadSBInfo();
            Item badge;
            if(role==2) badge=new HalfApron(2831);
            else if(role==1) badge=new WizardsHat(2764);
            else if(role==4) badge=new Cloak(2319);
            else badge=new HalfApron(2767);
            badge.Movable=false; AddItem(badge);
        }
        public AtlantisCitizen(Serial s) : base(s) { }
        public override void InitSBInfo(Mobile m) { m_Stock.Add(new CouncilStock(Role)); }
        public override bool CheckVendorAccess(Mobile from)
        { return from is PlayerMobile && Council!=null && Council.CanUse(from,this,true) && base.CheckVendorAccess(from); }
        public override void OnDoubleClick(Mobile from) { Talk(from,false); }
        public override bool HandlesOnSpeech(Mobile from) { return from.Map==Map && from.InRange(Location,3) || base.HandlesOnSpeech(from); }
        public override void OnSpeech(SpeechEventArgs e)
        {
            string s=e.Speech.ToLowerInvariant();
            if(!e.Handled && (s.Contains("hello") || s.Contains("council") || s.Contains("clue") || s.Contains("music box") || s==Name.ToLowerInvariant()))
            { Talk(e.Mobile,false); e.Handled=true; }
            if(!e.Handled) base.OnSpeech(e);
        }
        public override void AddCustomContextEntries(Mobile from,List<ContextMenuEntry> list)
        { base.AddCustomContextEntries(from,list); list.Add(new CouncilTalkEntry(from,this)); }
        private sealed class CouncilTalkEntry : ContextMenuEntry
        {
            private Mobile m_From; private AtlantisCitizen m_Npc;
            public CouncilTalkEntry(Mobile m,AtlantisCitizen n) : base(6146,3) { m_From=m; m_Npc=n; }
            public override void OnClick() { m_Npc.Talk(m_From,false); }
        }
        public void Talk(Mobile from,bool detail)
        {
            if(Council==null || !Council.CanUse(from,this,true)) { from.SendMessage("Come closer to speak with this citizen."); return; }
            if(detail && Role==0) { Council.StartEscort(from); return; }
            string story=Stories[Role];
            if(detail) story+="<BR><BR>"+CouncilText.Summary(Council.Progress(from));
            CouncilGump.Show(from,Council,this,Name+", "+Title,story,new string[]{Role==0?"I remember your music box. Come bear witness.":"What should I do next?","Review my investigation.","Buy supplies.","Sell supplies."});
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Council); w.Write(Role); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Council=r.ReadItem() as AtlantisCouncilController; Role=r.ReadInt(); Blessed=true; CantWalk=true; LoadSBInfo(); }
    }
    public sealed class CouncilStock : SBInfo
    {
        private List<GenericBuyInfo> m_Buy=new List<GenericBuyInfo>();
        private GenericSellInfo m_Sell=new GenericSellInfo();
        public override List<GenericBuyInfo> BuyInfo { get { return m_Buy; } }
        public override IShopSellInfo SellInfo { get { return m_Sell; } }
        private void Stock(Type type,int price,int art) { m_Buy.Add(new GenericBuyInfo(type,price,30,art,0)); m_Sell.Add(type,Math.Max(1,price/3)); }
        public CouncilStock(int role)
        {
            if(role==0) { Stock(typeof(Bandage),6,0xE21); Stock(typeof(LesserHealPotion),24,0xF0C); }
            else if(role==1) { Stock(typeof(BlankScroll),8,0xEF3); Stock(typeof(BlackPearl),6,0xF7A); }
            else if(role==2) { Stock(typeof(Dagger),24,0xF52); Stock(typeof(Buckler),45,0x1B73); }
            else if(role==4) { Stock(typeof(Candle),8,0xA28); Stock(typeof(SulfurousAsh),6,0xF8C); }
            else { Stock(typeof(BreadLoaf),8,0x103B); Stock(typeof(Bandage),6,0xE21); }
        }
    }
    public class AtlantisWitness : Mobile,ICouncilOwned
    {
        public AtlantisCouncilController Council { get; private set; }
        public Mobile Guide { get; private set; }
        private PathFollower m_Path;
        private Point3D m_LastSafe;
        private DateTime m_Expires;
        public AtlantisWitness(AtlantisCouncilController c,Mobile guide)
        { Council=c; Guide=guide; Name="Mira's remembering echo"; Body=401; Hue=0x4001; Blessed=true; InitStats(100,100,100); m_Expires=DateTime.UtcNow+TimeSpan.FromMinutes(20); }
        public AtlantisWitness(Serial s) : base(s) { }
        public void Pulse()
        {
            if(Deleted) return;
            if(Council==null || !Council.Active || Guide==null || Guide.Deleted || DateTime.UtcNow>m_Expires) { Delete(); return; }
            if(!Guide.Alive || Guide.Map!=Map.Atlantis || !Guide.InRange(Location,12)) return;
            if(Guide.InRange(Location,3) && Guide.InLOS(this)) m_LastSafe=Location;
            if(Guide.InRange(Location,2)) return;
            if(m_Path==null) m_Path=new PathFollower(this,Guide);
            m_Path.Follow(false,2);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if(from!=Guide || Council==null || !Council.CanUse(from,this,true)) return;
            from.SendMessage("Mira: The memorial is west of the music room, north of the infirmary. Walk slowly; I will follow.");
            if(m_LastSafe!=Point3D.Zero && Map.Atlantis.CanFit(m_LastSafe,16,true,false)) MoveToWorld(m_LastSafe,Map.Atlantis);
            m_Path=null;
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Council); w.Write(Guide); w.Write(m_LastSafe); w.Write(m_Expires); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Council=r.ReadItem() as AtlantisCouncilController; Guide=r.ReadMobile(); m_LastSafe=r.ReadPoint3D(); m_Expires=r.ReadDateTime(); }
    }
    public class CouncilWatchman : BaseCreature,ICouncilOwned
    {
        public AtlantisCouncilController Council { get; private set; }
        public Mobile Challenger { get; private set; }
        public CouncilWatchman(AtlantisCouncilController c,Mobile challenger,bool captain) : base(AIType.AI_Melee,FightMode.Aggressor,12,1,0.25,0.5)
        {
            Council=c; Challenger=challenger; Name=captain?"the oathbound Watch-Captain":"an oathbound sentry"; Body=400; Hue=0x4001;
            SetStr(100); SetDex(85); SetInt(50); SetHits(captain?240:120); SetDamage(7,12);
            SetSkill(SkillName.FistFighting,65); SetSkill(SkillName.Tactics,65); SetSkill(SkillName.MagicResist,50);
            SetResistance(ResistanceType.Physical,25); SetResistance(ResistanceType.Cold,40);
            Fame=0; Karma=-100; Tamable=false;
            Robe robe=new Robe(2831); robe.Movable=false; AddItem(robe);
        }
        public CouncilWatchman(Serial s) : base(s) { }
        public override bool BardImmune { get { return true; } }
        public override bool DeleteCorpseOnDeath { get { return true; } }
        public override void GenerateLoot() { }
        public override bool OnBeforeDeath()
        { bool die=base.OnBeforeDeath(); if(die && Council!=null && !Council.Deleted) Council.WatchDied(this); return die; }
        public override void OnThink()
        {
            if(Council==null || !Council.Active || Challenger==null || Challenger.Deleted) { Delete(); return; }
            if(Map!=Map.Atlantis || !InRange(Home,12)) { Combatant=null; MoveToWorld(Home,Map.Atlantis); }
            if(Challenger.Alive && Challenger.Map==Map && Challenger.InRange(Location,14)) Combatant=Challenger;
            base.OnThink();
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Council); w.Write(Challenger); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Council=r.ReadItem() as AtlantisCouncilController; Challenger=r.ReadMobile(); }
    }
    public class AtlantisHerald : BaseCreature,ICouncilOwned
    {
        public AtlantisCouncilController Council { get; private set; }
        private DateTime m_Next=DateTime.UtcNow+TimeSpan.FromSeconds(12),m_Deadline;
        private int m_Cycle,m_Decree,m_Lane;
        private bool m_Answered;
        public int Decree { get { return m_Decree; } }
        public AtlantisHerald(AtlantisCouncilController c) : base(AIType.AI_Melee,FightMode.Aggressor,18,1,0.3,0.5)
        {
            Council=c; Name="the Herald of the Last Decree"; Body=400; Hue=0x4001;
            SetStr(180); SetDex(90); SetInt(150); SetHits(1600); SetDamage(10,16);
            SetSkill(SkillName.FistFighting,85); SetSkill(SkillName.Tactics,85); SetSkill(SkillName.MagicResist,75);
            SetResistance(ResistanceType.Physical,35); SetResistance(ResistanceType.Cold,50); SetResistance(ResistanceType.Fire,15);
            SetResistance(ResistanceType.Energy,25); SetResistance(ResistanceType.Poison,35);
            Fame=0; Karma=-1000; Tamable=false; Robe robe=new Robe(2319); robe.Movable=false; AddItem(robe);
        }
        public AtlantisHerald(Serial s) : base(s) { }
        public override bool BardImmune { get { return true; } }
        public override bool DeleteCorpseOnDeath { get { return true; } }
        public override void GenerateLoot() { }
        public override void Damage(int amount,Mobile from)
        {
            if(Council==null || !Council.Active || !AtlantisCouncilController.InArena(from) || !Council.Participant(from)) return;
            if(m_Decree==1 && !m_Answered) { if(from!=null) from.SendMessage(1153,"The false record shields the Herald. Answer at the TRUTH ward!"); return; }
            base.Damage(amount,from);
        }
        private List<Mobile> Targets()
        {
            List<Mobile> result=new List<Mobile>();
            IPooledEnumerable e=Map.Atlantis.GetMobilesInBounds(new Rectangle2D(3255,3450,41,41));
            try { foreach(Mobile m in e) if(m.Alive && AtlantisCouncilController.InArena(m) && Council.Participant(m)) result.Add(m); }
            finally { e.Free(); }
            return result;
        }
        public void BeginDecree(int decree)
        {
            m_Decree=decree; m_Answered=false; m_Deadline=DateTime.UtcNow+TimeSpan.FromSeconds(8);
            string text=decree==1?"LET THE RECORD STAND! Answer at the TRUTH ward to break my shield.":decree==2?"NONE SHALL DEPART! Release the witness at the LOVE ward before the chains tighten.":"HOLD YOUR STATION! A surge crosses the marked lane. Rally at the COURAGE ward, or leave the lane.";
            Say(text);
            foreach(Mobile m in Targets()) if(m.Player) m.SendMessage(1153,text);
            if(decree==3)
            {
                Mobile target=Combatant; m_Lane=target!=null && AtlantisCouncilController.InArena(target)?target.X:X;
                for(int y=3452;y<=3488;y+=2)
                { int z=Map.Atlantis.GetAverageZ(m_Lane,y); if(z>=-20 && z<=-19) Effects.SendLocationEffect(new Point3D(m_Lane,y,z),Map.Atlantis,0x376A,80,10,2319,0); }
            }
            else
            {
                Item ward=Council.Node(decree==1?"wardtruth":"wardlove");
                if(ward!=null) Effects.SendLocationEffect(ward.Location,Map.Atlantis,0x376A,80,10,2319,0);
            }
        }
        public bool UseWard(Mobile from,string key)
        {
            Item ward=Council.Node(key);
            if(!Council.Participant(from) || ward==null || !Council.CanUse(from,ward,true) || m_Decree==0 || m_Answered) return false;
            string needed=m_Decree==1?"wardtruth":m_Decree==2?"wardlove":"wardcourage";
            if(key!=needed) { from.SendMessage("That ward does not answer the current decree."); return false; }
            m_Answered=true; from.SendMessage(1153,m_Decree==1?"The true record shatters the Herald's shield!":m_Decree==2?"The witness's chains fall away!":"The watch's rally stone shelters those within four paces.");
            return true;
        }
        public void ResolveDecree()
        {
            if(m_Decree==0) return;
            Item rally=Council.Node("wardcourage");
            foreach(Mobile m in Targets())
            {
                bool hit=m_Decree==1?!m_Answered:m_Decree==2?!m_Answered:Math.Abs(m.X-m_Lane)<=2 && !(m_Answered && rally!=null && m.InRange(rally.Location,4));
                if(hit) { DoHarmful(m); AOS.Damage(m,this,18,0,0,100,0,0); m.SendMessage(1153,"The decree breaks over you like a cold wave."); }
            }
            if(m_Decree==2 && !m_Answered) Hits=Math.Min(HitsMax,Hits+80);
            m_Decree=0; m_Next=DateTime.UtcNow+TimeSpan.FromSeconds(Hits<HitsMax/3?7:12);
        }
        public void Pulse()
        {
            if(Deleted || Council==null || !Council.Active) return;
            if(!AtlantisCouncilController.InArena(this)) MoveToWorld(Home,Map.Atlantis);
            if(Combatant==null || !Council.Participant(Combatant) || !AtlantisCouncilController.InArena(Combatant))
            { List<Mobile> t=Targets(); Combatant=t.Count>0?t[0]:null; }
            if(m_Decree!=0) { if(DateTime.UtcNow>=m_Deadline) ResolveDecree(); }
            else if(DateTime.UtcNow>=m_Next) BeginDecree((m_Cycle++%3)+1);
        }
        public override bool OnBeforeDeath()
        { bool die=base.OnBeforeDeath(); if(die && Council!=null) Council.BossDefeated(this); return die; }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Council); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Council=r.ReadItem() as AtlantisCouncilController; }
    }
}
