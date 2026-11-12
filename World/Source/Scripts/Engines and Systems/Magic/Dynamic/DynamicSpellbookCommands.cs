using Server.Commands;
using Server.Items;

namespace Server.Spells.Dynamic
{
	public static class DynamicSpellbookCommands
	{
		public static void Initialize()
		{
			CommandSystem.Register( "DynamicSpellbooks", AccessLevel.GameMaster, new CommandEventHandler( DynamicSpellbooks_OnCommand ) );
		}

		[Usage( "DynamicSpellbooks [refresh]" )]
		[Description( "Lists the spellbooks shown as native TazUO books and whether you are using a capable client. 'refresh' rebuilds them and makes clients reload open books." )]
		private static void DynamicSpellbooks_OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;

			if ( e.Length > 0 && Insensitive.Equals( e.GetString( 0 ), "refresh" ) )
			{
				DynamicSpellbookManager.InvalidateAll( true );
				from.SendMessage( "Dynamic spellbooks rebuilt; clients will reload open books." );
				return;
			}

			from.SendMessage( "Your client {0} native spellbooks.", DynamicSpellbookManager.IsCapable( from.NetState ) ? "supports" : "does not support" );

			foreach ( DynamicSpellbook book in DynamicSpellbookManager.Books )
			{
				from.SendMessage( "{0}: client type {1}, spell ids {2}-{3}", book.SpellbookType, book.ClientBookType, book.ClientSpellBase, book.ClientSpellBase + book.SpellDefinitions.Count - 1 );
			}
		}
	}
}
