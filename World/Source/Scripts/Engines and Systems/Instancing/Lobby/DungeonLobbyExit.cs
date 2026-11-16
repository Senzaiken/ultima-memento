using System;
using Server;

namespace Server.Engines.Instancing
{
	public class DungeonLobbyExit : InstancePortal
	{
		public DungeonLobbyExit() : base( 0xF6C )
		{
			Name = "the way back";
			Hue = 0x497;
			Light = LightType.Circle150;
		}

		public DungeonLobbyExit( Serial serial ) : base( serial )
		{
		}

		protected override bool Use( Mobile from )
		{
			return DungeonLobbyInstanceType.Instance.LeaveInstance( from );
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
