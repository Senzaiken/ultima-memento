using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Server.Items;
using Server.Network;

namespace Server.Spells.Dynamic
{
	/// <summary>
	/// Shows existing spellbooks as native spellbooks on clients that announce support for
	/// server-driven spellbooks (TazUO), and keeps the server gumps for everyone else.
	///
	/// Protocol (all 0xBF sub-commands):
	///   server -> 0x3F query (protocol version)         sent at login
	///   client -> 0x3F hello (protocol version)         marks the connection as capable
	///   server -> 0x3D register serial                  sent before the book is displayed
	///   client -> 0x38 request (serial, type, version)  server answers 0x39 cache valid or 0x3A full data
	///   server -> 0x3B invalidate cache
	///   client -> 0x3E bookmark pressed
	/// </summary>
	public static class DynamicSpellbookManager
	{
		public const byte ProtocolVersion = 1;

		private class CapableClient
		{
			public readonly byte Version;

			public CapableClient( byte version )
			{
				Version = version;
			}
		}

		private class Registration
		{
			public DynamicSpellbook Book;

			// Spells in the order the client shows them (grouped by page). Client spell id = ClientSpellBase + position.
			public List<SpellDefinition> DisplayOrder;

			// For each display position, the spell's index in Book.SpellDefinitions (= its bit in Spellbook.Content).
			public int[] BookIndex;

			public List<DynamicSpellDefinition> Definitions;
			public uint Version;
		}

		private static readonly ConditionalWeakTable<NetState, CapableClient> m_CapableClients = new ConditionalWeakTable<NetState, CapableClient>();
		private static readonly Dictionary<SpellbookType, Registration> m_ByServerType = new Dictionary<SpellbookType, Registration>();
		private static readonly Dictionary<byte, Registration> m_ByClientType = new Dictionary<byte, Registration>();

		public static void Initialize()
		{
			PacketHandlers.RegisterExtended( 0x38, true, new OnPacketReceive( OnSpellbookRequest ) );
			PacketHandlers.RegisterExtended( 0x3E, true, new OnPacketReceive( OnBookmarkPressed ) );
			PacketHandlers.RegisterExtended( 0x3F, false, new OnPacketReceive( OnHello ) );

			EventSink.Login += new LoginEventHandler( OnLogin );
		}

		// Ask the client whether it can show server-defined spellbooks. Clients that don't
		// understand the query ignore it and keep getting the server gumps.
		private static void OnLogin( LoginEventArgs e )
		{
			if ( e.Mobile != null && e.Mobile.NetState != null && m_ByServerType.Count > 0 )
				e.Mobile.NetState.Send( new DynamicSpellbookQuery( ProtocolVersion ) );
		}

		public static void Register( DynamicSpellbook book )
		{
			if ( book.ClientBookType < 20 )
				throw new InvalidOperationException( string.Format( "{0}: client book type {1} is reserved for stock client books.", book.GetType().Name, book.ClientBookType ) );

			if ( book.ClientSpellBase < 1999 )
				throw new InvalidOperationException( string.Format( "{0}: client spell ids must start at 1999 or higher.", book.GetType().Name ) );

			if ( book.SpellDefinitions.Count > 64 )
				throw new InvalidOperationException( string.Format( "{0}: a book can hold at most 64 spells.", book.GetType().Name ) );

			foreach ( Registration existing in m_ByServerType.Values )
			{
				if ( existing.Book.ClientBookType == book.ClientBookType && existing.Book.SpellbookType != book.SpellbookType )
					throw new InvalidOperationException( string.Format( "{0}: client book type {1} is already used by {2}.", book.GetType().Name, book.ClientBookType, existing.Book.GetType().Name ) );

				int existingEnd = existing.Book.ClientSpellBase + existing.Definitions.Count;
				int end = book.ClientSpellBase + book.SpellDefinitions.Count;

				if ( existing.Book.SpellbookType != book.SpellbookType && book.ClientSpellBase < existingEnd && existing.Book.ClientSpellBase < end )
					throw new InvalidOperationException( string.Format( "{0}: client spell ids overlap {1}.", book.GetType().Name, existing.Book.GetType().Name ) );
			}

			Registration registration = new Registration();
			registration.Book = book;
			Rebuild( registration );

			m_ByServerType[book.SpellbookType] = registration;
			m_ByClientType[book.ClientBookType] = registration;
		}

		public static bool IsCapable( NetState ns )
		{
			CapableClient client;
			return ns != null && m_CapableClients.TryGetValue( ns, out client );
		}

		public static DynamicSpellbook GetBook( SpellbookType type )
		{
			Registration registration;
			return m_ByServerType.TryGetValue( type, out registration ) ? registration.Book : null;
		}

		/// <summary>
		/// Opens the book as a native spellbook when the client supports it.
		/// Returns false when the caller should show its server gump instead.
		/// </summary>
		public static bool TryDisplay( Mobile from, Spellbook book )
		{
			if ( from == null || book == null || !IsCapable( from.NetState ) )
				return false;

			Registration registration;
			if ( !m_ByServerType.TryGetValue( book.SpellbookType, out registration ) )
				return false;

			if ( !registration.Book.CanDisplay( from, book ) )
				return false;

			from.Send( new RegisterSpellbookSerial( book.Serial, registration.Book.ClientBookType ) );
			book.DisplayTo( from );

			return true;
		}

		/// <summary>
		/// Casts a spell requested with a client-side id (native book, spell icon or macro) through its book's
		/// own cast rules. Returns false when the id isn't a dynamic spellbook id.
		/// </summary>
		public static bool TryCast( Mobile from, int clientSpellId )
		{
			if ( clientSpellId < 1999 )
				return false;

			foreach ( Registration registration in m_ByServerType.Values )
			{
				int position = clientSpellId - registration.Book.ClientSpellBase;

				if ( position >= 0 && position < registration.DisplayOrder.Count )
				{
					registration.Book.Cast( from, registration.DisplayOrder[position].SpellID );
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Recomputes a book's data after its definitions or layout changed and tells clients to drop their cache.
		/// </summary>
		public static void Invalidate( SpellbookType type, bool forceReload )
		{
			Registration registration;
			if ( !m_ByServerType.TryGetValue( type, out registration ) )
				return;

			Rebuild( registration );

			foreach ( NetState ns in NetState.Instances )
			{
				if ( IsCapable( ns ) )
					ns.Send( new InvalidateSpellbookCache( registration.Book.ClientBookType, registration.Version, forceReload ) );
			}
		}

		public static void InvalidateAll( bool forceReload )
		{
			foreach ( SpellbookType type in new List<SpellbookType>( m_ByServerType.Keys ) )
				Invalidate( type, forceReload );
		}

		public static IEnumerable<DynamicSpellbook> Books
		{
			get
			{
				foreach ( Registration registration in m_ByServerType.Values )
					yield return registration.Book;
			}
		}

		private static void OnHello( NetState state, PacketReader pvSrc )
		{
			byte version = pvSrc.ReadByte();

			if ( version < 1 )
				return;

			m_CapableClients.Remove( state );
			m_CapableClients.Add( state, new CapableClient( version ) );
		}

		private static void OnSpellbookRequest( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			Serial serial = pvSrc.ReadInt32();
			byte clientBookType = pvSrc.ReadByte();
			uint cachedVersion = pvSrc.ReadUInt32();
			bool forceRefresh = ( pvSrc.ReadByte() & 0x01 ) != 0;

			Registration registration;
			if ( from == null || !m_ByClientType.TryGetValue( clientBookType, out registration ) )
				return;

			Spellbook book = World.FindItem( serial ) as Spellbook;
			if ( book == null || book.Deleted || book.SpellbookType != registration.Book.SpellbookType )
				return;

			// Same reach the legacy gumps allow: carried, or close by (song books can be read off a table).
			if ( from.AccessLevel == AccessLevel.Player && book.RootParent != from && !from.InRange( book.GetWorldLocation(), 2 ) )
				return;

			ulong bitmask = ToDisplayBitmask( registration, registration.Book.GetSpellBitmask( from, book ) );

			if ( !forceRefresh && cachedVersion == registration.Version )
				state.Send( new SpellbookCacheValid( serial, clientBookType, registration.Version, bitmask ) );
			else
				state.Send( new SpellbookFullData( serial, registration.Book, registration.Definitions, registration.Version, bitmask ) );
		}

		private static void OnBookmarkPressed( NetState state, PacketReader pvSrc )
		{
			Mobile from = state.Mobile;

			Serial serial = pvSrc.ReadInt32();
			byte clientBookType = pvSrc.ReadByte();
			uint token = pvSrc.ReadUInt32();

			Registration registration;
			if ( from == null || !m_ByClientType.TryGetValue( clientBookType, out registration ) )
				return;

			DynamicSpellbookBookmark bookmark = registration.Book.Bookmark;
			if ( bookmark == null || bookmark.ActionType != DynamicBookmarkAction.ServerCallback )
				return;

			Spellbook book = World.FindItem( serial ) as Spellbook;
			if ( book == null || book.Deleted || book.SpellbookType != registration.Book.SpellbookType || book.RootParent != from )
				return;

			registration.Book.OnBookmarkPressed( from, book, token );
		}

		private static void Rebuild( Registration registration )
		{
			DynamicSpellbook book = registration.Book;
			IList<SpellDefinition> definitions = book.SpellDefinitions;

			// The client lays spells out in the order it receives them and titles each dictionary page with the next
			// page name, so spells are sent grouped by page (then by the book's sort key). The sort is stable.
			int[] bookIndex = new int[definitions.Count];
			for ( int i = 0; i < bookIndex.Length; i++ )
				bookIndex[i] = i;

			bookIndex = bookIndex.OrderBy( i => book.GetPage( definitions[i] ) ).ThenBy( i => book.GetSortKey( definitions[i] ) ).ToArray();

			List<SpellDefinition> displayOrder = new List<SpellDefinition>( bookIndex.Length );
			List<DynamicSpellDefinition> wire = new List<DynamicSpellDefinition>( bookIndex.Length );

			for ( int position = 0; position < bookIndex.Length; position++ )
			{
				SpellDefinition definition = definitions[bookIndex[position]];
				displayOrder.Add( definition );
				wire.Add( ToWire( book, definition, book.ClientSpellBase + position ) );
			}

			registration.DisplayOrder = displayOrder;
			registration.BookIndex = bookIndex;
			registration.Definitions = wire;
			registration.Version = CalculateVersion( book, wire );
		}

		// Spellbook.Content keeps one bit per spell in book order; the client expects one bit per display position.
		private static ulong ToDisplayBitmask( Registration registration, ulong bookBitmask )
		{
			ulong displayBitmask = 0;

			for ( int position = 0; position < registration.BookIndex.Length; position++ )
			{
				if ( ( bookBitmask & ( 1ul << registration.BookIndex[position] ) ) != 0 )
					displayBitmask |= 1ul << position;
			}

			return displayBitmask;
		}

		private static DynamicSpellDefinition ToWire( DynamicSpellbook book, SpellDefinition definition, int clientSpellId )
		{
			DynamicSpellDefinition spell = new DynamicSpellDefinition();

			spell.SpellID = (ushort)clientSpellId;
			spell.IconGraphic = book.GetIcon( definition );

			if ( definition.Name != null && definition.Name.Number > 0 )
				spell.NameCliloc = definition.Name.Number;

			spell.Name = definition.Name != null ? definition.Name.String : null;
			spell.PowerWords = definition.PowerWords != null ? definition.PowerWords.String : null;
			spell.Description = book.GetDescription( definition );
			spell.ManaCost = (byte)Math.Max( 0, Math.Min( 255, definition.ManaCost ) );
			spell.MinSkill = (byte)Math.Max( 0, Math.Min( 255, definition.MinSkill ) );
			spell.TargetType = (byte)definition.TargetType;
			spell.Page = book.GetPage( definition );
			spell.CustomReagents = book.GetReagents( definition );

			return spell;
		}

		private static uint CalculateVersion( DynamicSpellbook book, List<DynamicSpellDefinition> definitions )
		{
			StringBuilder sb = new StringBuilder();

			sb.Append( ProtocolVersion ).Append( '|' );
			sb.Append( book.BookGraphic ).Append( '|' ).Append( book.MinimizedGraphic ).Append( '|' );
			sb.Append( book.SpellsPerPageSide ).Append( '|' ).Append( book.MaxDictionaryPages ).Append( '|' );
			sb.Append( book.DisplayManaCost ).Append( book.DisplayMinSkill ).Append( book.DisplayPowerWords ).Append( '|' );
			sb.Append( book.ManaCostLabel ).Append( '|' ).Append( book.MinSkillLabel ).Append( '|' );
			sb.Append( book.CustomPropertyTitle ).Append( '|' ).Append( book.CustomPropertyLabel ).Append( '|' ).Append( book.CustomPropertyName ).Append( '|' );
			sb.Append( book.BookHue ).Append( '|' ).Append( book.TextColor ).Append( '|' ).Append( book.SpellNameColor ).Append( '|' ).Append( book.TitleColor ).Append( '|' );
			sb.Append( book.ContentOffsetX ).Append( '|' ).Append( book.ContentOffsetY ).Append( '|' );
			sb.Append( book.PageTurnLeftGraphic ).Append( '|' ).Append( book.PageTurnRightGraphic ).Append( '|' );
			sb.Append( book.PageTurnLeftX ).Append( '|' ).Append( book.PageTurnLeftY ).Append( '|' );
			sb.Append( book.PageTurnRightX ).Append( '|' ).Append( book.PageTurnRightY ).Append( '|' );

			if ( book.OverlayGraphics != null )
			{
				foreach ( ushort overlay in book.OverlayGraphics )
					sb.Append( overlay ).Append( ',' );
			}

			sb.Append( '|' );

			if ( book.PageNames != null )
			{
				foreach ( string pageName in book.PageNames )
					sb.Append( pageName ).Append( ',' );
			}

			sb.Append( '|' );

			foreach ( DynamicSpellDefinition spell in definitions )
			{
				sb.Append( spell.SpellID ).Append( ',' ).Append( spell.IconGraphic ).Append( ',' ).Append( spell.NameCliloc ).Append( ',' );
				sb.Append( spell.Name ).Append( ',' ).Append( spell.PowerWords ).Append( ',' ).Append( spell.Description ).Append( ',' );
				sb.Append( spell.ManaCost ).Append( ',' ).Append( spell.MinSkill ).Append( ',' ).Append( spell.TargetType ).Append( ',' );
				sb.Append( spell.Reagents ).Append( ',' ).Append( spell.Cooldown ).Append( ',' ).Append( spell.Page ).Append( ',' );

				if ( spell.CustomReagents != null )
					sb.Append( string.Join( "/", spell.CustomReagents ) );

				sb.Append( ';' );
			}

			sb.Append( '|' );

			if ( book.InfoPages != null )
			{
				foreach ( DynamicInfoPage page in book.InfoPages )
					sb.Append( page.Title ).Append( ',' ).Append( page.Body ).Append( ';' );
			}

			sb.Append( '|' );

			DynamicSpellbookBookmark bookmark = book.Bookmark;
			if ( bookmark != null )
			{
				sb.Append( bookmark.Graphic ).Append( ',' ).Append( bookmark.PressedGraphic ).Append( ',' );
				sb.Append( bookmark.X ).Append( ',' ).Append( bookmark.Y ).Append( ',' ).Append( bookmark.Hue ).Append( ',' );
				sb.Append( bookmark.Page ).Append( ',' ).Append( bookmark.ActionType ).Append( ',' ).Append( bookmark.Action ).Append( ',' );
				sb.Append( bookmark.Tooltip );
			}

			using ( MD5 md5 = MD5.Create() )
			{
				byte[] hash = md5.ComputeHash( Encoding.UTF8.GetBytes( sb.ToString() ) );
				return BitConverter.ToUInt32( hash, 0 );
			}
		}
	}
}
