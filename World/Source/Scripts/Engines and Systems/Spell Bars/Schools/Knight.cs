using Server.Mobiles;
using Server.Spells.Chivalry;

namespace Server.SpellBars
{
	public sealed class KnightSpellSchool : ISpellSchool
	{
		public static readonly KnightSpellSchool Instance = new KnightSpellSchool();

		public int MaxSlots
		{ get { return ChivalrySpellProvider.SpellCount; } }

		public SpellBarSchool School
		{ get { return SpellBarSchool.Knight; } }

		public int GetBackgroundImage(PlayerMobile from)
		{ return 11167; }

		public int GetIcon(PlayerMobile from, int slotIndex)
		{ return ChivalrySpellProvider.SpellDefinitions[slotIndex - 1].IconGraphic; }

		public string GetName(int slotIndex)
		{
			if (slotIndex < 1 || slotIndex > ChivalrySpellProvider.SpellCount)
				return string.Empty;

			return ChivalrySpellProvider.SpellDefinitions[slotIndex - 1].Name.String;
		}

		public int GetRegistrySpellId(int slotIndex)
		{ return ChivalrySpellProvider.SpellDefinitions[slotIndex - 1].SpellID; }

		public bool HasSpell(PlayerMobile from, int registrySpellId)
		{
			return ChivalrySpellProvider.HasSpell(from, registrySpellId);
		}
	}

	public sealed class SpellBarSetupGump_Knight_1 : SpellBarSetupGump
	{
		public SpellBarSetupGump_Knight_1(PlayerMobile from, int origin) : base(SpellBarId.Knight_1, from, origin)
		{
		}
	}

	public sealed class SpellBarSetupGump_Knight_2 : SpellBarSetupGump
	{
		public SpellBarSetupGump_Knight_2(PlayerMobile from, int origin) : base(SpellBarId.Knight_2, from, origin)
		{
		}
	}

	public sealed class SpellBarToolbarGump_Knight_1 : SpellBarToolbarGump
	{
		public SpellBarToolbarGump_Knight_1(PlayerMobile from) : base(SpellBarId.Knight_1, from)
		{
		}
	}

	public sealed class SpellBarToolbarGump_Knight_2 : SpellBarToolbarGump
	{
		public SpellBarToolbarGump_Knight_2(PlayerMobile from) : base(SpellBarId.Knight_2, from)
		{
		}
	}
}