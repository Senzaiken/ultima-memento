using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Instancing
{
	public class DungeonProgress
	{
		public string Dungeon;
		public Difficulty Difficulty;
		public int Percent;
		public bool Risen;
		public string Master;
		public bool MasterAwake;
		public string Clock;
		public string Omen;
	}

	// THE CHAMPION SPAWN INFO GUMP'S LAYOUT, FOR A DUNGEON RUN
	public class DungeonSigilGump : Gump
	{
		private const int Border = 20;
		private const int RowHeight = 25;
		private const int LineHeight = 18;
		private const int Width = 460;
		private const int LabelX = 20;
		private const int ValueX = 130;
		private const int ValueChars = 44;
		private const int OmenHeight = 46;
		private const int FontHue = LabelColors.OFFWHITE;

		public DungeonSigilGump( DungeonProgress p ) : base( 40, 40 )
		{
			List<string> master = Wrap( p.Master, ValueChars );
			int masterHeight = RowHeight + ( master.Count - 1 ) * LineHeight;

			int top = Border;
			int rows = p.Risen ? 4 : 5;
			AddBackground( 0, 0, Width, Border * 2 + RowHeight * rows + masterHeight + RowHeight / 2 + OmenHeight, 3600 );

			AddLabel( Border, top, FontHue, String.Format( "Sigil of {0}", p.Dungeon ) );
			top += RowHeight;

			top = AddRow( top, "Difficulty", p.Difficulty.ToString() );
			if ( !p.Risen )
				top = AddRow( top, "Progress", String.Format( "{0}%", p.Percent ) );

			AddLabel( LabelX, top, FontHue, "Master" );
			for ( int i = 0; i < master.Count; i++ )
				AddLabel( ValueX, top + i * LineHeight, p.MasterAwake ? LabelColors.RED : FontHue, master[i] );
			top += masterHeight;

			top = AddRow( top, "Time", p.Clock );

			AddImageTiled( Border + 4, top, Width - 47, 2, 9101 );
			top += RowHeight / 2;

			TextDefinition.AddHtmlText( this, Border, top, Width - Border * 2, OmenHeight, p.Omen, HtmlColors.PALER_RED );
			top += OmenHeight;

			AddButton( ValueX, top, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0 );
			AddLabel( ValueX + 30, top + 4, FontHue, "Refresh" );
		}

		private int AddRow( int top, string label, string value )
		{
			AddLabel( LabelX, top, FontHue, label );
			AddLabelCropped( ValueX, top, Width - ValueX - Border, RowHeight, FontHue, value );
			return top + RowHeight;
		}

		// SPLITS ON SPACES SO A LONG NAME AND TITLE RUN ONTO MORE LINES INSTEAD OF PAST THE EDGE
		private static List<string> Wrap( string text, int width )
		{
			List<string> lines = new List<string>();
			string line = "";

			foreach ( string word in text.Split( ' ' ) )
			{
				if ( line.Length > 0 && line.Length + 1 + word.Length > width )
				{
					lines.Add( line );
					line = word;
				}
				else
					line = line.Length > 0 ? line + " " + word : word;
			}

			lines.Add( line );
			return lines;
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			if ( info.ButtonID == 1 )
				DungeonInstanceType.Instance.SendProgress( state.Mobile );
		}
	}
}
