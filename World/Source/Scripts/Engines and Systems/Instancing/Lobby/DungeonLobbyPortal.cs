using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Instancing
{
	// The portal a Dungeon Master opens on his dais. Its serial keys the dungeon instance, so everyone
	// who steps through shares one run. When that run is over the portal collapses for good.
	public class DungeonLobbyPortal : Item
	{
		private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds( 5.0 );

		private DungeonMaster m_Master;
		private string m_DungeonName;
		private Difficulty m_Difficulty;
		private TimeSpan m_TimeLimit;
		private bool m_Used;
		private Timer m_Timer;

		[CommandProperty( AccessLevel.GameMaster )]
		public string DungeonName { get { return m_DungeonName; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public new Difficulty Difficulty { get { return m_Difficulty; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan TimeLimit { get { return m_TimeLimit; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public string InstanceStatus { get { return DungeonInstanceType.Instance.GetInstanceStatus( Serial ); } }

		public DungeonLobbyPortal( DungeonMaster master, string dungeonName, Difficulty difficulty, TimeSpan timeLimit ) : base( 0xF6C )
		{
			m_Master = master;
			m_DungeonName = dungeonName;
			m_Difficulty = difficulty;
			m_TimeLimit = timeLimit;

			Name = "a portal to " + dungeonName;
			Movable = false;
			Hue = 0x21;
			Light = LightType.Circle300;

			StartTimer();
		}

		public DungeonLobbyPortal( Serial serial ) : base( serial )
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
				from.SendMessage( "The portal will not carry the dead." );
				return false;
			}

			DungeonInstanceType.DungeonInstanceDefinition def = DungeonInstanceType.Instance.FindDefinition( m_DungeonName );
			if ( def == null )
			{
				from.SendMessage( "The portal flickers; whatever lay beyond it is gone." );
				return false;
			}

			Map returnMap;
			Point3D returnPoint;
			DungeonLobbyInstanceType.Instance.GetReturn( from, out returnMap, out returnPoint );

			if ( !DungeonInstanceType.Instance.SendToConfiguredInstance( from, Serial, def, m_Difficulty, returnMap, returnPoint, InstanceOwnerKind.PublicGateway, m_TimeLimit ) )
				return false;

			m_Used = true;
			return true;
		}

		private void StartTimer()
		{
			if ( m_Timer == null )
				m_Timer = Timer.DelayCall( CheckInterval, CheckInterval, new TimerCallback( Check ) );
		}

		// Once the dungeon behind it has been dissolved (cleared and left, or out of time), the portal goes too.
		private void Check()
		{
			if ( Deleted || !m_Used || DungeonInstanceType.Instance.HasInstance( Serial ) )
				return;

			Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x3728, 8, 20, 0x21, 0, 5042, 0 );
			Effects.PlaySound( Location, Map, 0x1FE );

			if ( m_Master != null && !m_Master.Deleted )
				m_Master.OnPortalSpent( this );

			Delete();
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			list.Add( 1070722, String.Format( "Difficulty: {0}", m_Difficulty ) );
			list.Add( 1070722, String.Format( "Time limit: {0}", DungeonMaster.GetTimeLimitLabel( m_TimeLimit ) ) );
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Timer != null )
			{
				m_Timer.Stop();
				m_Timer = null;
			}

			DungeonInstanceType.Instance.CleanupInstance( Serial );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version

			writer.Write( (Mobile) m_Master );
			writer.Write( (string) m_DungeonName );
			writer.Write( (int) m_Difficulty );
			writer.Write( (TimeSpan) m_TimeLimit );
			writer.Write( (bool) m_Used );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			m_Master = reader.ReadMobile() as DungeonMaster;
			m_DungeonName = reader.ReadString();
			m_Difficulty = (Difficulty)reader.ReadInt();
			m_TimeLimit = reader.ReadTimeSpan();
			m_Used = reader.ReadBool();

			StartTimer();
		}
	}
}
