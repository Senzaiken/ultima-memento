using Server.Items;
using System.Collections.Generic;
using System.Linq;

namespace Server.Spells.Elementalism
{
	public class ElementalSpellProvider
	{
		public const int SpellsPerCircle = 4;

		private const string NamePrefix = "Elemental ";

		public static readonly List<SpellDefinition> SpellDefinitions = new List<SpellDefinition>
		{
			// First Sphere
			Elemental_Armor_Spell.SpellInfo.SpellDefinition,
			Elemental_Bolt_Spell.SpellInfo.SpellDefinition,
			Elemental_Mend_Spell.SpellInfo.SpellDefinition,
			Elemental_Sanctuary_Spell.SpellInfo.SpellDefinition,

			// Second Sphere
			Elemental_Pain_Spell.SpellInfo.SpellDefinition,
			Elemental_Protection_Spell.SpellInfo.SpellDefinition,
			Elemental_Purge_Spell.SpellInfo.SpellDefinition,
			Elemental_Steed_Spell.SpellInfo.SpellDefinition,

			// Third Sphere
			Elemental_Call_Spell.SpellInfo.SpellDefinition,
			Elemental_Force_Spell.SpellInfo.SpellDefinition,
			Elemental_Wall_Spell.SpellInfo.SpellDefinition,
			Elemental_Warp_Spell.SpellInfo.SpellDefinition,

			// Fourth Sphere
			Elemental_Field_Spell.SpellInfo.SpellDefinition,
			Elemental_Restoration_Spell.SpellInfo.SpellDefinition,
			Elemental_Strike_Spell.SpellInfo.SpellDefinition,
			Elemental_Void_Spell.SpellInfo.SpellDefinition,

			// Fifth Sphere
			Elemental_Blast_Spell.SpellInfo.SpellDefinition,
			Elemental_Echo_Spell.SpellInfo.SpellDefinition,
			Elemental_Fiend_Spell.SpellInfo.SpellDefinition,
			Elemental_Hold_Spell.SpellInfo.SpellDefinition,

			// Sixth Sphere
			Elemental_Barrage_Spell.SpellInfo.SpellDefinition,
			Elemental_Rune_Spell.SpellInfo.SpellDefinition,
			Elemental_Storm_Spell.SpellInfo.SpellDefinition,
			Elemental_Summon_Spell.SpellInfo.SpellDefinition,

			// Seventh Sphere
			Elemental_Devastation_Spell.SpellInfo.SpellDefinition,
			Elemental_Fall_Spell.SpellInfo.SpellDefinition,
			Elemental_Gate_Spell.SpellInfo.SpellDefinition,
			Elemental_Havoc_Spell.SpellInfo.SpellDefinition,

			// Eighth Sphere
			Elemental_Apocalypse_Spell.SpellInfo.SpellDefinition,
			Elemental_Lord_Spell.SpellInfo.SpellDefinition,
			Elemental_Soul_Spell.SpellInfo.SpellDefinition,
			Elemental_Spirit_Spell.SpellInfo.SpellDefinition,
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

		/// <summary>
		/// Zero-based sphere (circle) of the spell; each sphere holds SpellsPerCircle consecutive spells.
		/// </summary>
		public static int GetCircle(SpellDefinition definition)
		{
			return (definition.SpellID - FirstSpellId) / SpellsPerCircle;
		}

		public static SpellDefinition GetDefinition(int spellID)
		{
			var spellDefinition = SpellDefinitions.FirstOrDefault(definition => definition.SpellID == spellID);

			return spellDefinition;
		}

		/// <summary>
		/// The spell name without the "Elemental" prefix (e.g. "Armor"), as shown in the spellbook and spell bars.
		/// </summary>
		public static string GetShortName(SpellDefinition definition)
		{
			string name = definition.Name.String;

			return name.StartsWith(NamePrefix) ? name.Substring(NamePrefix.Length) : name;
		}

		public static bool HasSpell(Mobile from, int spellID)
		{
			// Spellbook.Find only returns elemental books the caster can use (ElementalSpell.CanUseBook)
			Spellbook book = Spellbook.Find(from, spellID, SpellbookType.Elementalism);

			return book != null && book.HasSpell(spellID);
		}
	}
}
