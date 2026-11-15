using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Instancing
{
	// Left behind where an outdoor demon comes into the world. The rift stays sealed while its demon
	// lives; slaying the demon opens it for a while, and it leads to a Dungeon Master's holding room.
	public class DemonRiftPortal : Item
	{
		public static double SpawnChance = 0.10;
		public static int MaxActive = 20;
		public static int MinSpacing = 40;
		public static TimeSpan OpenDuration = TimeSpan.FromMinutes( 30 );

		private const int SealedHue = 0x497;
		private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds( 5.0 );

		private static readonly List<DemonRiftPortal> m_Active = new List<DemonRiftPortal>();

		private static readonly Type[] ExcludedDemons = new Type[]
		{
			typeof( SummonedDaemon ), typeof( SummonedDaemonGreater ), typeof( GuardianWolf ),
			typeof( PirateDaemon ), typeof( PirateDemon ), typeof( PirateDemoness ), typeof( PirateDevil ), typeof( PirateSuccubus ),
			typeof( DemonOfTheSea ), typeof( DeepSeaDevil )
		};

		private Mobile m_Demon;
		private bool m_Open;
		private DateTime m_ClosesAt;
		private readonly List<Serial> m_Entrants = new List<Serial>();
		private Timer m_Timer;

		public static int ActiveCount { get { return m_Active.Count; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Demon { get { return m_Demon; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsOpen
		{
			get { return m_Open; }
			set
			{
				if ( value && !m_Open )
					Open();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan TimeUntilClose
		{
			get { return m_Open && m_ClosesAt > DateTime.Now ? m_ClosesAt - DateTime.Now : TimeSpan.Zero; }
		}

		public DemonRiftPortal( Mobile demon ) : base( 0xDDA )
		{
			Movable = false;
			Light = LightType.Circle150;
			m_Demon = demon;
			ApplySealedLook();

			m_Active.Add( this );
			StartTimer();
		}

		public DemonRiftPortal( Serial serial ) : base( serial )
		{
		}

		private void ApplySealedLook()
		{
			Name = "a sealed demonic rift";
			Hue = SealedHue;
		}

		private void ApplyOpenLook()
		{
			Name = "a demonic rift";
			Hue = 0;
		}

		// ----- Spawning -----

		public static void Configure()
		{
			CommandSystem.Register( "demonrift", AccessLevel.GameMaster, new CommandEventHandler( OnCommand ) );
		}

		public static bool IsRiftDemon( BaseCreature bc )
		{
			if ( bc == null || bc.Deleted || bc.Summoned || bc.Controlled || bc.ControlMaster != null || bc.IsParagon )
				return false;

			Type type = bc.GetType();
			for ( int i = 0; i < ExcludedDemons.Length; i++ )
			{
				if ( ExcludedDemons[i] == type )
					return false;
			}

			SlayerEntry demons = SlayerGroup.GetEntryByName( SlayerName.Exorcism );
			return demons != null && demons.Slays( bc );
		}

		// Called once a spawner has placed a creature.
		public static void OnCreatureSpawned( BaseCreature bc )
		{
			if ( SpawnChance <= 0.0 || m_Active.Count >= MaxActive || !IsRiftDemon( bc ) || Utility.RandomDouble() >= SpawnChance )
				return;

			// Deferred: the spawner may still move the creature after this.
			Timer.DelayCall( TimeSpan.Zero, delegate { TryOpenFor( bc ); } );
		}

		private static bool TryOpenFor( BaseCreature bc )
		{
			if ( bc == null || bc.Deleted || !bc.Alive || m_Active.Count >= MaxActive || !IsOverworld( bc.Map, bc.Location ) || HasRiftNear( bc.Map, bc.Location ) )
				return false;

			DemonRiftPortal rift = new DemonRiftPortal( bc );
			rift.MoveToWorld( bc.Location, bc.Map );

			Effects.SendLocationParticles( EffectItem.Create( bc.Location, bc.Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 0x497, 0, 5052, 0 );
			Effects.PlaySound( bc.Location, bc.Map, 0x20E );
			return true;
		}

		// Out in the open on one of the surface lands: no dungeon, cave, town or instance.
		public static bool IsOverworld( Map map, Point3D p )
		{
			if ( map == null || map == Map.Internal || InstanceManager.IsInstanceMap( map ) )
				return false;

			if ( !Object.ReferenceEquals( map, Map.Lodor ) && !Object.ReferenceEquals( map, Map.Sosaria ) && !Object.ReferenceEquals( map, Map.SerpentIsland )
				&& !Object.ReferenceEquals( map, Map.IslesDread ) && !Object.ReferenceEquals( map, Map.SavagedEmpire ) )
				return false;

			Region reg = Region.Find( p, map );
			if ( !( reg.IsDefault || reg is OutDoorRegion || reg is OutDoorBadRegion ) )
				return false;

			return !IsWater( map, p );
		}

		private static bool IsWater( Map map, Point3D p )
		{
			LandTile land = map.Tiles.GetLandTile( p.X, p.Y );
			if ( ( TileData.LandTable[land.ID & TileData.MaxLandValue].Flags & TileFlag.Wet ) == 0 )
				return false;

			StaticTile[] tiles = map.Tiles.GetStaticTiles( p.X, p.Y, true );
			for ( int i = 0; i < tiles.Length; i++ )
			{
				if ( TileData.ItemTable[tiles[i].ID & TileData.MaxItemValue].Surface )
					return false;
			}

			return true;
		}

		private static bool HasRiftNear( Map map, Point3D p )
		{
			for ( int i = 0; i < m_Active.Count; i++ )
			{
				DemonRiftPortal rift = m_Active[i];
				if ( Object.ReferenceEquals( rift.Map, map ) && Utility.InRange( rift.Location, p, MinSpacing ) )
					return true;
			}

			return false;
		}

		public static void OnCreatureDeath( BaseCreature bc )
		{
			if ( bc == null || m_Active.Count == 0 )
				return;

			for ( int i = 0; i < m_Active.Count; i++ )
			{
				DemonRiftPortal rift = m_Active[i];
				if ( !rift.m_Open && Object.ReferenceEquals( rift.m_Demon, bc ) )
				{
					rift.Open();
					return;
				}
			}
		}

		// ----- Lifecycle -----

		private void StartTimer()
		{
			if ( m_Timer == null )
				m_Timer = Timer.DelayCall( CheckInterval, CheckInterval, new TimerCallback( Check ) );
		}

		private void Check()
		{
			if ( Deleted )
				return;

			if ( !m_Open )
			{
				// The demon went back to wherever it came from without being slain.
				if ( m_Demon == null || m_Demon.Deleted )
					Delete();

				return;
			}

			if ( DateTime.Now >= m_ClosesAt )
				Collapse();
		}

		public void Open()
		{
			if ( Deleted || m_Open )
				return;

			m_Open = true;
			m_Demon = null;
			m_ClosesAt = DateTime.Now + OpenDuration;
			ApplyOpenLook();

			Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 0x21, 0, 5022, 0 );
			Effects.PlaySound( Location, Map, 0x20F );
			PublicOverheadMessage( Server.Network.MessageType.Regular, 0x21, false, "With its master slain, the rift tears open!" );
		}

		private void Collapse()
		{
			Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x3728, 8, 20, 0x497, 0, 5042, 0 );
			Effects.PlaySound( Location, Map, 0x1FE );
			Delete();
		}

		// ----- Entering -----

		public override void OnDoubleClick( Mobile from )
		{
			if ( from == null )
				return;

			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.SendLocalizedMessage( 500446 ); // That is too far away.
				return;
			}

			Enter( from );
		}

		public override bool OnMoveOver( Mobile m )
		{
			if ( m == null || !m.Player )
				return true;

			return !Enter( m );
		}

		private bool Enter( Mobile from )
		{
			if ( !from.Alive )
			{
				from.SendMessage( "The rift will not carry the dead." );
				return false;
			}

			if ( !m_Open )
			{
				from.SendMessage( "The rift is sealed. It will not open while the demon that came through it still lives." );
				return false;
			}

			if ( from.Criminal || Server.Spells.SpellHelper.CheckCombat( from ) )
			{
				from.SendMessage( "You are too agitated to step into the rift." );
				return false;
			}

			bool returning = m_Entrants.Contains( from.Serial );
			if ( returning && !DungeonLobbyInstanceType.Instance.HasLiveLobby( from ) )
			{
				from.SendMessage( "The rift has already carried you once. It will not do so again." );
				return false;
			}

			if ( !DungeonLobbyInstanceType.Instance.Enter( from, Map, Location ) )
				return false;

			if ( !returning )
				m_Entrants.Add( from.Serial );

			Effects.PlaySound( Location, Map, 0x1FE );
			return true;
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			if ( m_Open )
				list.Add( 1070722, String.Format( "Closes in {0} minutes", Math.Max( 1, (int)Math.Ceiling( TimeUntilClose.TotalMinutes ) ) ) );
			else
				list.Add( 1070722, "Sealed until its demon is slain" );
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Timer != null )
			{
				m_Timer.Stop();
				m_Timer = null;
			}

			m_Active.Remove( this );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version

			writer.Write( (Mobile) m_Demon );
			writer.Write( (bool) m_Open );
			writer.Write( (DateTime) m_ClosesAt );

			writer.Write( (int) m_Entrants.Count );
			for ( int i = 0; i < m_Entrants.Count; i++ )
				writer.Write( (int) m_Entrants[i] );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			m_Demon = reader.ReadMobile();
			m_Open = reader.ReadBool();
			m_ClosesAt = reader.ReadDateTime();

			int count = reader.ReadInt();
			for ( int i = 0; i < count; i++ )
				m_Entrants.Add( (Serial)reader.ReadInt() );

			m_Active.Add( this );
			StartTimer();
		}

		// ----- Staff -----

		private static void OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			string sub = e.Arguments.Length > 0 ? e.Arguments[0].ToLower() : "";

			switch ( sub )
			{
				case "chance":
				{
					if ( e.Arguments.Length > 1 )
					{
						double pct;
						if ( Double.TryParse( e.Arguments[1], out pct ) )
							SpawnChance = Math.Max( 0.0, Math.Min( 100.0, pct ) ) / 100.0;
					}

					from.SendMessage( "Outdoor demons open a rift {0:0.##}% of the time ({1} active, max {2}).", SpawnChance * 100.0, m_Active.Count, MaxActive );
					break;
				}
				case "spawn":
				{
					Daemon demon = new Daemon();
					demon.MoveToWorld( from.Location, from.Map );

					DemonRiftPortal rift = new DemonRiftPortal( demon );
					rift.MoveToWorld( from.Location, from.Map );
					from.SendMessage( "A daemon comes through a sealed rift. Slay it to open the way." );
					break;
				}
				case "open":
				{
					DemonRiftPortal rift = new DemonRiftPortal( null );
					rift.MoveToWorld( from.Location, from.Map );
					rift.Open();
					break;
				}
				case "clear":
				{
					List<DemonRiftPortal> all = new List<DemonRiftPortal>( m_Active );
					for ( int i = 0; i < all.Count; i++ )
						all[i].Delete();

					from.SendMessage( "Removed {0} demonic rift(s).", all.Count );
					break;
				}
				default:
				{
					from.SendMessage( "[demonrift chance [percent] | spawn | open | clear  ({0} active)", m_Active.Count );
					break;
				}
			}
		}
	}
}
