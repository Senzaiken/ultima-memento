using System;
using System.Collections.Generic;
using Server.Items;
using Server.Spells.Dynamic;

namespace Server.Spells.Research
{
	// Native TazUO layout for the ancient (archmage) spellbook; spells come from ResearchSpellProvider.
	// Spells are grouped by school and listed by circle, in the same order the research system uses.
	public class ResearchDynamicSpellbook : DynamicSpellbook
	{
		private const int SchoolCount = 8;

		private static string[] m_PageNames;

		public static void Initialize()
		{
			DynamicSpellbookManager.Register( new ResearchDynamicSpellbook() );
		}

		public override SpellbookType SpellbookType { get { return SpellbookType.Archmage; } }
		public override byte ClientBookType { get { return 28; } }
		public override int ClientSpellBase { get { return 2800; } }
		public override IList<SpellDefinition> SpellDefinitions { get { return ResearchSpellProvider.SpellDefinitions; } }

		public override void Cast( Mobile from, int spellId )
		{
			ResearchSpellProvider.Cast( from, spellId );
		}

		public override ushort BookGraphic { get { return 0x08AC; } }
		public override ushort MinimizedGraphic { get { return 0x08BA; } }
		public override byte SpellsPerPageSide { get { return 8; } }
		public override byte MaxDictionaryPages { get { return 8; } }
		public override bool DisplayManaCost { get { return true; } }
		public override bool DisplayMinSkill { get { return true; } }

		public override string[] PageNames
		{
			get
			{
				if ( m_PageNames == null )
				{
					// Research indexes run circle by circle through the schools, so the first circle lists every school once.
					string[] names = new string[SchoolCount];

					for ( int i = 0; i < SchoolCount; i++ )
						names[i] = Server.Misc.Research.SpellInformation( i + 1, 3 );

					m_PageNames = names;
				}

				return m_PageNames;
			}
		}

		public override byte GetPage( SpellDefinition definition )
		{
			int index = ResearchSpellProvider.GetResearchIndex( definition.SpellID );

			return index > 0 ? (byte)( ( index - 1 ) % SchoolCount + 1 ) : (byte)0;
		}

		// Within a school, list spells by circle like the research bag and the legacy gump do.
		public override int GetSortKey( SpellDefinition definition )
		{
			return ResearchSpellProvider.GetResearchIndex( definition.SpellID );
		}

		public override string[] GetReagents( SpellDefinition definition )
		{
			int index = ResearchSpellProvider.GetResearchIndex( definition.SpellID );
			if ( index <= 0 )
				return null;

			string reagents = Server.Misc.Research.SpellInformation( index, 5 );

			return string.IsNullOrEmpty( reagents ) ? null : reagents.Split( new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries );
		}
	}
}
