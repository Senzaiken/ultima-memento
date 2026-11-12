using System.Collections.Generic;
using System.Linq;

namespace Server.Spells.Herbalist
{
	public class DruidSpellProvider
	{
		public static readonly List<SpellDefinition> SpellDefinitions = new List<SpellDefinition>
		{
			ShieldOfEarthSpell.SpellInfo.SpellDefinition,
			WoodlandProtectionSpell.SpellInfo.SpellDefinition,
			ProtectiveFairySpell.SpellInfo.SpellDefinition,
			HerbalHealingSpell.SpellInfo.SpellDefinition,
			GraspingRootsSpell.SpellInfo.SpellDefinition,
			BlendWithForestSpell.SpellInfo.SpellDefinition,
			SwarmOfInsectsSpell.SpellInfo.SpellDefinition,
			VolcanicEruptionSpell.SpellInfo.SpellDefinition,
			TreefellowSpell.SpellInfo.SpellDefinition,
			StoneCircleSpell.SpellInfo.SpellDefinition,
			DruidicRuneSpell.SpellInfo.SpellDefinition,
			LureStoneSpell.SpellInfo.SpellDefinition,
			NaturesPassageSpell.SpellInfo.SpellDefinition,
			MushroomGatewaySpell.SpellInfo.SpellDefinition,
			RestorativeSoilSpell.SpellInfo.SpellDefinition,
			FireflySpell.SpellInfo.SpellDefinition,
		};

		public static int FirstSpellId
		{ get { return SpellDefinitions[0].SpellID; } }

		public static int LastSpellId
		{ get { return SpellDefinitions[SpellDefinitions.Count - 1].SpellID; } }

		public static int SpellCount
		{ get { return SpellDefinitions.Count; } }

		public static SpellDefinition GetDefinition(int spellID)
		{
			var spellDefinition = SpellDefinitions.FirstOrDefault(definition => definition.SpellID == spellID);

			return spellDefinition;
		}
	}
}
