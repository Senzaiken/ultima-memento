using System;
using Server;

namespace Server.Engines.Instancing
{
	// STAFF PICK THE DUNGEON AND DIFFICULTY; EVERYONE WHO USES THE GATE SHARES ITS INSTANCE
	public class DungeonInstanceGate : InstancePortal
	{
		private string m_DungeonName;
		private Difficulty m_Difficulty = Difficulty.Normal;

		[CommandProperty( AccessLevel.GameMaster )]
		public string DungeonName
		{
			get { return m_DungeonName; }
			set
			{
				DungeonInstanceType.DungeonInstanceDefinition def = DungeonInstanceType.Instance.FindDefinition( value );
				m_DungeonName = def != null ? def.DisplayName : value;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public new Difficulty Difficulty
		{
			get { return m_Difficulty; }
			set { m_Difficulty = DungeonInstanceType.ClampDifficulty( value ); InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public string InstanceStatus { get { return DungeonInstanceType.Instance.GetInstanceStatus( Serial ); } }

		[Constructable]
		public DungeonInstanceGate() : base( 0xF6C )
		{
			Name = "a dungeon instance gate";
			Hue = 0x455;
			Light = LightType.Circle300;
			DungeonName = "0";
		}

		public DungeonInstanceGate( Serial serial ) : base( serial )
		{
		}

		protected override bool Use( Mobile from )
		{
			return DungeonInstanceType.Instance.SendToConfiguredInstance( from, Serial, DungeonInstanceType.Instance.FindDefinition( m_DungeonName ), m_Difficulty, Map, GetWorldLocation(), TimeSpan.Zero );
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			DungeonInstanceType.DungeonInstanceDefinition def = DungeonInstanceType.Instance.FindDefinition( m_DungeonName );
			list.Add( 1070722, "Dungeon: " + ( def != null ? def.DisplayName : "unresolved" ) );
			list.Add( 1070722, "Difficulty: " + m_Difficulty );
			list.Add( 1070722, "Instance: " + InstanceStatus );
		}

		public override void OnAfterDelete()
		{
			DungeonInstanceType.Instance.CleanupInstance( Serial );
			base.OnAfterDelete();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 1 ); // version

			writer.Write( (string) m_DungeonName );
			writer.Write( (int) m_Difficulty );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			if ( version < 1 )
				reader.ReadInt();

			m_DungeonName = reader.ReadString();
			m_Difficulty = DungeonInstanceType.ClampDifficulty( (Difficulty)reader.ReadInt() );
		}
	}
}
