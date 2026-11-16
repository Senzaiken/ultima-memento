using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Instancing
{
	public class DungeonMasterGump : Gump
	{
		private enum Buttons
		{
			Open = 1,
			DifficultyBase = 100,
			TimeLimitBase = 200
		}

		private const int SliderWidth = 550;

		private readonly DungeonMaster m_Master;
		private readonly Difficulty m_Difficulty;
		private readonly int m_TimeIndex;

		public DungeonMasterGump( Mobile from, DungeonMaster master, Difficulty difficulty, int timeIndex ) : base( 50, 50 )
		{
			m_Master = master;
			m_Difficulty = DungeonInstanceType.ClampDifficulty( difficulty );
			m_TimeIndex = Math.Max( 0, Math.Min( DungeonMaster.TimeLimits.Length - 1, timeIndex ) );

			AddImage( 0, 0, 9582, Server.Misc.PlayerSettings.GetGumpHue( from ) );

			int x = 10;
			int y = 10;
			TextDefinition.AddHtmlText( this, x, y, 550, 20, String.Format( "{0} {1}", master.Name, master.Title ), HtmlColors.COOL_BLUE );

			x += 10;
			y += 30;
			TextDefinition.AddHtmlText( this, x, y, 540, 60,
				"I can open the way to a dungeon of my choosing. Tell me how dangerous its creatures should be, and how long you will have to defeat its master. " +
				"If you would rather not race the clock, choose Untimed.", HtmlColors.COOL_BLUE );
			y += 70;

			AddSlider( x, y, "Monster Difficulty", "Increase the health and damage of every creature", Buttons.DifficultyBase, (int)Difficulty.Deadly + 1, (int)m_Difficulty );
			y += 75;

			AddSlider( x, y, "Time Limit", "How long you have to defeat its master before the dungeon crumbles", Buttons.TimeLimitBase, DungeonMaster.TimeLimits.Length, m_TimeIndex );
			y += 75;

			TextDefinition.AddHtmlText( this, x, y, 540, 20, String.Format( "Difficulty: {0}     Time limit: {1}",
				m_Difficulty, DungeonMaster.GetTimeLimitLabel( DungeonMaster.TimeLimits[m_TimeIndex] ) ), HtmlColors.COOL_BLUE );

			x = 471;
			y = 350;
			AddButton( x, y, 4005, 4007, (int)Buttons.Open, GumpButtonType.Reply, 0 );
			TextDefinition.AddHtmlText( this, x + 33, y + 3, 100, 20, "OPEN THE WAY", HtmlColors.COOL_BLUE );
		}

		private void AddSlider( int x, int y, string label, string tooltip, Buttons baseValue, int ticks, int selected )
		{
			TextDefinition.AddHtmlText( this, x, y, 300, 20, label, HtmlColors.COOL_BLUE );
			AddTooltip( tooltip );
			x += 10;
			y += 20;

			int space = (int)( (double)( SliderWidth - 14 ) / ( ticks - 1 ) );
			AddImageTiled( x, y + 18, SliderWidth, 3, 2700 );

			const int UpArrow = 5600;
			const int DownArrow = 5602;

			for ( int i = 0; i < ticks; i++ )
			{
				if ( i == selected )
				{
					AddImage( x + ( i * space ), y, DownArrow );
					AddImage( x + ( i * space ), y + 24, UpArrow );
				}
				else
				{
					AddButton( x + ( i * space ), y + 24, UpArrow, UpArrow, (int)baseValue + i, GumpButtonType.Reply, 0 );
				}

				if ( baseValue == Buttons.DifficultyBase )
					AddTooltip( String.Format( "Difficulty: {0}", (Difficulty)i ) );
				else
					AddTooltip( String.Format( "Time limit: {0}", DungeonMaster.GetTimeLimitLabel( DungeonMaster.TimeLimits[i] ) ) );
			}
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			Mobile from = state.Mobile;
			if ( from == null || info.ButtonID == 0 || m_Master == null || m_Master.Deleted )
				return;

			int id = info.ButtonID;

			if ( id == (int)Buttons.Open )
			{
				if ( !m_Master.CanOpenFor( from ) )
				{
					from.SendMessage( "The Dungeon Master cannot open the way for you now." );
					return;
				}

				m_Master.OpenPortal( from, m_Difficulty, DungeonMaster.TimeLimits[m_TimeIndex] );
			}
			else if ( id >= (int)Buttons.TimeLimitBase )
			{
				from.SendGump( new DungeonMasterGump( from, m_Master, m_Difficulty, id - (int)Buttons.TimeLimitBase ) );
			}
			else if ( id >= (int)Buttons.DifficultyBase )
			{
				from.SendGump( new DungeonMasterGump( from, m_Master, (Difficulty)( id - (int)Buttons.DifficultyBase ), m_TimeIndex ) );
			}
		}
	}
}
