using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.Jedi
{
	// Native TazUO layout for the Jedi book; spells come from JediSpellProvider.
	public class JediDynamicSpellbook : DynamicSpellbook
	{
		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new JediDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.Jedi; } }
		public override byte ClientBookType { get { return 26; } }
		public override int ClientSpellBase { get { return 2600; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return JediSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			JediSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x2B01; } }
		public override ushort MinimizedGraphic { get { return 0x2B04; } }
		public override byte SpellsPerPageSide { get { return 5; } }
		public override byte MaxDictionaryPages { get { return 2; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override bool DisplayPowerWords { get { return false; } }
	}
}
