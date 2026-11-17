using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Instancing
{
	// OFFERED TO THE FALLEN WHILE COMPANIONS STILL STAND BELOW: RETURN TO THE HALL, OR WAIT TO BE RAISED
	public class DungeonRecallGump : Gump
	{
		private const int Width = 420;
		private const int FontHue = LabelColors.OFFWHITE;

		private readonly Map m_Map;

		public DungeonRecallGump( DungeonMaster master, Map map ) : base( 60, 60 )
		{
			m_Map = map;

			AddBackground( 0, 0, Width, 170, 3600 );
			AddLabel( 20, 20, FontHue, String.Format( "{0} calls to your spirit", master.Name ) );
			TextDefinition.AddHtmlText( this, 20, 48, Width - 40, 60, "Your companions still stand below. Shall I draw you back to my hall, body and all, to rise at my ankh? Or will you linger here and trust them to raise you?", HtmlColors.PALER_RED );

			AddButton( 20, 120, 4005, 4007, 1, GumpButtonType.Reply, 0 );
			AddLabel( 55, 122, FontHue, "Return to the hall" );
			AddButton( 220, 120, 4017, 4019, 2, GumpButtonType.Reply, 0 );
			AddLabel( 255, 122, FontHue, "Wait for my companions" );
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile m = state.Mobile;
			if ( m == null || m.Alive || !Object.ReferenceEquals( m.Map, m_Map ) )
				return;

			if ( info.ButtonID == 1 )
				DungeonInstanceType.PullBack( m, m_Map );
			else
				m.SendMessage( 0x22, "Your spirit lingers below. Your companions may yet raise you." );
		}
	}
}
