using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Instancing
{
	// THE SKY DWELLING ROPE: PLAYERS GET THE CHOOSER, STAFF CLIMB TO THE TEMPLATE
	public class SkyDwellingTeleporter : KeywordTeleporter
	{
		[Constructable]
		public SkyDwellingTeleporter()
		{
		}

		public override void StartTeleport( Mobile m )
		{
			if ( m.AccessLevel > AccessLevel.Player )
			{
				base.StartTeleport( m );
				return;
			}

			m.CloseGump( typeof( SkyDwellingChooserGump ) );
			new SkyDwellingChooserGump( m );
		}

		public SkyDwellingTeleporter( Serial serial ) : base( serial )
		{
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
