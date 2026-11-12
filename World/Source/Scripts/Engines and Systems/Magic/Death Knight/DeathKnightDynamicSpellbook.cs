using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.DeathKnight
{
	// Native TazUO layout for the DeathKnight book; spells come from DeathKnightSpellProvider.
	public class DeathKnightDynamicSpellbook : DynamicSpellbook
	{
		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new DeathKnightDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.DeathKnight; } }
		public override byte ClientBookType { get { return 23; } }
		public override int ClientSpellBase { get { return 2300; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return DeathKnightSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			DeathKnightSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x2B00; } }
		public override ushort MinimizedGraphic { get { return 0x2B03; } }
		public override byte SpellsPerPageSide { get { return 7; } }
		public override byte MaxDictionaryPages { get { return 2; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override bool DisplayPowerWords { get { return true; } }
	}
}
