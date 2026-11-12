using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.Syth
{
	// Native TazUO layout for the Syth book; spells come from SythSpellProvider.
	public class SythDynamicSpellbook : DynamicSpellbook
	{
		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new SythDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.Syth; } }
		public override byte ClientBookType { get { return 30; } }
		public override int ClientSpellBase { get { return 3000; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return SythSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			SythSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x2B00; } }
		public override ushort MinimizedGraphic { get { return 0x2B03; } }
		public override byte SpellsPerPageSide { get { return 5; } }
		public override byte MaxDictionaryPages { get { return 2; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override bool DisplayPowerWords { get { return false; } }
	}
}
