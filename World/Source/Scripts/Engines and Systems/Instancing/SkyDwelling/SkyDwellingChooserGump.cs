using System;
using System.Collections.Generic;
using Server;
using Knives.TownHouses;

namespace Server.Engines.Instancing
{
	public class SkyDwellingChooserGump : GumpPlusLight
	{
		private const int Width = 340;

		public SkyDwellingChooserGump( Mobile m ) : base( m, 100, 100 )
		{
		}

		private static SkyDwellingInstanceType Sky { get { return SkyDwellingInstanceType.Instance; } }

		protected override void BuildGump()
		{
			int y = 0;

			AddHtml( 0, y += 10, Width, "<CENTER>Where would you like to go?" );
			AddHtml( 12, y += 28, Width, "<BASEFONT COLOR=#FFD700>Your Sky Dwelling</BASEFONT>" );

			AddButton( 20, y += 22, 0xFA5, 0xFA7, "own", new GumpCallback( EnterOwn ) );
			if ( Sky.OwnsDwelling( Owner ) )
				AddHtml( 55, y + 2, Width - 60, String.Format( "Enter your sky dwelling{0}", Sky.IsPublic( Owner ) ? " (Public)" : "" ) );
			else
				AddHtml( 55, y + 2, Width - 60, "Available Sky Dwelling -- visit & purchase" );

			y = AddSection( y, "<BASEFONT COLOR=#88FF88>Dwellings You May Visit</BASEFONT>", Sky.InvitedInstances( Owner ) );
			y = AddSection( y, "<BASEFONT COLOR=#88CCFF>Public Sky Dwellings</BASEFONT>", Sky.PublicInstances( Owner ) );

			AddBackgroundZero( 0, 0, Width, y + 45, 0x1453 );
		}

		private int AddSection( int y, string title, List<Instance> list )
		{
			if ( list.Count == 0 )
				return y;

			AddHtml( 12, y += 30, Width, title );

			for ( int i = 0; i < list.Count; i++ )
			{
				Mobile owner = World.FindMobile( list[i].OwnerSerial );
				AddButton( 20, y += 22, 0xFA5, 0xFA7, new GumpStateCallback( EnterInstance ), list[i].OwnerSerial );
				AddHtml( 55, y + 2, Width - 60, owner != null ? String.Format( "{0}'s Sky Dwelling", owner.Name ) : "a sky dwelling" );
			}

			return y;
		}

		private void EnterOwn()
		{
			Sky.SendOwnerToTheirInstance( Owner );
		}

		private void EnterInstance( object state )
		{
			Sky.EnterChosen( Owner, (Serial)state );
		}
	}
}
