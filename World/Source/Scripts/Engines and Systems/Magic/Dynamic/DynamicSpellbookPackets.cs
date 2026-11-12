using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Spells.Dynamic
{
	// 0xBF/0x39 - the client's cached copy is current; only the owned-spell bitmask is refreshed.
	public sealed class SpellbookCacheValid : Packet
	{
		public SpellbookCacheValid( Serial spellbookSerial, byte clientBookType, uint version, ulong spellBitmask ) : base( 0xBF )
		{
			EnsureCapacity( 22 );

			m_Stream.Write( (short)0x39 );
			m_Stream.Write( (int)spellbookSerial );
			m_Stream.Write( (byte)clientBookType );
			m_Stream.Write( (int)version );
			PacketWriterHelper.WriteUInt64( m_Stream, spellBitmask );
		}
	}

	// 0xBF/0x3A - full book layout and spell list.
	public sealed class SpellbookFullData : Packet
	{
		private const uint CacheTtlSeconds = 14400;

		public SpellbookFullData( Serial spellbookSerial, DynamicSpellbook book, List<DynamicSpellDefinition> spells, uint version, ulong spellBitmask ) : base( 0xBF )
		{
			string[] pageNames = book.PageNames ?? new string[0];
			ushort[] overlays = book.OverlayGraphics ?? new ushort[0];
			List<DynamicInfoPage> infoPages = book.InfoPages ?? new List<DynamicInfoPage>();
			DynamicSpellbookBookmark bookmark = book.Bookmark;

			EnsureCapacity( 256 );

			m_Stream.Write( (short)0x3A );
			m_Stream.Write( (int)spellbookSerial );
			m_Stream.Write( (byte)book.ClientBookType );
			m_Stream.Write( (int)version );
			PacketWriterHelper.WriteUInt64( m_Stream, spellBitmask );
			m_Stream.Write( (short)book.BookGraphic );
			m_Stream.Write( (short)book.MinimizedGraphic );
			m_Stream.Write( (byte)spells.Count );
			m_Stream.Write( (int)CacheTtlSeconds );
			m_Stream.Write( (byte)book.SpellsPerPageSide );
			m_Stream.Write( (byte)book.MaxDictionaryPages );
			m_Stream.Write( (byte)pageNames.Length );

			byte displayFlags = 0;
			if ( book.DisplayManaCost ) displayFlags |= 0x01;
			if ( book.DisplayMinSkill ) displayFlags |= 0x02;
			if ( book.DisplayPowerWords ) displayFlags |= 0x04;
			m_Stream.Write( (byte)displayFlags );

			m_Stream.Write( (short)book.BookHue );
			m_Stream.Write( (short)book.TextColor );
			m_Stream.Write( (short)book.SpellNameColor );
			m_Stream.Write( (short)book.ContentOffsetX );
			m_Stream.Write( (short)book.ContentOffsetY );
			m_Stream.Write( (short)book.PageTurnLeftGraphic );
			m_Stream.Write( (short)book.PageTurnRightGraphic );
			m_Stream.Write( (short)book.PageTurnLeftX );
			m_Stream.Write( (short)book.PageTurnLeftY );
			m_Stream.Write( (short)book.PageTurnRightX );
			m_Stream.Write( (short)book.PageTurnRightY );

			m_Stream.Write( (byte)overlays.Length );
			foreach ( ushort overlay in overlays )
				m_Stream.Write( (short)overlay );

			WriteAscii8( book.CustomPropertyTitle );
			WriteAscii8( book.CustomPropertyLabel );
			WriteAscii8( book.CustomPropertyName );

			foreach ( string pageName in pageNames )
				WriteAscii8( pageName );

			foreach ( DynamicSpellDefinition spell in spells )
			{
				m_Stream.Write( (short)spell.SpellID );
				m_Stream.Write( (short)spell.IconGraphic );
				m_Stream.Write( (int)spell.NameCliloc );
				WriteAscii8( spell.Name );
				WriteAscii8( spell.PowerWords );
				WriteAscii8( spell.Description );
				m_Stream.Write( (byte)spell.ManaCost );
				m_Stream.Write( (byte)spell.MinSkill );
				m_Stream.Write( (byte)spell.TargetType );
				m_Stream.Write( (short)spell.Reagents );

				string[] reagents = spell.CustomReagents ?? new string[0];
				m_Stream.Write( (byte)Math.Min( reagents.Length, 255 ) );
				for ( int i = 0; i < reagents.Length && i < 255; i++ )
					WriteAscii8( reagents[i] );

				m_Stream.Write( (short)spell.Cooldown );
				m_Stream.Write( (byte)spell.Page );
			}

			WriteAscii8( book.ManaCostLabel );
			WriteAscii8( book.MinSkillLabel );
			m_Stream.Write( (short)book.TitleColor );

			m_Stream.Write( (byte)Math.Min( infoPages.Count, 255 ) );
			for ( int i = 0; i < infoPages.Count && i < 255; i++ )
			{
				WriteAscii16( infoPages[i].Title );
				WriteAscii16( infoPages[i].Body );
			}

			m_Stream.Write( (byte)( bookmark != null ? 0x01 : 0x00 ) );
			if ( bookmark != null )
			{
				m_Stream.Write( (short)bookmark.Graphic );
				m_Stream.Write( (short)bookmark.PressedGraphic );
				m_Stream.Write( (short)bookmark.X );
				m_Stream.Write( (short)bookmark.Y );
				m_Stream.Write( (short)bookmark.Hue );
				m_Stream.Write( (byte)bookmark.Page );
				m_Stream.Write( (byte)bookmark.ActionType );
				m_Stream.Write( (int)bookmark.Action );
				WriteAscii8( bookmark.Tooltip );
			}
		}

		// Length-prefixed (1 byte) ASCII, truncated to 255 characters.
		private void WriteAscii8( string value )
		{
			int length = value == null ? 0 : Math.Min( value.Length, 255 );
			m_Stream.Write( (byte)length );

			for ( int i = 0; i < length; i++ )
				m_Stream.Write( (byte)value[i] );
		}

		// Length-prefixed (2 bytes) ASCII, for longer text such as info pages.
		private void WriteAscii16( string value )
		{
			int length = value == null ? 0 : Math.Min( value.Length, 65535 );
			m_Stream.Write( (short)length );

			for ( int i = 0; i < length; i++ )
				m_Stream.Write( (byte)value[i] );
		}
	}

	// 0xBF/0x3B - drop the cached copy of a book type (0xFF = all) and optionally reopen it.
	public sealed class InvalidateSpellbookCache : Packet
	{
		public InvalidateSpellbookCache( byte clientBookType, uint newVersion, bool forceReload ) : base( 0xBF )
		{
			EnsureCapacity( 11 );

			m_Stream.Write( (short)0x3B );
			m_Stream.Write( (byte)clientBookType );
			m_Stream.Write( (int)newVersion );
			m_Stream.Write( (byte)( forceReload ? 0x01 : 0x00 ) );
		}
	}

	// 0xBF/0x3D - tells the client that this item is a server-defined book of the given type.
	// Must arrive before the book is displayed, because stock books share the same item graphics.
	public sealed class RegisterSpellbookSerial : Packet
	{
		public RegisterSpellbookSerial( Serial serial, byte clientBookType ) : base( 0xBF )
		{
			EnsureCapacity( 10 );

			m_Stream.Write( (short)0x3D );
			m_Stream.Write( (int)serial );
			m_Stream.Write( (byte)clientBookType );
		}
	}

	// 0xBF/0x3F - asks the client whether it supports server-defined spellbooks; capable clients answer with 0xBF/0x3F.
	public sealed class DynamicSpellbookQuery : Packet
	{
		public DynamicSpellbookQuery( byte protocolVersion ) : base( 0xBF )
		{
			EnsureCapacity( 6 );

			m_Stream.Write( (short)0x3F );
			m_Stream.Write( (byte)protocolVersion );
		}
	}

	internal static class PacketWriterHelper
	{
		public static void WriteUInt64( PacketWriter stream, ulong value )
		{
			for ( int i = 7; i >= 0; --i )
				stream.Write( (byte)( value >> ( i * 8 ) ) );
		}
	}
}
