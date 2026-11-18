using System;
using Server;
using Server.Network;

namespace Server.Gumps
{
	// THE GRAVESTONE FRAME SHARED BY EVERY PROMPT SHOWN TO THE DEAD
	public abstract class SpiritGump : Gump
	{
		public const string Calm = "#b7cbda";
		public const string Dire = "#da3f3f";

		protected const int AcceptButton = 1;
		protected const int TextHeight = 141;

		private string m_Color = Calm;

		protected SpiritGump( int x, int y ) : base( x, y )
		{
		}

		protected void AddFrame( Mobile owner, string color, string title, string text )
		{
			AddFrame( owner, color, title, text, TextHeight );
		}

		protected void AddFrame( Mobile owner, string color, string title, string text, int textHeight )
		{
			m_Color = color;

			Closable = true;
			Disposable = true;
			Dragable = true;
			Resizable = false;

			AddPage( 0 );
			AddImage( 0, 0, owner.Karma < 0 ? 9587 : 9586, Server.Misc.PlayerSettings.GetGumpHue( owner ) );
			AddText( 10, 11, 349, 20, title );
			AddText( 11, 41, 385, textHeight, text );
		}

		// ANY BUTTON BUT ACCEPT, LIKE CLOSING THE GUMP, REPLIES WITH 0
		protected void AddAcceptDecline()
		{
			AddButton( 368, 10, 4017, 4017, 0, GumpButtonType.Reply, 0 );
			AddButton( 10, 225, 4023, 4023, AcceptButton, GumpButtonType.Reply, 0 );
			AddButton( 367, 225, 4020, 4020, 0, GumpButtonType.Reply, 0 );
		}

		protected void AddCost( int price )
		{
			AddText( 238, 193, 100, 20, "Resurrect:" );
			AddText( 343, 194, 54, 20, String.Format( "{0} Gold", price ) );
		}

		protected void AddFunds( string label, string amount )
		{
			AddText( 11, 195, 61, 20, label );
			AddText( 77, 195, 100, 20, amount );
		}

		protected void AddText( int x, int y, int width, int height, string text )
		{
			AddHtml( x, y, width, height, @"<BODY><BASEFONT Color=" + m_Color + ">" + text + "</BASEFONT></BODY>", false, false );
		}
	}

	public class SpiritChoice
	{
		public readonly string Label;
		public readonly Action Chosen;

		public SpiritChoice( string label, Action chosen )
		{
			Label = label;
			Chosen = chosen;
		}
	}

	// A PROMPT FOR THE DEAD BUILT FROM A LIST OF CHOICES; EACH CHOICE CHECKS FOR ITSELF THAT IT STILL APPLIES
	public class SpiritChoiceGump : SpiritGump
	{
		// ROWS SIT FAR ENOUGH APART TO BE TAPPED ON A TOUCH SCREEN
		private const int RowHeight = 40;
		private const int LastRowY = 225;

		private readonly SpiritChoice[] m_Choices;

		// A GUMP THAT CANNOT BE CLOSED STAYS UNTIL A CHOICE IS MADE
		public SpiritChoiceGump( Mobile owner, string title, string text, bool closable, params SpiritChoice[] choices ) : base( 50, 50 )
		{
			m_Choices = choices;

			int firstRowY = LastRowY - RowHeight * ( choices.Length - 1 );
			AddFrame( owner, Calm, title, text, firstRowY - 51 );
			Closable = closable;

			if ( closable )
				AddButton( 368, 10, 4017, 4017, 0, GumpButtonType.Reply, 0 );

			for ( int i = 0; i < choices.Length; i++ )
			{
				int y = firstRowY + RowHeight * i;
				AddButton( 10, y, 4005, 4007, i + 1, GumpButtonType.Reply, 0 );
				AddText( 45, y + 2, 345, 20, choices[i].Label );
			}
		}

		public override void OnResponse( NetState state, RelayInfo info )
		{
			int choice = info.ButtonID - 1;
			if ( choice >= 0 && choice < m_Choices.Length && m_Choices[choice].Chosen != null )
				m_Choices[choice].Chosen();
		}
	}
}
