using Server.Commands;
using Server.Spells;
using Server.Spells.Elementalism;
using System.Collections.Generic;

namespace Server.Scripts.Commands
{
	public class ElementalCommands
	{
		private static List<Docs.DocCommandEntry> m_HelpEntries;

		public static void Initialize()
		{
			m_HelpEntries = CastSpellCommand.RegisterAll(
				"Elementalism",
				ElementalSpellProvider.SpellDefinitions,
				ElementalSpellProvider.Cast,
				definition =>
				{
					// Legacy commands, e.g. [EArmor for Elemental Armor
					return new string[] { GetLegacyCommand(definition) };
				}
			);

			CommandSystem.Register("Spells_Elementalism", AccessLevel.Player, new CommandEventHandler(SpellsElementalism_OnCommand));
		}

		public static string GetLegacyCommand(SpellDefinition definition)
		{
			return string.Format("E{0}", ElementalSpellProvider.GetShortName(definition));
		}

		[Usage("Spells_Elementalism")]
		[Description("Lists Elementalism spell cast commands.")]
		private static void SpellsElementalism_OnCommand(CommandEventArgs e)
		{
			e.Mobile.SendGump(new HelpInfo.CommandListGump(0, e.Mobile, m_HelpEntries));
		}
	}
}
