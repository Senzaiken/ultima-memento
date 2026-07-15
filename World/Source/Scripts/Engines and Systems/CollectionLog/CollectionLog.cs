using System;
using Server.Accounting;
using Server.Collectibles;
using Server.Gumps;
using Server.Targeting;

namespace Server.Items
{
	public class CollectionLog : Item
	{
		public override string DefaultName { get { return "Collection Log"; } }

		[Constructable]
		public CollectionLog() : base( 0x2253 )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;
			Hue = 0x47E;
		}

		public CollectionLog( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from == null )
				return;

			if ( !IsChildOf( from.Backpack ) && Parent != from )
			{
				from.SendMessage( "The Collection Log must be in your backpack to open." );
				return;
			}

			var account = from.Account as Account;
			if ( account == null )
			{
				from.SendMessage( "Your account could not be located." );
				return;
			}

			from.CloseGump( typeof( CollectionLogGump ) );
			from.SendGump( new CollectionLogGump( from, this, CollectionLogCatalog.Categories[0].ID ) );
		}

		public void BeginScanTarget( Mobile from )
		{
			from.SendMessage( "Select an item to add to your Collection Log." );
			from.Target = new ScanTarget( this );
		}

		private class ScanTarget : Target
		{
			private readonly CollectionLog m_Log;

			public ScanTarget( CollectionLog log ) : base( 8, false, TargetFlags.None )
			{
				m_Log = log;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Log == null || m_Log.Deleted )
					return;

				var item = targeted as Item;
				if ( item == null )
				{
					from.SendMessage( "That isn't an item." );
					Reopen( from );
					return;
				}

				if ( !item.IsChildOf( from.Backpack ) && item.RootParent != from )
				{
					from.SendMessage( "You can only scan items you are carrying." );
					Reopen( from );
					return;
				}

				int canonical = CollectionLogCatalog.Resolve( item.ItemID );

				if ( canonical < 0 )
				{
					from.SendMessage( "That item's graphic is not tracked by the Collection Log." );
					Reopen( from );
					return;
				}

				var account = from.Account as Account;
				if ( account == null )
				{
					from.SendMessage( "Your account could not be located." );
					return;
				}

				if ( CollectionLogStorage.MarkCollected( account, canonical ) )
					from.SendMessage( "Added to your Collection Log." );
				else
					from.SendMessage( "You have already collected that graphic." );

				Reopen( from );
			}

			private void Reopen( Mobile from )
			{
				if ( m_Log == null || m_Log.Deleted || from == null )
					return;

				from.CloseGump( typeof( CollectionLogGump ) );
				from.SendGump( new CollectionLogGump( from, m_Log, CollectionLogCatalog.Categories[0].ID ) );
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
