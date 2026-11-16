using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Engines.Instancing
{
	// LEFT WHERE AN OUTDOOR DEMON SPAWNS; SEALED UNTIL THAT DEMON IS SLAIN
	public class DemonRiftPortal : InstancePortal
	{
		public static double SpawnChance = 1.0;
		public static int MaxActive = 20;
		public static int MinSpacing = 40;
		public static TimeSpan OpenDuration = TimeSpan.FromMinutes( 30 );

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
		private readonly Dictionary<Serial, Serial> m_Entrants = new Dictionary<Serial, Serial>();
		private Timer m_Timer;

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Demon { get { return m_Demon; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsOpen { get { return m_Open; } set { if ( value ) Open(); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan TimeUntilClose { get { return m_Open && m_ClosesAt > DateTime.Now ? m_ClosesAt - DateTime.Now : TimeSpan.Zero; } }

		public DemonRiftPortal( Mobile demon ) : base( 0xDDA )
		{
			Name = "a sealed demonic rift";
			Hue = 0x497;
			Light = LightType.Circle150;
			m_Demon = demon;

			m_Active.Add( this );
			StartTimer();
		}

		public DemonRiftPortal( Serial serial ) : base( serial )
		{
		}

		public static void Configure()
		{
			CommandSystem.Register( "demonrift", AccessLevel.GameMaster, new CommandEventHandler( OnCommand ) );
		}

		public static bool IsRiftDemon( BaseCreature bc )
		{
			if ( bc.Summoned || bc.Controlled || bc.ControlMaster != null || bc.IsParagon || Array.IndexOf( ExcludedDemons, bc.GetType() ) >= 0 )
				return false;

			SlayerEntry demons = SlayerGroup.GetEntryByName( SlayerName.Exorcism );
			return demons != null && demons.Slays( bc );
		}

		public static void OnCreatureSpawned( BaseCreature bc )
		{
			if ( m_Active.Count >= MaxActive || Utility.RandomDouble() >= SpawnChance || !IsRiftDemon( bc ) )
				return;

			// THE SPAWNER MAY STILL MOVE THE CREATURE
			Timer.DelayCall( TimeSpan.Zero, delegate { TryOpenFor( bc ); } );
		}

		private static void TryOpenFor( BaseCreature bc )
		{
			if ( bc.Deleted || !bc.Alive || m_Active.Count >= MaxActive || !IsOverworld( bc.Map, bc.Location ) || HasRiftNear( bc.Map, bc.Location ) )
				return;

			new DemonRiftPortal( bc ).MoveToWorld( bc.Location, bc.Map );

			Effects.SendLocationParticles( EffectItem.Create( bc.Location, bc.Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 0x497, 0, 5052, 0 );
			Effects.PlaySound( bc.Location, bc.Map, 0x20E );
		}

		// OUT IN THE OPEN ON A SURFACE LAND: NO DUNGEON, CAVE, TOWN, WATER OR INSTANCE
		public static bool IsOverworld( Map map, Point3D p )
		{
			if ( !Object.ReferenceEquals( map, Map.Lodor ) && !Object.ReferenceEquals( map, Map.Sosaria ) && !Object.ReferenceEquals( map, Map.SerpentIsland )
				&& !Object.ReferenceEquals( map, Map.IslesDread ) && !Object.ReferenceEquals( map, Map.SavagedEmpire ) )
				return false;

			Region reg = Region.Find( p, map );
			return ( reg.IsDefault || reg is OutDoorRegion || reg is OutDoorBadRegion ) && !IsWater( map, p );
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
			foreach ( DemonRiftPortal rift in m_Active )
			{
				if ( Object.ReferenceEquals( rift.Map, map ) && Utility.InRange( rift.Location, p, MinSpacing ) )
					return true;
			}

			return false;
		}

		public static void OnCreatureDeath( BaseCreature bc )
		{
			foreach ( DemonRiftPortal rift in m_Active )
			{
				if ( Object.ReferenceEquals( rift.m_Demon, bc ) )
				{
					rift.Open();
					return;
				}
			}
		}

		private void StartTimer()
		{
			m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromSeconds( 5.0 ), new TimerCallback( Check ) );
		}

		private void Check()
		{
			if ( !m_Open && ( m_Demon == null || m_Demon.Deleted ) )
			{
				Delete();
			}
			else if ( m_Open && DateTime.Now >= m_ClosesAt )
			{
				Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x3728, 8, 20, 0x497, 0, 5042, 0 );
				Effects.PlaySound( Location, Map, 0x1FE );
				Delete();
			}
		}

		public void Open()
		{
			if ( Deleted || m_Open )
				return;

			m_Open = true;
			m_Demon = null;
			m_ClosesAt = DateTime.Now + OpenDuration;
			Name = "a demonic rift";
			Hue = 0;

			Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 0x21, 0, 5022, 0 );
			Effects.PlaySound( Location, Map, 0x20F );
			PublicOverheadMessage( MessageType.Regular, 0x21, false, "With its master slain, the rift tears open!" );
		}

		// EACH PLAYER GETS ONE TRIP, THOUGH THEY MAY RETURN TO THE HALL THEY JOINED WHILE IT STANDS
		protected override bool Use( Mobile from )
		{
			Serial hall = DungeonLobbyInstanceType.Instance.OwnerKey( from );
			Serial joined;

			if ( !from.Alive )
				from.SendMessage( "The rift will not carry the dead." );
			else if ( !m_Open )
				from.SendMessage( "The rift is sealed. It will not open while the demon that came through it still lives." );
			else if ( from.Criminal || Server.Spells.SpellHelper.CheckCombat( from ) )
				from.SendMessage( "You are too agitated to step into the rift." );
			else if ( m_Entrants.TryGetValue( from.Serial, out joined ) && ( joined != hall || !DungeonLobbyInstanceType.Instance.IsLive( hall ) ) )
				from.SendMessage( "The rift has already carried you once. It will not do so again." );
			else if ( DungeonLobbyInstanceType.Instance.Enter( from, Map, Location ) )
			{
				m_Entrants[from.Serial] = hall;
				Effects.PlaySound( Location, Map, 0x1FE );
				return true;
			}

			return false;
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
				m_Timer.Stop();

			m_Active.Remove( this );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 ); // version

			writer.Write( (Mobile) m_Demon );
			writer.Write( (bool) m_Open );
			writer.Write( (DateTime) m_ClosesAt );

			writer.Write( (int) m_Entrants.Count );
			foreach ( KeyValuePair<Serial, Serial> kvp in m_Entrants )
			{
				writer.Write( (int) kvp.Key );
				writer.Write( (int) kvp.Value );
			}
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
				m_Entrants[(Serial)reader.ReadInt()] = version >= 1 ? (Serial)reader.ReadInt() : Serial.MinusOne;

			m_Active.Add( this );
			StartTimer();
		}

		private static void OnCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			string sub = e.Arguments.Length > 0 ? e.Arguments[0].ToLower() : "";
			double pct;

			switch ( sub )
			{
				case "chance":
					if ( e.Arguments.Length > 1 && Double.TryParse( e.Arguments[1], out pct ) )
						SpawnChance = Math.Max( 0.0, Math.Min( 100.0, pct ) ) / 100.0;

					from.SendMessage( "Outdoor demons open a rift {0:0.##}% of the time ({1} active, max {2}).", SpawnChance * 100.0, m_Active.Count, MaxActive );
					break;
				case "spawn":
					Daemon demon = new Daemon();
					demon.MoveToWorld( from.Location, from.Map );
					new DemonRiftPortal( demon ).MoveToWorld( from.Location, from.Map );
					from.SendMessage( "A daemon comes through a sealed rift. Slay it to open the way." );
					break;
				case "open":
					DemonRiftPortal rift = new DemonRiftPortal( null );
					rift.MoveToWorld( from.Location, from.Map );
					rift.Open();
					break;
				case "clear":
					List<DemonRiftPortal> all = new List<DemonRiftPortal>( m_Active );
					foreach ( DemonRiftPortal r in all )
						r.Delete();

					from.SendMessage( "Removed {0} demonic rift(s).", all.Count );
					break;
				default:
					from.SendMessage( "[demonrift chance [percent] | spawn | open | clear  ({0} active)", m_Active.Count );
					break;
			}
		}
	}
}
