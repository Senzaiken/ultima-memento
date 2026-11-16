using System;
using Server;
using Server.Items;
using Knives.TownHouses;

namespace Server.Engines.Instancing
{
	// BUYING IT CLAIMS THE DWELLING IT HANGS IN; IT THEN BECOMES THE OWNER'S PUBLIC/PRIVATE SWITCH
	[Flipable( 0xC0B, 0xC0C )]
	public class SkyDwellingSign : TownHouseSign
	{
		private bool m_Owned;
		private Serial m_OwnerSerial;

		public bool OwnedSign { get { return m_Owned; } }

		[Constructable]
		public SkyDwellingSign()
		{
			Name = "Purchase this Sky Dwelling";
		}

		public SkyDwellingSign( Serial serial ) : base( serial )
		{
		}

		public void SetOwned( Serial owner )
		{
			m_Owned = true;
			m_OwnerSerial = owner;
			Name = "Sky Dwelling Management";
			Visible = true;
			InvalidateProperties();
		}

		public override void OnDoubleClick( Mobile m )
		{
			if ( m.AccessLevel != AccessLevel.Player )
			{
				base.OnDoubleClick( m );
				return;
			}

			if ( !Visible )
				return;

			if ( m_Owned )
			{
				Mobile owner = World.FindMobile( m_OwnerSerial );
				if ( m == owner )
					new SkyDwellingManagementGump( m );
				else
					m.SendMessage( "This sky dwelling belongs to {0}.", owner != null ? owner.Name : "someone else" );
			}
			else if ( SkyDwellingInstanceType.Instance.OwnsDwelling( m ) )
				m.SendMessage( "You already own a sky dwelling." );
			else if ( CanBuyHouse( m ) )
				new TownHouseConfirmGump( m, this );
			else
				m.SendMessage( "You cannot purchase a sky dwelling." );
		}

		public override void Purchase( Mobile m, bool sellitems )
		{
			if ( m_Owned )
				return;

			if ( SkyDwellingInstanceType.Instance.OwnsDwelling( m ) )
			{
				m.SendMessage( "You already own a sky dwelling." );
				return;
			}

			if ( !CanBuyHouse( m ) )
			{
				m.SendMessage( "You cannot purchase a sky dwelling." );
				return;
			}

			int price = Free ? 0 : Price;

			if ( m.AccessLevel == AccessLevel.Player && !Server.Mobiles.Banker.Withdraw( m, price ) )
			{
				m.SendMessage( "You cannot afford this sky dwelling." );
				return;
			}

			if ( m.AccessLevel == AccessLevel.Player && price > 0 )
				m.SendLocalizedMessage( 1060398, price.ToString() ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.

			SkyDwellingInstanceType.Instance.Purchase( m );
			SetOwned( m.Serial );
			m.SendMessage( "You have purchased this sky dwelling! Double-click this sign to set it public or private." );
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			if ( m_Owned )
			{
				Mobile owner = World.FindMobile( m_OwnerSerial );
				list.Add( 1070722, owner != null ? String.Format( "{0}'s sky dwelling -- double-click to manage", owner.Name ) : "an owned sky dwelling" );
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 ); // version
			writer.Write( (bool) m_Owned );
			writer.Write( (int) m_OwnerSerial );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( version >= 1 )
			{
				m_Owned = reader.ReadBool();
				m_OwnerSerial = (Serial)reader.ReadInt();
			}
		}
	}
}
