using System;
using Server;
using Server.Items;
using Server.Misc;

namespace Server.Engines.Instancing
{
	// Dropped by a dungeon instance's boss: unlocked, untrapped, and filled the first time it is opened.
	public class DungeonBossChest : LockableContainer
	{
		private int m_Level;
		private bool m_Filled;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Level { get { return m_Level; } set { m_Level = Math.Max( 1, Math.Min( 10, value ) ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Filled { get { return m_Filled; } set { m_Filled = value; InvalidateProperties(); } }

		[Constructable]
		public DungeonBossChest() : this( 5 )
		{
		}

		[Constructable]
		public DungeonBossChest( int level ) : base( 0x281F )
		{
			Level = level;
			Name = "treasure chest";
			Movable = false;
			Weight = 5.0;
			ItemID = Utility.RandomList( 0x281F, 0x2820, 0x4FE6, 0x4FE7 );
			GumpID = 0x975;
			Resource = CraftResource.Iron;
			Unlock();
		}

		public void SetOwner( Mobile boss )
		{
			if ( boss == null )
				return;

			string owner = boss.Name;
			if ( !String.IsNullOrEmpty( boss.Title ) )
				owner = boss.Name + " " + boss.Title;

			Name = "treasure chest of " + owner;
			Hue = boss.Hue;
		}

		private void Unlock()
		{
			Locked = false;
			LockLevel = 0;
			MaxLockLevel = 0;
			RequiredSkill = 0;
			TrapType = TrapType.None;
			TrapPower = 0;
			TrapLevel = 0;
		}

		private void TryFill( Mobile from )
		{
			if ( m_Filled || from == null )
				return;

			m_Filled = true;

			int level = m_Level;
			if ( GetPlayerInfo.LuckyPlayer( from.Luck ) )
				level += Utility.RandomMinMax( 1, 2 );

			ContainerFunctions.FillTheContainer( level, this, from );
			InvalidateProperties();
		}

		public override void Open( Mobile from )
		{
			Unlock();
			TryFill( from );
			from.SendSound( 0x48, GetWorldLocation() );
			base.Open( from );
		}

		public override bool DisplaysContent { get { return m_Filled; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			if ( !m_Filled )
				list.Add( "Filled with treasure" );
		}

		public DungeonBossChest( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
			writer.Write( (int) m_Level );
			writer.Write( (bool) m_Filled );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
			m_Level = reader.ReadInt();
			m_Filled = reader.ReadBool();
		}
	}
}
