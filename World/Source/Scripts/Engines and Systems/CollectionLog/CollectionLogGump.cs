using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Collectibles;
using Server.Items;
using Server.Network;
using Server.Utilities;

namespace Server.Gumps
{
	public class CollectionLogGump : Gump
	{
		private const int CATEGORY_BUTTON_OFFSET = 5000;
		private const int SCAN_BUTTON = 1;

		private const int GUMP_WIDTH = 840;
		private const int GUMP_HEIGHT = 770;

		// Right panel (the scrolling viewport)
		private const int PANEL_X = 290;
		private const int PANEL_W = 500;
		private const int VIEWPORT_TOP = 95;
		private const int VIEWPORT_BOTTOM = 700;
		private const int VIEWPORT_HEIGHT = VIEWPORT_BOTTOM - VIEWPORT_TOP;

		// Scroll-arrow column lives to the right of the viewport
		private const int ARROW_X = PANEL_X + PANEL_W + 12;

		// Section cell sizes (must match GRID widths below)
		private const int SMALL_CELL = 70;
		private const int MEDIUM_CELL = 120;
		private const int LARGE_CELL = 190;

		// Cols per bucket so each row spans roughly PANEL_W (480)
		private const int SMALL_COLS = 6;   //  6 * 70  = 420
		private const int MEDIUM_COLS = 4;  //  4 * 120 = 480
		private const int LARGE_COLS = 2;   //  2 * 190 = 380

		// Hues
		private const int LABEL_HUE = 1153;
		private const int HEADER_HUE = 0x35;
		private const int COLLECTED_HUE = 0;
		private const int DIMMED_HUE = 0x3E9;

		private readonly Mobile m_From;
		private readonly CollectionLog m_Log;
		private readonly int m_CategoryID;

		public CollectionLogGump( Mobile from, CollectionLog log, int categoryID ) : base( 50, 50 )
		{
			m_From = from;
			m_Log = log;
			m_CategoryID = categoryID;

			Closable = true;
			Disposable = true;
			Dragable = true;
			Resizable = false;

			var account = from.Account as Account;
			var collected = account != null ? CollectionLogStorage.GetCollected( account ) : new HashSet<int>();
			int total = CollectionLogCatalog.TotalCount;
			int collectedCount = collected.Count( id => CollectionLogCatalog.ContainsItemID( id ) );
			int percent = total > 0 ? (int) Math.Round( 100.0 * collectedCount / total ) : 0;

			// --- Page 0: everything that's always visible ---
			AddPage( 0 );
			AddBackground( 0, 0, GUMP_WIDTH, GUMP_HEIGHT, 3600 );

			AddLabel( 24, 20, LABEL_HUE, "Collection Log" );
			AddLabel( 24, 44, HEADER_HUE, string.Format( "{0} / {1} collected   ({2}%)", collectedCount, total, percent ) );

			AddImageTiled( PANEL_X, 50, 500, 9, 9750 );
			if ( collectedCount > 0 && total > 0 )
			{
				int fillWidth = (int) ( 500.0 * collectedCount / total );
				if ( fillWidth > 0 )
					AddImageTiled( PANEL_X, 50, fillWidth, 9, 9752 );
			}

			DrawCategories( collected );

			var selected = CollectionLogCatalog.FindCategory( m_CategoryID ) ?? CollectionLogCatalog.Categories[0];
			AddLabel( PANEL_X, 70, HEADER_HUE, selected.Name );

			// Scan button (always visible)
			AddButton( 24, GUMP_HEIGHT - 50, 4023, 4025, SCAN_BUTTON, GumpButtonType.Reply, 0 );
			AddLabel( 60, GUMP_HEIGHT - 48, LABEL_HUE, "Scan Item" );

			// --- Scroll pages ---
			var rows = BuildRows( selected );
			var pages = PackRowsIntoPages( rows, VIEWPORT_HEIGHT );

			for ( int p = 0; p < pages.Count; p++ )
			{
				AddPage( p + 1 );
				DrawScrollPage( pages[p], collected );

				if ( p > 0 )
					AddButton( ARROW_X, VIEWPORT_TOP, 4014, 4015, 0, GumpButtonType.Page, p ); // ↑
				if ( p < pages.Count - 1 )
					AddButton( ARROW_X, VIEWPORT_BOTTOM - 18, 4005, 4006, 0, GumpButtonType.Page, p + 2 ); // ↓

				if ( pages.Count > 1 )
					AddLabel( ARROW_X - 4, ( VIEWPORT_TOP + VIEWPORT_BOTTOM ) / 2, LABEL_HUE, ( p + 1 ) + "/" + pages.Count );
			}
		}

		private void DrawCategories( HashSet<int> collected )
		{
			const int SIDEBAR_X = 20;
			const int SIDEBAR_Y = 80;
			const int CATEGORY_HEIGHT = 32;
			const int SIDEBAR_W = 260;
			const int BULLET_X = 8;     // bullet position inside the row
			const int LABEL_X = 38;     // label starts AFTER the bullet
			const int COUNT_RIGHT_PAD = 12;

			int y = SIDEBAR_Y;

			foreach ( var cat in CollectionLogCatalog.Categories )
			{
				if ( cat.Entries.Count == 0 )
					continue;

				int catTotal = cat.Entries.Count;
				int catCollected = cat.Entries.Count( e => collected.Contains( e.ItemID ) );

				bool isSelected = cat.ID == m_CategoryID;

				int bgID = isSelected ? 9354 : 9350;
				AddBackground( SIDEBAR_X, y, SIDEBAR_W, CATEGORY_HEIGHT, bgID );

				if ( !isSelected )
					AddButton( SIDEBAR_X + BULLET_X, y + 6, 2151, 2152,
						CATEGORY_BUTTON_OFFSET + cat.ID, GumpButtonType.Reply, 0 );
				else
					AddImage( SIDEBAR_X + BULLET_X, y + 6, 2151, 1152 ); // hued selected indicator

				AddLabel( SIDEBAR_X + LABEL_X, y + 6, isSelected ? HEADER_HUE : LABEL_HUE, cat.Name );

				string countText = string.Format( "{0}/{1}", catCollected, catTotal );
				int countW = countText.Length * 7;
				AddLabel( SIDEBAR_X + SIDEBAR_W - COUNT_RIGHT_PAD - countW, y + 6, LABEL_HUE, countText );

				y += CATEGORY_HEIGHT + 4;
			}
		}

		// One row of cells of identical size. CellSize is also the row height.
		private class ContentRow
		{
			public List<CollectionEntry> Items;
			public int CellSize;
			public int Cols;
		}

		private List<ContentRow> BuildRows( CollectionCategory category )
		{
			var rows = new List<ContentRow>();

			AddBucketRows( rows, CollectionLogCatalog.EntriesInBucket( category, SizeBucket.Small ).ToList(),  SMALL_CELL,  SMALL_COLS );
			AddBucketRows( rows, CollectionLogCatalog.EntriesInBucket( category, SizeBucket.Medium ).ToList(), MEDIUM_CELL, MEDIUM_COLS );
			AddBucketRows( rows, CollectionLogCatalog.EntriesInBucket( category, SizeBucket.Large ).ToList(),  LARGE_CELL,  LARGE_COLS );

			return rows;
		}

		private void AddBucketRows( List<ContentRow> rows, List<CollectionEntry> entries, int cellSize, int cols )
		{
			for ( int i = 0; i < entries.Count; i += cols )
			{
				rows.Add( new ContentRow {
					Items = entries.Skip( i ).Take( cols ).ToList(),
					CellSize = cellSize,
					Cols = cols
				} );
			}
		}

		private List<List<ContentRow>> PackRowsIntoPages( List<ContentRow> rows, int viewportHeight )
		{
			var pages = new List<List<ContentRow>>();
			var current = new List<ContentRow>();
			int currentHeight = 0;

			foreach ( var row in rows )
			{
				if ( currentHeight + row.CellSize > viewportHeight && current.Count > 0 )
				{
					pages.Add( current );
					current = new List<ContentRow>();
					currentHeight = 0;
				}

				current.Add( row );
				currentHeight += row.CellSize;
			}

			if ( current.Count > 0 )
				pages.Add( current );

			if ( pages.Count == 0 )
				pages.Add( new List<ContentRow>() );

			return pages;
		}

		private void DrawScrollPage( List<ContentRow> rows, HashSet<int> collected )
		{
			int y = VIEWPORT_TOP;

			foreach ( var row in rows )
			{
				for ( int i = 0; i < row.Items.Count; i++ )
				{
					int x = PANEL_X + i * row.CellSize;
					DrawCell( row.Items[i], collected, x, y, row.CellSize );
				}

				y += row.CellSize;
			}
		}

		private void DrawCell( CollectionEntry entry, HashSet<int> collected, int cellX, int cellY, int cellSize )
		{
			bool isCollected = collected.Contains( entry.ItemID );

			int innerW = cellSize - 6;
			int innerH = cellSize - 6;
			AddImageTiled( cellX, cellY, innerW, innerH, 2624 );
			AddImageTiled( cellX, cellY, innerW, 1, 9101 );
			AddImageTiled( cellX, cellY, 1, innerH, 9101 );
			AddImageTiled( cellX + innerW, cellY, 1, innerH, 9101 );
			AddImageTiled( cellX, cellY + innerH, innerW, 1, 9101 );

			int hue = isCollected ? ( entry.Hue == 0 ? COLLECTED_HUE : entry.Hue ) : DIMMED_HUE;
			DrawComposite( entry, cellX, cellY, innerW, innerH, hue );
		}

		private void DrawComposite( CollectionEntry entry, int cellX, int cellY, int w, int h, int hue )
		{
			var composite = CollectionLogCatalog.GetCompositeBounds( entry );

			int cellCenterX = cellX + w / 2;
			int cellCenterY = cellY + h / 2;
			int compositeCenterX = composite.X + composite.Width / 2;
			int compositeCenterY = composite.Y + composite.Height / 2;

			int offsetX = cellCenterX - compositeCenterX;
			int offsetY = cellCenterY - compositeCenterY;

			foreach ( var tile in entry.RenderTiles() )
			{
				int sdx = CollectionLogCatalog.ScreenDX( tile.DX, tile.DY );
				int sdy = CollectionLogCatalog.ScreenDY( tile.DX, tile.DY );

				AddItem( offsetX + sdx, offsetY + sdy, tile.ItemID, hue );
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info == null || sender == null || sender.Mobile == null )
				return;

			int id = info.ButtonID;

			if ( id == 0 )
				return;

			if ( id == SCAN_BUTTON )
			{
				if ( m_Log != null && !m_Log.Deleted )
					m_Log.BeginScanTarget( sender.Mobile );
				return;
			}

			if ( id >= CATEGORY_BUTTON_OFFSET )
			{
				int catID = id - CATEGORY_BUTTON_OFFSET;
				sender.Mobile.CloseGump( typeof( CollectionLogGump ) );
				sender.Mobile.SendGump( new CollectionLogGump( sender.Mobile, m_Log, catID ) );
				return;
			}
		}
	}
}
