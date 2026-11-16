using System;
using Server;
using Server.Items;

namespace Server.Engines.Instancing
{
	// ITS SERIAL KEYS THE DUNGEON INSTANCE, SO EVERYONE WHO STEPS THROUGH SHARES ONE RUN
	public class DungeonLobbyPortal : InstancePortal
	{
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

		public DungeonLobbyPortal( DungeonMaster master, DungeonInstanceType.DungeonInstanceDefinition def, Difficulty difficulty, TimeSpan timeLimit ) : base( 0xF6C )
		{
			m_Master = master;
			m_DungeonName = def.DisplayName;
			m_Difficulty = difficulty;
			m_TimeLimit = timeLimit;

			Name = "a portal to " + def.Name;
			Hue = 0x21;
			Light = LightType.Circle300;

			StartTimer();
		}

		public DungeonLobbyPortal( Serial serial ) : base( serial )
		{
		}

		protected override bool Use( Mobile from )
		{
			if ( !from.Alive )
			{
				from.SendMessage( "The portal will not carry the dead." );
				return false;
			}

			DungeonInstanceType.DungeonInstanceDefinition def = DungeonInstanceType.Instance.FindDefinition( m_DungeonName );
			if ( def == null || m_Master == null || m_Master.Deleted )
			{
				from.SendMessage( "The portal flickers; whatever lay beyond it is gone." );
				return false;
			}

			if ( !DungeonInstanceType.Instance.SendToConfiguredInstance( from, Serial, def, m_Difficulty, m_Master.ReturnMap, m_Master.ReturnPoint, m_TimeLimit ) )
				return false;

			m_Used = true;
			return true;
		}

		private void StartTimer()
		{
			m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromSeconds( 5.0 ), new TimerCallback( Check ) );
		}

		// ONCE THE DUNGEON BEHIND IT IS GONE, THE PORTAL GOES TOO
		private void Check()
		{
			if ( !m_Used || DungeonInstanceType.Instance.HasInstance( Serial ) )
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
				m_Timer.Stop();

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
