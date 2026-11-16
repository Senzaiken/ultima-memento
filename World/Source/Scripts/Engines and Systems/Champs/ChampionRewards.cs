using System;
using Server.Items;
using Server.Misc;
using Server.Mobiles;

namespace Server.Engines.CannedEvil
{
	public class ChampionRewards
	{
		public static int GetBossItemDropChance(int spawnSize, Difficulty difficulty)
		{
			return 100;
		}

		public static int GetGoldPercent(int spawnSize, Difficulty difficulty)
		{
			const int FLAT_BONUS = 25;

			// 10% per Difficulty -- up to 60%
			var difficultyBonus = 10 * (int)difficulty;

			// 5% per Size -- up to 60%
			var sizeBonus = 5 * spawnSize;

			// Size starts at 1, so flat + 5% = 30% base
			return Math.Min(100, FLAT_BONUS + difficultyBonus + sizeBonus);
		}

		public static int GetArtifactDropChance(int spawnSize, Difficulty difficulty)
		{
			// 10% per Difficulty -- up to 60%
			var difficultyBonus = 10 * (int)difficulty;

			// 5% per Size -- up to 60%
			var sizeBonus = 5 * spawnSize;

			return Math.Min(100, difficultyBonus + sizeBonus);
		}

		public static int GetPowerscrollDropCount(int spawnSize, Difficulty difficulty)
		{
			// 1 per Difficulty -- up to 6
			var difficultyBonus = 1 + (int)difficulty;

			// 1 per 3 Size -- up to 4
			var sizeBonus = spawnSize / 3;

			return difficultyBonus + sizeBonus;
		}

		// A HOARD MINION ALWAYS, AND BY CHANCE A TREASURE CHEST, A POWERFUL ITEM AND AN ARTIFACT
		public static void AddBossRewards(Container c, BaseCreature boss, int treasureChestChance, int bossItemChance, int artifactChance)
		{
			c.DropItem(new HoardMinionFamiliarItem());

			if (treasureChestChance > 0 && Utility.RandomDouble() < treasureChestChance / 100.0)
			{
				LootChest chest = new LootChest(10);
				ContainerFunctions.MakeDemonBox(chest, boss);
				c.DropItem(chest);
			}

			if (bossItemChance > 0 && Utility.RandomDouble() < bossItemChance / 100.0)
			{
				var killer = MobileUtilities.TryGetMasterPlayer(boss);
				var item = Loot.RandomMagicalItem(LootPackEntry.playOrient(killer));
				item = LootPackEntry.Enchant(killer, 500, item);
				string owner = boss.Name;
				if (!string.IsNullOrWhiteSpace(boss.Title)) { owner = boss.Name + " " + boss.Title; }
				item.InfoText1 = string.Format("[Belonged to: {0}]", owner);
				c.DropItem(item);
			}

			if (artifactChance > 0 && Utility.RandomDouble() < artifactChance / 100.0)
				c.DropItem(Loot.RandomArty());
		}

		public static int GetTreasureChestDropChance(int spawnSize, Difficulty difficulty)
		{
			// 20% per Difficulty -- up to 60%
			var difficultyBonus = 20 * (int)difficulty;

			// 10% per Size -- up to 120%
			var sizeBonus = 10 * spawnSize;

			return Math.Min(100, difficultyBonus + sizeBonus);
		}
	}
}