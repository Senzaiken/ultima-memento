using System;
using System.Collections.Generic;
using System.Linq;
using Server;

namespace Server.Collectibles
{
	public enum SizeBucket { Small, Medium, Large }

	public class TileComponent
	{
		public int ItemID { get; private set; }
		public int DX { get; private set; }
		public int DY { get; private set; }

		public TileComponent( int itemID, int dx, int dy )
		{
			ItemID = itemID;
			DX = dx;
			DY = dy;
		}
	}

	public class CollectionEntry
	{
		public int ItemID { get; private set; }
		public string Name { get; private set; }
		public int Hue { get; private set; }
		public int[] Aliases { get; private set; }
		public TileComponent[] DisplayTiles { get; private set; }

		public CollectionEntry( int itemID, string name ) : this( itemID, name, 0, null, null ) { }

		public CollectionEntry( int itemID, string name, int[] aliases ) : this( itemID, name, 0, aliases, null ) { }

		public CollectionEntry( int itemID, string name, int[] aliases, TileComponent[] displayTiles ) : this( itemID, name, 0, aliases, displayTiles ) { }

		public CollectionEntry( int itemID, string name, int hue, int[] aliases, TileComponent[] displayTiles )
		{
			ItemID = itemID;
			Name = name;
			Hue = hue;
			Aliases = aliases ?? new int[0];
			DisplayTiles = displayTiles;
		}

		public bool Matches( int scannedID )
		{
			if ( scannedID == ItemID )
				return true;

			for ( int i = 0; i < Aliases.Length; i++ )
			{
				if ( Aliases[i] == scannedID )
					return true;
			}

			return false;
		}

		public IEnumerable<TileComponent> RenderTiles()
		{
			if ( DisplayTiles != null && DisplayTiles.Length > 0 )
			{
				foreach ( var t in DisplayTiles )
					yield return t;
			}
			else
			{
				yield return new TileComponent( ItemID, 0, 0 );
			}
		}
	}

	public class CollectionCategory
	{
		public int ID { get; private set; }
		public string Name { get; private set; }
		public List<CollectionEntry> Entries { get; private set; }

		public CollectionCategory( int id, string name, List<CollectionEntry> entries )
		{
			ID = id;
			Name = name;
			Entries = entries;
		}
	}

	public static class CollectionLogCatalog
	{
		private static int[] A( params int[] ids ) { return ids; }
		private static TileComponent T( int itemID, int dx, int dy ) { return new TileComponent( itemID, dx, dy ); }
		private static TileComponent[] D( params TileComponent[] tiles ) { return tiles; }

		public static readonly List<CollectionCategory> Categories = new List<CollectionCategory>
		{
			new CollectionCategory( 1, "Tapestries", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0EAA, "Tapestry I",   A( 0x0EAB ),                       D( T( 0x0EAA, 0, 0 ), T( 0x0EAB,  1, 0 ) ) ),
				new CollectionEntry( 0x0EAC, "Tapestry II",  A( 0x0EAD, 0x0EAE, 0x0EAF ),       D( T( 0x0EAC, 0, 0 ), T( 0x0EAD,  1, 0 ) ) ),
				new CollectionEntry( 0x0FD6, "Tapestry III", A( 0x0FD5, 0x0FD7, 0x0FD8 ),       D( T( 0x0FD5,-2, 0 ), T( 0x0FD6,  0, 0 ) ) ),
				new CollectionEntry( 0x0FDA, "Tapestry IV",  A( 0x0FD9, 0x0FDB, 0x0FDC ),       D( T( 0x0FD9,-1, 0 ), T( 0x0FDA,  0, 0 ) ) ),
				new CollectionEntry( 0x0FDE, "Tapestry V",   A( 0x0FDD, 0x0FDF, 0x0FE0 ),       D( T( 0x0FDD,-1, 0 ), T( 0x0FDE,  0, 0 ) ) ),
				new CollectionEntry( 0x0FE2, "Tapestry VI",  A( 0x0FE1, 0x0FE3, 0x0FE4 ),       D( T( 0x0FE1,-1, 0 ), T( 0x0FE2,  0, 0 ) ) ),
			} ),

			new CollectionCategory( 2, "Paintings & Portraits", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0EA0, "Large Painting" ),
				new CollectionEntry( 0x0E9F, "Woman Portrait I",  A( 0x0EC8 ) ),
				new CollectionEntry( 0x0EE7, "Woman Portrait II", A( 0x0EC9 ) ),
				new CollectionEntry( 0x0EA2, "Man Portrait I",    A( 0x0EA1 ) ),
				new CollectionEntry( 0x0EA3, "Man Portrait II",   A( 0x0EA4 ) ),
				new CollectionEntry( 0x0EA6, "Lady Portrait I",   A( 0x0EA5 ) ),
				new CollectionEntry( 0x0EA7, "Lady Portrait II",  A( 0x0EA8 ) ),
			} ),

			new CollectionCategory( 3, "Statues", new List<CollectionEntry>
			{
				new CollectionEntry( 0x1224, "Statue of a Woman",       A( 0x139A ) ),
				new CollectionEntry( 0x1227, "Statue of a Man",         A( 0x139C ) ),
				new CollectionEntry( 0x1226, "Statue of an Angel",      A( 0x139B ) ),
				new CollectionEntry( 0x1228, "Statue of a Pegasus",     A( 0x139D ) ),
				new CollectionEntry( 0x1225, "Statue of a Figure (SE)" ),
				new CollectionEntry( 0x12CA, "Bust of a Man",           A( 0x12CB ) ),
				new CollectionEntry( 0x207C, "Statue of an Angel (alt)" ),
				new CollectionEntry( 0x42BB, "Statue of a Gargoyle" ),
				new CollectionEntry( 0x42BC, "Bust of a Demon" ),
				new CollectionEntry( 0x42C0, "Statue of a Demon (sm)",  A( 0x42C1 ) ),
				new CollectionEntry( 0x42C2, "Statue of an Odd Creature" ),
				new CollectionEntry( 0x42C5, "Statue of a Demon (lg)" ),
				new CollectionEntry( 0x40BC, "Statue of a Medusa" ),
				new CollectionEntry( 0x48A8, "Statue of a Dragon Head", A( 0x48A9 ) ),
				new CollectionEntry( 0x4578, "Statue of a Seahorse",    A( 0x4579 ) ),
				new CollectionEntry( 0x457A, "Statue of a Mermaid",     A( 0x457B ) ),
				new CollectionEntry( 0x457C, "Statue of a Gryphon",     A( 0x457D ) ),
				new CollectionEntry( 0x3F19, "Statue of a God",         A( 0x3F1A ) ),
				new CollectionEntry( 0x3F1B, "Statue of a Knight",      A( 0x3F1C ) ),
				new CollectionEntry( 0x4688, "Statue of a Cat",         A( 0x4689 ) ),
				new CollectionEntry( 0x3142, "Statue of a Lion",        A( 0x3143 ) ),
				new CollectionEntry( 0x3182, "Statue of a Lion (alt)" ),
				new CollectionEntry( 0x31C1, "Statue of a Pegasus (alt)", A( 0x31C2 ) ),
				new CollectionEntry( 0x31C7, "Statue of a Knight (alt)",  A( 0x31C8 ) ),
				new CollectionEntry( 0x31CB, "Statue of an Explorer",     A( 0x31CC ) ),
				new CollectionEntry( 0x31CD, "Statue of a Wizard",        A( 0x31CE ) ),
				new CollectionEntry( 0x31CF, "Statue of a Spearman",      A( 0x31D0 ) ),
				new CollectionEntry( 0x31D1, "Statue of a Priest",        A( 0x31D2 ) ),
				new CollectionEntry( 0x31D3, "Statue of a King",          A( 0x31D4 ) ),
				new CollectionEntry( 0x31FC, "Statue of a God (alt)",     A( 0x31FD ) ),
				new CollectionEntry( 0x31FE, "Statue of a Guard",         A( 0x31FF ) ),
				new CollectionEntry( 0x320B, "Statue of an Elf I",        A( 0x3219 ) ),
				new CollectionEntry( 0x320C, "Statue of an Elf II",       A( 0x3212 ) ),
				new CollectionEntry( 0x321F, "Statue of an Elf III",      A( 0x3225 ) ),
				new CollectionEntry( 0x322B, "Statue of an Elf IV",       A( 0x3235 ) ),
				new CollectionEntry( 0x1947, "Statue of Budah",           A( 0x1948 ) ),
				new CollectionEntry( 0x2419, "Small Sculpture" ),
				new CollectionEntry( 0x241A, "Small Tower Sculpture" ),
				new CollectionEntry( 0x241B, "Tall Sculpture" ),
				new CollectionEntry( 0x2848, "Player Sculpture",          A( 0x2849 ) ),
			} ),

			new CollectionCategory( 4, "Wall Trophies", new List<CollectionEntry>
			{
				// Decorative Shields (11 + 2 sword-on-shields)
				new CollectionEntry( 0x156C, "Decorative Shield I",    A( 0x156D ) ),
				new CollectionEntry( 0x156E, "Decorative Shield II",   A( 0x156F ) ),
				new CollectionEntry( 0x1570, "Decorative Shield III",  A( 0x1571 ) ),
				new CollectionEntry( 0x1572, "Decorative Shield IV",   A( 0x1573 ) ),
				new CollectionEntry( 0x1574, "Decorative Shield V",    A( 0x1575 ) ),
				new CollectionEntry( 0x1576, "Decorative Shield VI",   A( 0x1577 ) ),
				new CollectionEntry( 0x1578, "Decorative Shield VII",  A( 0x1579 ) ),
				new CollectionEntry( 0x157A, "Decorative Shield VIII", A( 0x157B ) ),
				new CollectionEntry( 0x157C, "Decorative Shield IX",   A( 0x157D ) ),
				new CollectionEntry( 0x157E, "Decorative Shield X",    A( 0x157F ) ),
				new CollectionEntry( 0x1580, "Decorative Shield XI",   A( 0x1581 ) ),
				new CollectionEntry( 0x1582, "Shield with Swords I",   A( 0x1583, 0x1634, 0x1635 ) ),
				new CollectionEntry( 0x1584, "Shield with Swords II",  A( 0x1585, 0x1636, 0x1637 ) ),
				// Decorative Weapons
				new CollectionEntry( 0x155C, "Wall Bow",      A( 0x155D, 0x155E, 0x155F ) ),
				new CollectionEntry( 0x1560, "Wall Axe",      A( 0x1561, 0x1562, 0x1563 ) ),
				new CollectionEntry( 0x1564, "Wall Sword",    A( 0x1565, 0x1566, 0x1567 ) ),
				new CollectionEntry( 0x1568, "Wall Double Axe", A( 0x1569, 0x156A, 0x156B ) ),
			} ),

			new CollectionCategory( 5, "Doom Artifacts", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0B47, "Artifact Large Vase" ),
				new CollectionEntry( 0x0B48, "Artifact Vase" ),
				new CollectionEntry( 0x0913, "Dirt Patch" ),
				new CollectionEntry( 0x1F18, "Evil Idol Skull" ),
				new CollectionEntry( 0x2204, "Skull Pole" ),
				new CollectionEntry( 0x1D94, "Wall of Blood",       A( 0x1D95 ) ),
				new CollectionEntry( 0x224E, "Demon Skull",         A( 0x224F, 0x2250, 0x2251 ) ),
				new CollectionEntry( 0x295C, "Futon",               A( 0x295D, 0x295E, 0x295F ) ),
				new CollectionEntry( 0x12EE, "Lava Tile" ),
				new CollectionEntry( 0x320D, "Swamp Tile" ),
				new CollectionEntry( 0x346E, "Water Tile" ),
				new CollectionEntry( 0x0E21, "Tattered Ancient Mummy Wrapping" ),
				new CollectionEntry( 0x3486, "Pier",                A( 0x348B, 0x03AE ) ),
				new CollectionEntry( 0x10D7, "Web",                 A( 0x10D8, 0x10DD ) ),
			} ),

			new CollectionCategory( 6, "Broken Furniture", new List<CollectionEntry>
			{
				new CollectionEntry( 0x3F21, "Broken Armoire (deed)",        A( 0x0C12, 0x0C13 ) ),
				new CollectionEntry( 0x3F1E, "Broken Bed (deed)",            A( 0x1894, 0x1895, 0x1896, 0x1897, 0x1898, 0x1899, 0x189A, 0x189B ) ),
				new CollectionEntry( 0x3F22, "Broken Bookcase (deed)",       A( 0x0C14, 0x0C15 ) ),
				new CollectionEntry( 0x3F23, "Broken Chest of Drawers (deed)", A( 0x0C24, 0x0C25 ) ),
				new CollectionEntry( 0x3F26, "Broken Covered Chair (deed)",  A( 0x0C18 ) ),
				new CollectionEntry( 0x3F20, "Broken Vanity (deed)",         A( 0x0C20, 0x0C21, 0x0C22, 0x0C23 ) ),
				new CollectionEntry( 0x3F24, "Broken Fallen / Standing Chair (deed)", A( 0x0C17, 0x0C1B, 0x0C1C, 0x0C1D, 0x0C1E ) ),
				new CollectionEntry( 0x0C19, "Broken Chair (sitting)",       A( 0x0C1A ) ),
			} ),

			new CollectionCategory( 7, "Evil Home Decor", new List<CollectionEntry>
			{
				// All Evil Home Decor deeds spawn as ItemID 0x3420 or 0x3425 (crate/box wrappers),
				// so the canonical id below is the *placed* component.
				new CollectionEntry( 0x2A58, "Bone Throne",            A( 0x2A59 ) ),
				new CollectionEntry( 0x2A5D, "Disturbing Portrait",    A( 0x2A5E, 0x2A5F, 0x2A60, 0x2A61, 0x2A62, 0x2A63, 0x2A64 ) ),
				new CollectionEntry( 0x2A65, "Unsettling Portrait",    A( 0x2A66, 0x2A67, 0x2A68 ) ),
				new CollectionEntry( 0x2A69, "Creepy Portrait",        A( 0x2A6A, 0x2A6B, 0x2A6C, 0x2A6D ) ),
				new CollectionEntry( 0x2A71, "Mounted Pixie (Green)",  A( 0x2A72 ) ),
				new CollectionEntry( 0x2A73, "Mounted Pixie (Orange)", A( 0x2A74 ) ),
				new CollectionEntry( 0x2A75, "Mounted Pixie (Blue)",   A( 0x2A76 ) ),
				new CollectionEntry( 0x2A77, "Mounted Pixie (Lime)",   A( 0x2A78 ) ),
				new CollectionEntry( 0x2A79, "Mounted Pixie (White)",  A( 0x2A7A ) ),
				new CollectionEntry( 0x2A7B, "Haunted Mirror",         A( 0x2A7D ) ),
				new CollectionEntry( 0x2A9B, "Sacrificial Altar",      A( 0x2A9A, 0x2A9C, 0x2A9D ) ),
				new CollectionEntry( 0x3420, "Evil Decor Crate (box)", A( 0x3425 ) ),
			} ),

			new CollectionCategory( 8, "Containers & Vessels", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0FAE, "Closed Barrel" ),
				new CollectionEntry( 0x1EB5, "Unfinished Barrel" ),
				new CollectionEntry( 0x0E77, "Water Barrel",  A( 0x154D ) ),
				new CollectionEntry( 0x14E0, "Bucket",        A( 0x2004 ) ),
				new CollectionEntry( 0x0E83, "Water Tub",     A( 0x0E7B ) ),
				new CollectionEntry( 0x1519, "Huge Water Tub", A( 0x1534 ) ),
			} ),

			new CollectionCategory( 9, "Pagan Reagents", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0F79, "Blackmoor" ),
				new CollectionEntry( 0x0F7C, "Bloodspawn" ),
				new CollectionEntry( 0x0F7F, "Brimstone" ),
				new CollectionEntry( 0x4077, "Dragon's Blood I" ),
				new CollectionEntry( 0x0F82, "Dragon's Blood II" ),
				new CollectionEntry( 0x0F87, "Eye of Newt" ),
				new CollectionEntry( 0x18E1, "Garlic",        A( 0x18E2 ) ),
				new CollectionEntry( 0x18E3, "Garlic Bulb",   A( 0x18E4 ) ),
				new CollectionEntry( 0x18E9, "Ginseng",       A( 0x18EA ) ),
				new CollectionEntry( 0x18EB, "Ginseng Root",  A( 0x18EC ) ),
				new CollectionEntry( 0x18DF, "Mandrake",      A( 0x18E0 ) ),
				new CollectionEntry( 0x18DD, "Mandrake Root", A( 0x18DE ) ),
				new CollectionEntry( 0x18E5, "Nightshade",    A( 0x18E6, 0x18E7 ) ),
				new CollectionEntry( 0x0F89, "Obsidian" ),
				new CollectionEntry( 0x0F8B, "Pumice" ),
				new CollectionEntry( 0x0F91, "Wyrm's Heart" ),
			} ),

			new CollectionCategory( 10, "Flowers", new List<CollectionEntry>
			{
				new CollectionEntry( 0x18D9, "Wild Flower",     A( 0x18DA ) ),
				new CollectionEntry( 0x234B, "Rose of Trinsic", A( 0x234C, 0x234D ) ),
			} ),

			new CollectionCategory( 11, "Tarot Cards", new List<CollectionEntry>
			{
				new CollectionEntry( 0x12A5, "Tarot Card I" ),
				new CollectionEntry( 0x12A6, "Tarot Card II" ),
				new CollectionEntry( 0x12A7, "Tarot Card III" ),
				new CollectionEntry( 0x12A8, "Tarot Card IV" ),
				new CollectionEntry( 0x12A9, "Tarot Card V" ),
				new CollectionEntry( 0x12AA, "Tarot Card VI" ),
				new CollectionEntry( 0x12AB, "Deck of Tarot",       A( 0x12AC ) ),
			} ),

			new CollectionCategory( 12, "Playing Cards", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0E15, "Playing Cards III" ),
				new CollectionEntry( 0x0E16, "Playing Cards II" ),
				new CollectionEntry( 0x0E17, "Playing Cards IV" ),
				new CollectionEntry( 0x0E18, "Playing Cards V" ),
				new CollectionEntry( 0x0E19, "Playing Cards I" ),
				new CollectionEntry( 0x0FA2, "Playing Cards Deck",  A( 0x0FA3 ) ),
			} ),

			new CollectionCategory( 13, "Jars", new List<CollectionEntry>
			{
				new CollectionEntry( 0x1005, "Empty Jar (group)", A( 0x0E44, 0x0E45, 0x0E46, 0x0E47 ) ),
				new CollectionEntry( 0x1006, "Full Jar (group)",  A( 0x0E4A, 0x0E4B ) ),
				new CollectionEntry( 0x1007, "Half-Empty Jar (group)", A( 0x0E4C, 0x0E4D, 0x0E4E, 0x0E4F ) ),
			} ),

			new CollectionCategory( 14, "Magic Curiosities", new List<CollectionEntry>
			{
				new CollectionEntry( 0x0E2E, "Crystal Ball" ),
				new CollectionEntry( 0x1F19, "Magical Crystal" ),
			} ),

			new CollectionCategory( 15, "Decorative Rocks", new List<CollectionEntry>
			{
				new CollectionEntry( 0x1778, "Small Rock" ),
				new CollectionEntry( 0x1363, "Stone" ),
				new CollectionEntry( 0x1367, "Stones" ),
				new CollectionEntry( 0x136D, "Stone Pile" ),
			} ),

			new CollectionCategory( 16, "Tinker Curios", new List<CollectionEntry>
			{
				new CollectionEntry( 0x1EB6, "Empty Tool Kit",       A( 0x1EB7 ) ),
				new CollectionEntry( 0x1BFC, "Crossbow Bolts" ),
			} ),

			new CollectionCategory( 17, "Miscellaneous Rares", new List<CollectionEntry>
			{
				new CollectionEntry( 0x1003, "Spittoon" ),
			} ),
		};

		public static int Resolve( int scannedID )
		{
			foreach ( var cat in Categories )
			{
				foreach ( var entry in cat.Entries )
				{
					if ( entry.Matches( scannedID ) )
						return entry.ItemID;
				}
			}

			return -1;
		}

		public static bool ContainsItemID( int itemID )
		{
			return Resolve( itemID ) >= 0;
		}

		public static IEnumerable<int> AllCanonicalIDs()
		{
			return Categories.SelectMany( c => c.Entries ).Select( e => e.ItemID );
		}

		public static int TotalCount
		{
			get { return Categories.Sum( c => c.Entries.Count ); }
		}

		public static CollectionCategory FindCategory( int id )
		{
			return Categories.FirstOrDefault( c => c.ID == id );
		}

		// --- composite bounds + bucket classification ---

		// UO iso projection: +1 east tile = (+22, +22) screen; +1 south tile = (-22, +22) screen
		public const int ISO_HALF_TILE = 22;

		public static int ScreenDX( int dx, int dy ) { return ( dx - dy ) * ISO_HALF_TILE; }
		public static int ScreenDY( int dx, int dy ) { return ( dx + dy ) * ISO_HALF_TILE; }

		// Returns the bounding box (in art-space coords) needed to draw all tiles centered around (0,0).
		public static Rectangle2D GetCompositeBounds( CollectionEntry entry )
		{
			int? minX = null, minY = null, maxX = null, maxY = null;

			foreach ( var tile in entry.RenderTiles() )
			{
				if ( tile.ItemID <= 0 || tile.ItemID >= ItemBounds.Table.Length )
					continue;

				var b = ItemBounds.Table[tile.ItemID];
				int sdx = ScreenDX( tile.DX, tile.DY );
				int sdy = ScreenDY( tile.DX, tile.DY );

				int tMinX = b.X + sdx;
				int tMinY = b.Y + sdy;
				int tMaxX = tMinX + b.Width;
				int tMaxY = tMinY + b.Height;

				if ( minX == null || tMinX < minX ) minX = tMinX;
				if ( minY == null || tMinY < minY ) minY = tMinY;
				if ( maxX == null || tMaxX > maxX ) maxX = tMaxX;
				if ( maxY == null || tMaxY > maxY ) maxY = tMaxY;
			}

			if ( minX == null )
				return new Rectangle2D( 0, 0, 44, 44 );

			return new Rectangle2D( minX.Value, minY.Value, maxX.Value - minX.Value, maxY.Value - minY.Value );
		}

		// Cell-fit thresholds — items larger than these in either dimension go into the next bucket.
		public const int SMALL_MAX = 50;
		public const int MEDIUM_MAX = 100;

		public static SizeBucket ClassifyBucket( CollectionEntry entry )
		{
			var b = GetCompositeBounds( entry );
			int max = Math.Max( b.Width, b.Height );

			if ( max <= SMALL_MAX )  return SizeBucket.Small;
			if ( max <= MEDIUM_MAX ) return SizeBucket.Medium;
			return SizeBucket.Large;
		}

		public static IEnumerable<CollectionEntry> EntriesInBucket( CollectionCategory cat, SizeBucket bucket )
		{
			return cat.Entries.Where( e => ClassifyBucket( e ) == bucket );
		}
	}
}
