using System;
using Server;

namespace Server.Engines.Instancing
{
	// WALK OVER OR DOUBLE-CLICK TO USE
	public abstract class InstancePortal : Item
	{
		public InstancePortal( int itemID ) : base( itemID )
		{
			Movable = false;
		}

		public InstancePortal( Serial serial ) : base( serial )
		{
		}

		protected abstract bool Use( Mobile m );

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.InRange( GetWorldLocation(), 2 ) )
				Use( from );
			else
				from.SendLocalizedMessage( 500446 ); // That is too far away.
		}

		public override bool OnMoveOver( Mobile m )
		{
			return !m.Player || !Use( m );
		}
	}
}
