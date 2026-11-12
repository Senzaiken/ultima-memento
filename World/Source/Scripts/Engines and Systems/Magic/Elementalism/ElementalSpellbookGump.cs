using System; 
using System.Collections; 
using Server; 
using Server.Items; 
using Server.Misc; 
using Server.Network; 
using Server.Spells; 
using Server.Spells.Elementalism; 
using Server.Prompts;
using Server.Scripts.Commands;

namespace Server.Gumps 
{ 
	public class ElementalSpellbookGump : Gump
	{
		private const int FIRST_LIST_PAGE = 2;
		private const int SPELLS_PER_LIST_PAGE = 2 * ElementalSpellProvider.SpellsPerCircle;

		private static readonly string[] SphereNumerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" };

		private static int FIRST_SPELL_PAGE
		{ get { return FIRST_LIST_PAGE + ( ElementalSpellProvider.SpellCount + SPELLS_PER_LIST_PAGE - 1 ) / SPELLS_PER_LIST_PAGE; } }

		private static int LAST_PAGE
		{ get { return FIRST_SPELL_PAGE + ElementalSpellProvider.SpellCount - 1; } }

		private ElementalSpellbook m_Book;

		public bool HasSpell(Mobile from, int spellID)
		{
			if ( m_Book.RootParentEntity == from )
				return (m_Book.HasSpell(spellID));
			else
				return false;
		}

		public ElementalSpellbookGump( Mobile from, ElementalSpellbook book, int page ) : base( 100, 100 ) 
		{
			if ( page < 1 || page > LAST_PAGE ){ page = 1; }

			m_Book = book;
			m_Book.EllyPage = page;

            this.Closable=true;
			this.Disposable=true;
			this.Dragable=true;
			this.Resizable=false;

			int hue = 2884;
			string fnt = ElementalSpell.FontColor( m_Book.ItemID );

			if ( m_Book.ItemID == 0x6713 ) // EARTH
			{
				hue = 2884;
			}
			else if ( m_Book.ItemID == 0x6715 ) // WATER
			{
				hue = 2876;
			}
			else if ( m_Book.ItemID == 0x6717 ) // AIR
			{
				hue = 2807;
			}
			else if ( m_Book.ItemID == 0x6719 ) // FIRE
			{
				hue = 2464;
			}

			AddPage(0);
			AddImage(0, 0, 11138, hue);
			AddImage(0, 0, 11152);
			AddImage(0, 0, 11147);

			int PriorPage = page - 1;
				if ( PriorPage < 1 ){ PriorPage = LAST_PAGE; }
			int NextPage = page + 1;
				if ( NextPage > LAST_PAGE ){ NextPage = 1; }

			AddButton(24, 8, 11159, 11159, PriorPage, GumpButtonType.Reply, 0); 	// PAGE LEFT
			AddButton(295, 8, 11160, 11160, NextPage, GumpButtonType.Reply, 0); 	// PAGE RIGHT

			AddButton(40, 209, 2095, 2095, (500+page), GumpButtonType.Reply, 0);	// HELP

			if ( page == 1 ) // MAIN PAGE
			{
				AddHtml( 85, 12, 89, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Elemental</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 207, 12, 89, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Spellbook</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 34, 35, 133, 160, @"<BODY><BASEFONT Color=" + fnt + ">Known elemental spells can be cast by selecting the button next to each listed spell. A known spell will also have a red bookmark on its detail page you can click on to cast as well.</BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 188, 35, 133, 160, @"<BODY><BASEFONT Color=" + fnt + ">Each spell has the power needed to cast, which is the amount of mana and stamina the caster needs. The bookmark below can be selected to view the information on how this magic works.</BASEFONT></BODY>", (bool)false, (bool)false);
			}
			else if ( page >= FIRST_LIST_PAGE && page < FIRST_SPELL_PAGE ) // SPELL LISTS
			{
				int firstSpell = ( page - FIRST_LIST_PAGE ) * SPELLS_PER_LIST_PAGE;
				int firstSphere = firstSpell / ElementalSpellProvider.SpellsPerCircle;

				AddHtml( 59, 14, 100, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Sphere " + SphereNumerals[firstSphere] + "</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 207, 14, 100, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Sphere " + SphereNumerals[firstSphere + 1] + "</BIG></BASEFONT></BODY>", (bool)false, (bool)false);

				for ( int i = 0; i < SPELLS_PER_LIST_PAGE && firstSpell + i < ElementalSpellProvider.SpellCount; ++i )
				{
					SpellDefinition definition = ElementalSpellProvider.SpellDefinitions[firstSpell + i];
					int id = definition.SpellID;

					// Left page holds the first sphere, right page holds the second
					int x = i < ElementalSpellProvider.SpellsPerCircle ? 0 : 155;
					int y = 40 * ( i % ElementalSpellProvider.SpellsPerCircle );

					if ( HasSpell(from, id) ){ AddButton(35 + x, 55 + y, 2224, 2224, id, GumpButtonType.Reply, 0); }
					AddHtml( 60 + x, 50 + y, 102, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>" + ElementalSpellProvider.GetShortName( definition ) + "</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
					AddButton(155 + x, 55 + y, 2104, 2104, FIRST_SPELL_PAGE + firstSpell + i, GumpButtonType.Reply, 0);
				}
			}
			else // SPELL DESCRIPTIONS
			{
				int spellIndex = page - FIRST_SPELL_PAGE;
				SpellDefinition definition = ElementalSpellProvider.SpellDefinitions[spellIndex];
				int spell = definition.SpellID;
				string sphere = SphereNumerals[ElementalSpellProvider.GetCircle( definition )];
				int sect = FIRST_LIST_PAGE + spellIndex / SPELLS_PER_LIST_PAGE;

				int spellIcon = ElementalSpell.SpellIcon( book.ItemID, spell );
				if ( HasSpell(from, spell) ){ AddButton(74, 86, spellIcon, spellIcon, spell, GumpButtonType.Reply, 0); }
				else AddImage( 74, 86, spellIcon );

				var spellName = ElementalSpellProvider.GetShortName( definition );

				AddHtml( 34, 13, 133, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG><CENTER>Elemental</CENTER></BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 34, 29, 133, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG><CENTER>" + spellName + "</CENTER></BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 26, 130, 144, 20, @"<BODY><BASEFONT Color=" + fnt + "><CENTER>[" + ElementalCommands.GetLegacyCommand( definition ) + "</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 34, 166, 100, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Power:</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 139, 166, 38, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>" + definition.ManaCost + "</BIG></BASEFONT></BODY>", (bool)false, (bool)false);

				AddHtml( 34, 184, 100, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Elementalism:</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 139, 184, 38, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>" + definition.MinSkill + "</BIG></BASEFONT></BODY>", (bool)false, (bool)false);

				AddHtml( 190, 14, 100, 20, @"<BODY><BASEFONT Color=" + fnt + "><BIG>Sphere " + sphere + "</BIG></BASEFONT></BODY>", (bool)false, (bool)false);
				AddHtml( 189, 38, 134, 159, @"<BODY><BASEFONT Color=" + fnt + ">" + ElementalSpell.DescriptionInfo( spell, m_Book.ItemID ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

				AddButton(35, 35, 2104, 2104, sect, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse( NetState state, RelayInfo info ) 
		{
			Mobile from = state.Mobile; 
			from.SendSound( 0x55 );

			if ( info.ButtonID < 200 && info.ButtonID > 0 )
			{
				int page = info.ButtonID;
				if ( page < 1 ){ page = LAST_PAGE; }
				if ( page > LAST_PAGE ){ page = 1; }
				from.SendGump( new ElementalSpellbookGump( from, m_Book, page ) );
			}
			else if ( info.ButtonID >= 500 )
			{
				from.SendGump( new ElementalSpellbookGump( from, m_Book, (info.ButtonID-500) ) );
				from.SendGump( new ElementalSpellHelp( from, m_Book, 1 ) );
			}
			else if ( ElementalSpellProvider.GetDefinition( info.ButtonID ) != null && HasSpell(from, info.ButtonID) )
			{
				ElementalSpellProvider.Cast( from, info.ButtonID );

				from.SendGump( new ElementalSpellbookGump( from, m_Book, m_Book.EllyPage ) );
			}
		}
	}

	public class ElementalSpellHelp : Gump 
	{
		private ElementalSpellbook m_Book; 

		public ElementalSpellHelp( Mobile from, ElementalSpellbook book, int page ) : base( 300, 200 ) 
		{
			m_Book = book; 

			from.SendSound( 0x55 );

            this.Closable=true;
			this.Disposable=true;
			this.Dragable=true;
			this.Resizable=false;

			AddPage(0);

			int img = 11143;
			int pic = 11148;
			string cat = "EARTH";
			string fnt = ElementalSpell.FontColor( m_Book.ItemID );

			if ( m_Book.ItemID == 0x6713 ) // EARTH
			{
				img = 11143;
				pic = 11148;
				cat = "EARTH";

			}
			else if ( m_Book.ItemID == 0x6715 ) // WATER
			{
				img = 11144;
				pic = 11149;
				cat = "WATER";
			}
			else if ( m_Book.ItemID == 0x6717 ) // AIR
			{
				img = 11145;
				pic = 11150;
				cat = "AIR";
			}
			else if ( m_Book.ItemID == 0x6719 ) // FIRE
			{
				img = 11146;
				pic = 11151;
				cat = "FIRE";
			}

			int btn = 4005;
			int pge = 2;
				if ( page == 2 ){ btn = 4014; pge = 1; }


			AddImage(0, 0, 7041, Server.Misc.PlayerSettings.GetGumpHue( from ));
			AddButton(608, 9, 4017, 4017, 0, GumpButtonType.Reply, 0);
			AddButton(567, 10, btn, btn, pge, GumpButtonType.Reply, 0);
			AddHtml( 12, 12, 530, 20, @"<BODY><BASEFONT Color=" + fnt + ">" + cat + " ELEMENTAL MAGIC</BASEFONT></BODY>", (bool)false, (bool)false);

			if ( page == 2 )
			{
				AddImage(14, 326, pic);
				string commands = "";
				foreach ( SpellDefinition definition in ElementalSpellProvider.SpellDefinitions )
					commands += "[" + ElementalCommands.GetLegacyCommand( definition ) + "<BR>    Cast " + definition.Name.String + "<BR><BR>";
				commands += "You can also type [Spells_Elementalism to see these commands.<BR><BR>";

				AddHtml( 181, 118, 451, 360, @"<BODY><BASEFONT Color=" + fnt + ">" + commands + "<BR><BR></BASEFONT></BODY>", (bool)false, (bool)true);
				AddHtml( 12, 44, 623, 60, @"<BODY><BASEFONT Color=" + fnt + ">Below are the [ commands you can either type to quickly cast a particular spell, or set a hot key to issue this command and cast the spell.</BASEFONT></BODY>", (bool)false, (bool)false);
			}
			else
			{
				AddImage(14, 365, img);
				string lowreg = "If you manage to find magical items that have a lower reagent quality, then the stamina required for spells will be reduced proportionally by that value. ";
					if ( MyServerSettings.LowerReg() < 1 )
						lowreg = "";

				AddHtml( 118, 47, 516, 433, @"<BODY><BASEFONT Color=" + fnt + ">Unlike other forms of magic, elementalism relies on both the intellect and physical condition of the spellcaster. No reagents are required, but casting these spells require an equal use of mana and stamina, referred to as 'power'. " + lowreg + "There are no supplement skills required for elementalists, like mages with their psychology or necromancers with their spiritualism. There is a guild you can join at the Lyceum, which is hidden on the Devil Guard mountains, with an illusionary spell. To get there, you need only cast the Sanctuary spell. This spell cannot be used everywhere, but as you learn more about elementalism you will be able to cast it in places like dungeons to quickly get to safety.<BR><BR>If you manage to visit the Lyceum, there are four shrines to the elements of Earth, Air, Water, and Fire. Each elementalist can only focus on one element at a time, and they can change their focus whenever they want. To do so, simply step up onto the shrine of your choice. If you want to change your clothing to match your focused element, simply speak the word 'culara'. Changing focus does not affect your spell library. This means that if you have 10 spells in your Earth Elemental Spellbook, and you change your focus to Water Elemental Magic, then your changed Water Elemental Spellbook will have the same 10 types of spells in it.<BR><BR>The only way to scribe elemental spells onto scrolls is by writing the magic words with daemon blood, and casting spells can be a tedious process, but it doesn't have to be. There is a secret that few elementalists know. You can have up to 2 custom spell bars that you can cast with. These spell bars keep your favorite spells at your fingertips, and allow you to cast them quicker. To learn the commands to access these secrets, you can find them by using the 'Help' button on the paperdoll.<BR><BR>This magic is said to have been forged by the Titans of the Elements, and many elementalists try to find their way into the Underworld to seek them out. What they desire are the spellbooks from the Titans, as they are very powerful. Most mages and elementalists have their spell casting often fail in the Underworld. Elementalists believe that equipping one of the Titan's spellbooks would allow them to explore the dark realm without this hinderance. Whether truth or falsehood, one can only try.<BR><BR>A word of warning in practicing other forms of magic, magery and necromancy cannot be bestowed upon the same elementalist. One knowing about these other forms of magic will cause any of the spells to fail when attempting to cast. This includes having items that enhance these types of skills. An aspiring spellcaster must choose a path and move toward that goal. You can either pursue elementalism, or you can learn about magery and necromancy. Elementalism knowledge even affects the forces of magic wands or runic magic. The pursuit of magic research is also something elementalists cannot achieve. If you find that you are losing interest in one of these schools of magic, and want to quickly pursue the other, then search for the Sorcerer Cave in Sosaria or the Conjurerer's Cave in Lodoria. They have crystals discovered centuries ago, that can help you forget what you learned in one area of magic so you can learn another.<BR><BR>Lastly, elemental magic is very sensitive to the forces surrounding the caster. The more armor you are wearing, the more likely your spells will fail. You either avoid wearing armor or find armor suited for spellcasters, that perhaps have a mage armor quality to them. Shields with spell channeling forces also help. Metal armor is the most obtrusive in this regard, where wooden armor is less detrimental. Leather armor is the least impactful, but finding quality clothing is the desired course.</BASEFONT></BODY>", (bool)false, (bool)true);
			}
		}

		public override void OnResponse( NetState state, RelayInfo info ) 
		{
			Mobile from = state.Mobile; 
			from.SendSound( 0x55 );

			if ( info.ButtonID > 0 )
			{
				int page = info.ButtonID;
				if ( page < 1 ){ page = 37; }
				if ( page > 37 ){ page = 1; }
				from.SendGump( new ElementalSpellHelp( from, m_Book, page ) );
			}
		}
	}
}