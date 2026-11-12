using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.HolyMan
{
	// Native TazUO layout for the HolyMan book; spells come from HolyManSpellProvider.
	public class HolyManDynamicSpellbook : DynamicSpellbook
	{
		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new HolyManDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.HolyMan; } }
		public override byte ClientBookType { get { return 25; } }
		public override int ClientSpellBase { get { return 2500; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return HolyManSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			HolyManSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x2B01; } }
		public override ushort MinimizedGraphic { get { return 0x2B04; } }
		public override byte SpellsPerPageSide { get { return 7; } }
		public override byte MaxDictionaryPages { get { return 2; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override bool DisplayPowerWords { get { return true; } }
	}
}
