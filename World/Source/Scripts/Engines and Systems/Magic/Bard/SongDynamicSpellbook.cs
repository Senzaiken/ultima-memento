using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.Song
{
	// Native TazUO layout for the Song book; spells come from BardSongProvider.
	public class SongDynamicSpellbook : DynamicSpellbook
	{
		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new SongDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.Song; } }
		public override byte ClientBookType { get { return 22; } }
		public override int ClientSpellBase { get { return 2200; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return BardSongProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			BardSongProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x2B01; } }
		public override ushort MinimizedGraphic { get { return 0x2B04; } }
		public override byte SpellsPerPageSide { get { return 8; } }
		public override byte MaxDictionaryPages { get { return 2; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }
		public override bool DisplayPowerWords { get { return true; } }
	}
}
