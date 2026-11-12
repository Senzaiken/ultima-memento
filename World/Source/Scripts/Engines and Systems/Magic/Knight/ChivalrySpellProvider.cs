using Server.Items;
using System.Collections.Generic;
using System.Linq;

namespace Server.Spells.Chivalry
{
	public class ChivalrySpellProvider
	{
		public static readonly List<SpellDefinition> SpellDefinitions = new List<SpellDefinition>
		{
			CleanseByFireSpell.SpellInfo.SpellDefinition,
			CloseWoundsSpell.SpellInfo.SpellDefinition,
			ConsecrateWeaponSpell.SpellInfo.SpellDefinition,
			DispelEvilSpell.SpellInfo.SpellDefinition,
			DivineFurySpell.SpellInfo.SpellDefinition,
			EnemyOfOneSpell.SpellInfo.SpellDefinition,
			HolyLightSpell.SpellInfo.SpellDefinition,
			NobleSacrificeSpell.SpellInfo.SpellDefinition,
			RemoveCurseSpell.SpellInfo.SpellDefinition,
			SacredJourneySpell.SpellInfo.SpellDefinition,
		};

		public static int FirstSpellId
		{ get { return SpellDefinitions[0].SpellID; } }

		public static int LastSpellId
		{ get { return SpellDefinitions[SpellDefinitions.Count - 1].SpellID; } }

		public static int SpellCount
		{ get { return SpellDefinitions.Count; } }

		public static void Cast(Mobile from, int spellID)
		{
			if (!Multis.DesignContext.Check(from)) return;

			if (HasSpell(from, spellID))
				SpellRegistry.NewSpell(spellID, from, null).Cast();
			else
				from.SendLocalizedMessage(500015); // You do not have that spell!
		}

		public static SpellDefinition GetDefinition(int spellID)
		{
			var spellDefinition = SpellDefinitions.FirstOrDefault(definition => definition.SpellID == spellID);

			return spellDefinition;
		}

		public static bool HasSpell(Mobile from, int spellID)
		{
			Spellbook book = Spellbook.Find(from, spellID, SpellbookType.Paladin);

			return book != null && book.HasSpell(spellID);
		}
	}
}
