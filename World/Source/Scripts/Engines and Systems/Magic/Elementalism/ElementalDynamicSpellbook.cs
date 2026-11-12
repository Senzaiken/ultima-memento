using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.Elementalism
{
	// Native TazUO layout for the elemental spellbook; spells come from ElementalSpellProvider.
	// Spells are grouped by sphere, and the bottom-left bookmark opens the elemental help gump.
	public class ElementalDynamicSpellbook : DynamicSpellbook
	{
		private const uint HelpToken = 1;

		private static readonly string[] m_PageNames = new string[]
		{
			"First Sphere", "Second Sphere", "Third Sphere", "Fourth Sphere",
			"Fifth Sphere", "Sixth Sphere", "Seventh Sphere", "Eighth Sphere"
		};

		private static readonly List<DynamicInfoPage> m_InfoPages = new List<DynamicInfoPage>
		{
			new DynamicInfoPage( "Elemental", "Unlike other forms of magic, elementalism relies on both the intellect and physical conditions of the spellcaster. No reagents are required. Casting requires mana and stamina, referred to as 'power'." ),
			new DynamicInfoPage( "Spellbook", "If you manage to find magical items that have lower reagent quality, then the stamina required for spells will be reduced proportionally by that value." )
		};

		private static readonly DynamicSpellbookBookmark m_Bookmark = new DynamicSpellbookBookmark
		{
			Graphic = 2095,
			PressedGraphic = 2095,
			X = 40,
			Y = 209,
			ActionType = DynamicBookmarkAction.ServerCallback,
			Action = HelpToken,
			Tooltip = "Elemental Magic Help"
		};

		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new ElementalDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.Elementalism; } }
		public override byte ClientBookType { get { return 21; } }
		public override int ClientSpellBase { get { return 2100; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return ElementalSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			ElementalSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 11138; } }
		public override ushort MinimizedGraphic { get { return 0x08BA; } }
		public override byte SpellsPerPageSide { get { return (byte)ElementalSpellProvider.SpellsPerCircle; } }
		public override byte MaxDictionaryPages { get { return 8; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override string ManaCostLabel { get { return "Power"; } }
		public override string MinSkillLabel { get { return "Min. Skill"; } }

		public override ushort BookHue { get { return 2807; } }
		public override ushort TextColor { get { return 0x0044; } }
		public override ushort SpellNameColor { get { return 0x0044; } }
		public override ushort TitleColor { get { return 0x0044; } }
		public override short ContentOffsetX { get { return -22; } }
		public override short ContentOffsetY { get { return -5; } }
		public override ushort PageTurnLeftGraphic { get { return 11159; } }
		public override ushort PageTurnRightGraphic { get { return 11160; } }
		public override short PageTurnLeftX { get { return 24; } }
		public override short PageTurnLeftY { get { return 8; } }
		public override short PageTurnRightX { get { return 295; } }
		public override short PageTurnRightY { get { return 8; } }
		public override ushort[] OverlayGraphics { get { return new ushort[] { 11152, 11147 }; } }

		public override string[] PageNames { get { return m_PageNames; } }
		public override List<DynamicInfoPage> InfoPages { get { return m_InfoPages; } }
		public override DynamicSpellbookBookmark Bookmark { get { return m_Bookmark; } }

		public override byte GetPage( SpellDefinition definition )
		{
			return (byte)ElementalSpellProvider.GetCircle( definition );
		}

		public override void OnBookmarkPressed( Mobile from, Spellbook book, uint token )
		{
			ElementalSpellbook elementalBook = book as ElementalSpellbook;

			if ( token != HelpToken || elementalBook == null )
				return;

			from.CloseGump( typeof( ElementalSpellHelp ) );
			from.SendGump( new ElementalSpellHelp( from, elementalBook, 1 ) );
		}
	}
}
