using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.Mystic
{
	// Native TazUO layout for the Mystic book; spells come from MysticSpellProvider.
	public class MysticDynamicSpellbook : DynamicSpellbook
	{
		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new MysticDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.Mystic; } }
		public override byte ClientBookType { get { return 32; } }
		public override int ClientSpellBase { get { return 3200; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return MysticSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			MysticSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x2B01; } }
		public override ushort MinimizedGraphic { get { return 0x2B04; } }
		public override byte SpellsPerPageSide { get { return 5; } }
		public override byte MaxDictionaryPages { get { return 2; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override bool DisplayPowerWords { get { return true; } }

		public override string GetDescription( SpellDefinition definition )
		{
			return MysticSpellProvider.GetDescription( definition );
		}
	}
}
