using System;
using Server;

namespace Server.Engines.Instancing
{
	// RIDES IN THE TOP OF ITS BEARER'S PACK FOR AS LONG AS THEY STAY IN THE DUNGEON RUN, AND CRUMBLES ON THE WAY OUT
	public class DungeonRunSigil : Item
	{
		private Serial m_Run;

		public Serial Run { get { return m_Run; } }

		public DungeonRunSigil( Serial run, string dungeon ) : base( 0xE2D )
		{
			m_Run = run;
			Name = String.Format( "a sigil of {0}", dungeon );
			LootType = LootType.Blessed;
			Weight = 0.0;
		}

		public DungeonRunSigil( Serial serial ) : base( serial )
		{
		}

		public override bool Nontransferable { get { return true; } }

		public override void HandleInvalidTransfer( Mobile from )
		{
			from.SendMessage( "The sigil will not leave the top of your pack." );
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			list.Add( 1070722, "Double-click to read the dungeon's progress" );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) || !DungeonInstanceType.Instance.SendProgress( from ) )
				from.SendMessage( "The sigil is dark." );
		}

		// A PACK CHANGES MAP ONE ITEM AT A TIME, SO THE CHECK WAITS UNTIL THE MOVE IS DONE
		public override void OnMapChange()
		{
			Timer.DelayCall( TimeSpan.Zero, CheckBearer );
		}

		private void CheckBearer()
		{
			if ( !Deleted && Map != Map.Internal && !DungeonInstanceType.Instance.IsRunMap( m_Run, Map ) )
				Delete();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
			writer.Write( (int) m_Run );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
			m_Run = (Serial)reader.ReadInt();
		}
	}
}
