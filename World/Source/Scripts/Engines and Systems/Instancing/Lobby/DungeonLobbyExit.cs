using System;
using Server;

namespace Server.Engines.Instancing
{
	// The gate at the foot of a dungeon hall, back to where its rift stood.
	public class DungeonLobbyExit : Item
	{
		public DungeonLobbyExit() : base( 0xF6C )
		{
			Name = "the way back";
			Movable = false;
			Hue = 0x497;
			Light = LightType.Circle150;
		}

		public DungeonLobbyExit( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from == null )
				return;

			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.SendLocalizedMessage( 500446 ); // That is too far away.
				return;
			}

			DungeonLobbyInstanceType.Instance.LeaveInstance( from );
		}

		public override bool OnMoveOver( Mobile m )
		{
			if ( m == null || !m.Player )
				return true;

			return !DungeonLobbyInstanceType.Instance.LeaveInstance( m );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			reader.ReadInt();
		}
	}
}
