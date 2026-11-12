using System.Collections.Generic;
using System.Linq;

namespace Server.Spells.Undead
{
	public class WitchSpellProvider
	{
		public static readonly List<SpellDefinition> SpellDefinitions = new List<SpellDefinition>
		{
			SpectreShadowSpell.SpellInfo.SpellDefinition,
			ManaLeechSpell.SpellInfo.SpellDefinition,
			UndeadCurePoisonSpell.SpellInfo.SpellDefinition,
			HellsBrandSpell.SpellInfo.SpellDefinition,
			UndeadGraveyardGatewaySpell.SpellInfo.SpellDefinition,
			RetchedAirSpell.SpellInfo.SpellDefinition,
			UndeadEyesSpell.SpellInfo.SpellDefinition,
			UndeadWallOfSpikesSpell.SpellInfo.SpellDefinition,
			VampireGiftSpell.SpellInfo.SpellDefinition,
			UnBloodlessSpecPactSpell.SpellInfo.SpellDefinition,
			NecroPoisonSpell.SpellInfo.SpellDefinition,
			HellsGateSpell.SpellInfo.SpellDefinition,
			GhostlyImagesSpell.SpellInfo.SpellDefinition,
			GhostPhaseSpell.SpellInfo.SpellDefinition,
			NecroUnlockSpell.SpellInfo.SpellDefinition,
			PhantasmSpell.SpellInfo.SpellDefinition,
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
