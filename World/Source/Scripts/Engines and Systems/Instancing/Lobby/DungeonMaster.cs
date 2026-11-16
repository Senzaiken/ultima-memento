using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Instancing
{
	public class DungeonMaster : BaseCreature
	{
		public static readonly TimeSpan[] TimeLimits = new TimeSpan[]
		{
			TimeSpan.FromMinutes( 15 ),
			TimeSpan.FromMinutes( 30 ),
			TimeSpan.FromMinutes( 45 ),
			TimeSpan.FromMinutes( 60 ),
			TimeSpan.Zero
		};

		private const int TalkRange = 4;

		private DungeonLobbyPortal m_Portal;
		private bool m_Spent;
		private bool m_Opening;

		public Serial HallKey;
		public Map ReturnMap;
		public Point3D ReturnPoint;

		public Item Portal { get { return m_Portal; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Spent { get { return m_Spent; } set { m_Spent = value; } }

		public static string GetTimeLimitLabel( TimeSpan limit )
		{
			return limit > TimeSpan.Zero ? String.Format( "{0} minutes", (int)limit.TotalMinutes ) : "Untimed";
		}

		[Constructable]
		public DungeonMaster() : base( AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4 )
		{
			Name = NameList.RandomName( "male" );
			Title = "the Dungeon Master";
			Body = 0x190;
			Hue = Utility.RandomSkinColor();
			SpeechHue = 0x22;
			NameHue = 0x22;

			Blessed = true;
			CantWalk = true;

			SetStr( 100 );
			SetDex( 100 );
			SetInt( 100 );
			SetHits( 100 );

			HairItemID = 0x203C;
			HairHue = 0x497;
			FacialHairItemID = 0x204C;
			FacialHairHue = 0x497;

			Dress( new Robe( 0x497 ) );
			Dress( new WizardsHat( 0x497 ) );
			Dress( new Sandals( 0x455 ) );
			Dress( new GnarledStaff() );
		}

		public DungeonMaster( Serial serial ) : base( serial )
		{
		}

		private void Dress( Item item )
		{
			item.Movable = false;
			item.LootType = LootType.Blessed;
			AddItem( item );
		}

		public override bool ClickTitle { get { return true; } }
		public override bool ShowFameTitle { get { return false; } }

		public override bool HandlesOnSpeech( Mobile from )
		{
			return from.Player && from.InRange( this, TalkRange );
		}

		public override void OnSpeech( SpeechEventArgs e )
		{
			if ( e.Handled || !e.Mobile.Player || !e.Mobile.InRange( this, TalkRange ) )
				return;

			e.Handled = true;
			Interact( e.Mobile );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.InRange( this, TalkRange ) && InLOS( from ) )
				Interact( from );
			else
				from.SendMessage( "He is too far away to hear you." );
		}

		private bool PortalIsOpen { get { return m_Portal != null && !m_Portal.Deleted; } }

		private void Interact( Mobile from )
		{
			Direction = GetDirectionTo( from );

			if ( m_Opening )
			{
				SayTo( from, "Patience. The way is opening." );
			}
			else if ( PortalIsOpen )
			{
				SayTo( from, String.Format( "The way is open to {0}: {1}, {2}. Climb the dais and step through.",
					m_Portal.DungeonName, m_Portal.Difficulty, GetTimeLimitLabel( m_Portal.TimeLimit ).ToLower() ) );
			}
			else if ( m_Spent )
			{
				SayTo( from, "Your trial here is done. The gate behind you leads home." );
			}
			else
			{
				from.CloseGump( typeof( DungeonMasterGump ) );
				from.SendGump( new DungeonMasterGump( from, this, Difficulty.Normal, 1 ) );
			}
		}

		public bool CanOpenFor( Mobile from )
		{
			return !Deleted && !m_Spent && !m_Opening && !PortalIsOpen && Object.ReferenceEquals( from.Map, Map ) && from.InRange( this, 12 );
		}

		public void OpenPortal( Mobile from, Difficulty difficulty, TimeSpan timeLimit )
		{
			if ( !CanOpenFor( from ) )
				return;

			DungeonInstanceType.DungeonInstanceDefinition def = DungeonInstanceType.Instance.GetRandomDefinition();
			if ( def == null )
			{
				SayTo( from, "Every road is closed to me today. Come back another time." );
				return;
			}

			m_Opening = true;
			Direction = Direction.North;
			Say( "So be it! Stand back..." );
			Animate( 17, 7, 1, true, false, 0 );
			FixedParticles( 0x376A, 9, 32, 5030, 0x21, 0, EffectLayer.Waist );
			PlaySound( 0x1F2 );

			Map map = Map;
			Point3D top = DungeonLobbyInstanceType.DaisTop;

			Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), delegate { Explode( map, top, 2 ); } );
			Timer.DelayCall( TimeSpan.FromSeconds( 2.0 ), delegate { Explode( map, top, 1 ); } );
			Timer.DelayCall( TimeSpan.FromSeconds( 3.0 ), delegate { FinishOpening( map, top, def, difficulty, timeLimit ); } );
		}

		private void Explode( Map map, Point3D top, int ring )
		{
			if ( Deleted )
				return;

			Animate( 17, 7, 1, true, false, 0 );

			for ( int i = 0; i < 4; i++ )
				Effects.SendLocationEffect( new Point3D( top.X + ( i % 2 == 0 ? -ring : ring ), top.Y + ( i < 2 ? -ring : ring ), top.Z ), map, 0x36BD, 20, 10, 0, 0 );

			Effects.SendLocationParticles( EffectItem.Create( top, map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 0x21, 0, 5052, 0 );
			Effects.PlaySound( top, map, 0x307 );
		}

		private void FinishOpening( Map map, Point3D top, DungeonInstanceType.DungeonInstanceDefinition def, Difficulty difficulty, TimeSpan timeLimit )
		{
			m_Opening = false;

			if ( Deleted || m_Spent || PortalIsOpen )
				return;

			m_Portal = new DungeonLobbyPortal( this, def.DisplayName, difficulty, timeLimit );
			m_Portal.MoveToWorld( top, map );

			Effects.SendLocationParticles( EffectItem.Create( top, map, EffectItem.DefaultDuration ), 0x375A, 9, 20, 0x21, 0, 5016, 0 );
			Effects.SendLocationParticles( EffectItem.Create( top, map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 0, 0, 5030, 0 );
			Effects.PlaySound( top, map, 0x20E );

			Direction = Direction.South;
			Say( String.Format( "The way is open. {0} awaits you{1}.", def.DisplayName,
				timeLimit > TimeSpan.Zero ? String.Format( ", and you have {0} minutes to defeat its master", (int)timeLimit.TotalMinutes ) : "" ) );
		}

		public void OnPortalSpent( DungeonLobbyPortal portal )
		{
			if ( portal != m_Portal )
				return;

			m_Portal = null;
			m_Spent = true;
			Say( "The way has closed. Your trial here is done." );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 ); // version

			writer.Write( (int) HallKey );
			writer.Write( (Map) ReturnMap );
			writer.Write( (Point3D) ReturnPoint );
			writer.Write( (Item) m_Portal );
			writer.Write( (bool) m_Spent );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( version >= 1 )
			{
				HallKey = (Serial)reader.ReadInt();
				ReturnMap = reader.ReadMap();
				ReturnPoint = reader.ReadPoint3D();
			}

			m_Portal = reader.ReadItem() as DungeonLobbyPortal;
			m_Spent = reader.ReadBool();
		}
	}
}
