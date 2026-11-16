using System;
using Server;

namespace Server.Engines.Instancing
{
	public class DungeonInstanceExitTeleporter : InstancePortal
	{
		[Constructable]
		public DungeonInstanceExitTeleporter() : this( 7107 )
		{
		}

		public DungeonInstanceExitTeleporter( int itemID ) : base( itemID > 0 ? itemID : 7107 )
		{
			Name = "a dungeon instance exit";
			Hue = 0x455;
			Weight = -2;
		}

		public DungeonInstanceExitTeleporter( Serial serial ) : base( serial )
		{
		}

		protected override bool Use( Mobile from )
		{
			return DungeonInstanceType.Instance.LeaveInstance( from );
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			list.Add( 1070722, "Walk over or double-click to leave the instance" );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
