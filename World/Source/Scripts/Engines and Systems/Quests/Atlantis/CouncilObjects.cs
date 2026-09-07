using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;
using Server.Items;

namespace Server.Engines.Atlantis
{
    public class CouncilObject : Item, ICouncilOwned
    {
        public AtlantisCouncilController Council { get; private set; }
        public string Key { get; private set; }
        public override bool Decays { get { return false; } }
        public CouncilObject(AtlantisCouncilController c,string key,int art,int hue,string name) : base(art)
        { Council=c; Key=key; Hue=hue; Name=name; Movable=false; }
        public CouncilObject(Serial s) : base(s) { }
        public override void OnDoubleClickDead(Mobile m) { if(Key=="return") OnDoubleClick(m); }
        public override void OnDoubleClick(Mobile from)
        {
            if(Council==null || !Council.CanUse(from,this,Key!="return")) { from.SendMessage("Come closer to read or use this relic."); return; }
            if(Key.StartsWith("shelf"))
            {
                int n=Int32.Parse(Key.Substring(5)); Council.Progress(from).Evidence|=1<<n;
                CouncilGump.Show(from,Council,this,CouncilText.ClueTitles[n],CouncilText.Clues[n],new string[0]); return;
            }
            if(Key=="truth") { CouncilGump.Show(from,Council,this,"Challenge the record",CouncilText.TruthQuestion,new string[]{"The watch abandoned its posts.","The council withdrew the rescue ships.","Mira refused passage aboard a ship."}); return; }
            if(Key=="keepsake") { Council.Progress(from).Keepsake=true; CouncilGump.Show(from,Council,this,"A tune without an ending","Inside the box is a brass comb, a tiny ship, and the name MIRA. You remember its melody. Mira tends the beds in the infirmary to the northwest; ask her about the music box. The keepsake stays here for other investigators.",new string[0]); return; }
            if(Key=="standard") { Council.Progress(from).Standard=true; CouncilGump.Show(from,Council,this,"The watch's torn standard","Beneath the council's command is an older oath: 'Our strength belongs to the people.' You commit the words to memory. Raise that oath at the stone in the armory to the west.",new string[0]); return; }
            if(Key=="memorial") { Council.CompleteEscort(from); return; }
            if(Key=="oath") { CouncilGump.Show(from,Council,this,"The watch must choose","Whom should the watch protect when its orders condemn the city? Raising the people's oath will summon the captain and two sentries to challenge you.",new string[]{"Protect the council at any cost.","Protect the people; raise their oath."}); return; }
            if(Key=="council") { Council.Claim(from); CouncilGump.Show(from,Council,this,"The Last Council",CouncilText.CouncilIntro+"<BR><BR>"+CouncilText.Summary(Council.Progress(from)),new string[]{"Call the witnesses and challenge the decree."}); return; }
            if(Key=="return") { Council.Claim(from); Council.Return(from); return; }
            if(Key.StartsWith("ward"))
            {
                if(Council.Herald==null || Council.Herald.Deleted) { from.SendMessage("This ward waits for the Herald's next decree."); return; }
                Council.Herald.UseWard(from,Key); return;
            }
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Council); w.Write(Key); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Council=r.ReadItem() as AtlantisCouncilController; Key=r.ReadString(); Movable=false; }
    }
    public class CouncilMemorialSeal : Item
    {
        public CouncilMemorialSeal() : base(0x108A) { Name="a memorial seal of the Last Council"; Hue=2319; Weight=1; LootType=LootType.Blessed; }
        public CouncilMemorialSeal(Serial s) : base(s) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public static class CouncilText
    {
        public static readonly string[] ClueTitles={"The public proclamation","The harbor register","The sealed evacuation order"};
        public static readonly string[] Clues={
            "BY THE LAST COUNCIL: Remain in your homes. The watch holds every gate. Rescue ships wait at the anchorage and will carry every citizen to safety when the bell rings. No household shall be forgotten.<BR><BR>The date and council seal are intact. A clerk has scratched 'the bell never rang' into the margin.",
            "Third watch: every watch post reports occupied. Fourth watch: all rescue vessels recalled under the council's seal. Fifth watch: the public proclamation delivered to the households. Passengers embarked: NONE.<BR><BR>A loose petition reads: 'Mira, infirmary keeper. I will stay with the sick until a ship comes for us all.'",
            "SEALED ORDER: Withdraw the rescue vessels before the proclamation is read. The city's preservation requires that its citizens remain within the binding. Let the Herald enforce our decree beyond our deaths.<BR><BR>Under a different hand: 'The stones were saved. We were the price.'" };
        public const string TruthQuestion="The proclamation promised rescue. The harbor register records the watches in order. The sealed order names the price.<BR><BR>Which accusation can you prove from all three documents?";
        public const string CouncilIntro="Water trembles over the council's seal. From the empty chairs: 'Our city endures. What answer have you brought?' The Herald waits beneath the ship at the Unanswered Anchorage.";
        public static string Summary(CouncilProgress p)
        {
            return "Archive records: "+((p.Evidence&1)!=0?"proclamation ":"")+((p.Evidence&2)!=0?"register ":"")+((p.Evidence&4)!=0?"sealed order":"")
                +"<BR>Truth: "+(p.Truth?"the lie exposed":"read the three shelves and challenge the archive lectern")
                +"<BR>Love: "+(p.Love?"Mira's testimony restored":p.Keepsake?"speak with Mira; guide her echo to the memorial":"find Mira's music box in the sunken household")
                +"<BR>Courage: "+(p.Courage?"the watch's oath restored":p.Standard?"raise the people's oath in the armory":"remember the torn standard in the barracks")
                +"<BR><BR>"+(p.Ready?"The council seal can now open the passage.":"Each testimony can be completed in any order.")
                +(p.Won?"<BR>The Herald has fallen to your challenge.":"");
        }
    }
    public class CouncilGump : Gump
    {
        private AtlantisCouncilController m_Council;
        private IEntity m_Source;
        private Mobile m_Viewer;
        private int m_Choices;
        public static void Show(Mobile m,AtlantisCouncilController c,IEntity source,string title,string body,string[] choices)
        { m.CloseGump(typeof(CouncilGump)); m.SendGump(new CouncilGump(m,c,source,title,body,choices)); }
        public static void Journal(Mobile m,AtlantisCouncilController c)
        {
            CouncilProgress p=c.Progress(m);
            string body=CouncilText.Summary(p);
            for(int i=0;i<3;i++) if((p.Evidence&(1<<i))!=0) body+="<BR><BR><B>"+CouncilText.ClueTitles[i]+"</B><BR>"+CouncilText.Clues[i];
            Show(m,c,null,"Your Atlantis investigation",body,new string[0]);
        }
        public CouncilGump(Mobile m,AtlantisCouncilController c,IEntity source,string title,string body,string[] choices) : base(70,60)
        {
            m_Council=c; m_Source=source; m_Viewer=m; m_Choices=choices.Length;
            AddPage(0); AddBackground(0,0,610,520,9270); AddLabel(25,18,1153,title);
            AddHtml(25,55,555,245,"<BASEFONT COLOR=#D9F0ED>"+body+"</BASEFONT>",false,true);
            for(int i=0;i<choices.Length;i++) { AddButton(25,315+i*38,4005,4007,i+1,GumpButtonType.Reply,0); AddHtml(65,315+i*38,510,36,"<BASEFONT COLOR=#D9F0ED>"+choices[i]+"</BASEFONT>",false,false); }
            AddLabel(25,485,1153,"Double-click citizens to talk. Use their context menu to buy or sell.");
        }
        public override void OnResponse(NetState state,RelayInfo info)
        {
            if(state.Mobile!=m_Viewer || info.ButtonID<=0 || info.ButtonID>m_Choices || m_Council==null || m_Council.Deleted) return;
            Item item=m_Source as Item; Mobile citizen=m_Source as Mobile;
            if((item!=null && item.Deleted) || (citizen!=null && citizen.Deleted) || !m_Council.CanUse(m_Viewer,m_Source,true)) return;
            CouncilObject o=item as CouncilObject;
            if(o!=null)
            {
                if(o.Key=="truth") m_Council.SolveTruth(m_Viewer,info.ButtonID);
                else if(o.Key=="oath") { if(info.ButtonID==2) m_Council.StartWatch(m_Viewer); else m_Viewer.SendMessage("The old order offers only the same ending."); }
                else if(o.Key=="council") m_Council.EnterArena(m_Viewer);
            }
            AtlantisCitizen npc=citizen as AtlantisCitizen;
            if(npc!=null)
            {
                if(info.ButtonID==1) npc.Talk(m_Viewer,true);
                else if(info.ButtonID==2) Journal(m_Viewer,m_Council);
                else if(info.ButtonID==3) npc.VendorBuy(m_Viewer);
                else if(info.ButtonID==4) npc.VendorSell(m_Viewer);
            }
        }
    }
}
