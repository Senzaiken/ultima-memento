using System;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Engines.Instancing
{
	// A GLOWING ORB ON A PEDESTAL BY A DUNGEON RUN'S ENTRANCE; DOUBLE-CLICK TO READ HOW FAR THE RUN HAS COME
	public class DungeonSigil : BaseAddon
	{
		private const int SearchRange = 10;
		private const int Clearance = 20;
		private const int DetourRange = 3;
		private const int PedestalHeight = 9;

		// SIDES DRAWN IN FRONT OF THE LANDING FIRST, SO DECORATION BEHIND IT NEVER HIDES THE STONE
		private static readonly Direction[] Sides = new Direction[]
		{
			Direction.East, Direction.South, Direction.Down, Direction.Right, Direction.Left, Direction.North, Direction.West, Direction.Up
		};

		// HAND-PICKED FLOOR FOR EACH DUNGEON'S SIGIL: AGAINST A WALL, IN VIEW OF THE LANDING, 5 TO 10 TILES AWAY WHERE THE ROOM ALLOWS
		private static readonly Dictionary<string, Point3D> Spots = new Dictionary<string, Point3D>
		{
			{ "Lodor: Argentrock Castle", new Point3D( 6098, 502, 0 ) },
			{ "Lodor: Dungeon Covetous", new Point3D( 5459, 1852, 0 ) },
			{ "Lodor: Dungeon Deceit", new Point3D( 5184, 632, 0 ) },
			{ "Lodor: Dungeon Despise", new Point3D( 5519, 672, 20 ) },
			{ "Lodor: Dungeon Destard", new Point3D( 5241, 1001, 0 ) },
			{ "Lodor: Dungeon Hythloth", new Point3D( 5904, 16, 44 ) },
			{ "Lodor: Dungeon Shame", new Point3D( 5394, 120, 0 ) },
			{ "Lodor: Dungeon Wrong", new Point3D( 5823, 622, 0 ) },
			{ "Lodor: Morgaelin's Inferno", new Point3D( 7001, 745, -30 ) },
			{ "Lodor: Terathan Keep", new Point3D( 5342, 1600, 40 ) },
			{ "Lodor: the Ancient Elven Mine", new Point3D( 6971, 2706, 0 ) },
			{ "Lodor: the Castle of Dracula", new Point3D( 6935, 1699, 0 ) },
			{ "Lodor: the City of Embers", new Point3D( 5783, 1501, 20 ) },
			{ "Lodor: the Crypts of Dracula", new Point3D( 5767, 2840, 1 ) },
			{ "Lodor: the Daemon's Crag", new Point3D( 5885, 2167, 0 ) },
			{ "Lodor: the Depths of Carthax Lake", new Point3D( 5762, 1767, 66 ) },
			{ "Lodor: the Frozen Hells", new Point3D( 5876, 241, -5 ) },
			{ "Lodor: the Hall of the Mountain King", new Point3D( 6833, 1892, 50 ) },
			{ "Lodor: the Lodoria Catacombs", new Point3D( 5578, 1919, 0 ) },
			{ "Lodor: the Sanctum of Saltmarsh", new Point3D( 6286, 1160, 0 ) },
			{ "Lodor: the Temple of Osirus", new Point3D( 6216, 3936, 40 ) },
			{ "Lodor: the Undersea Pass", new Point3D( 6437, 3968, 10 ) },
			{ "Lodor: the Vault of the Black Knight", new Point3D( 6622, 563, 0 ) },
			{ "Lodor: the Volcanic Cave", new Point3D( 6015, 3465, 0 ) },
			{ "Lodor: the Zealan Tombs", new Point3D( 5550, 1661, 96 ) },
			{ "SavagedEmpire: Dungeon Rock", new Point3D( 671, 2357, -27 ) },
			{ "SavagedEmpire: the Ancient Sky Ship", new Point3D( 1097, 3159, 20 ) },
			{ "SavagedEmpire: the Azure Castle", new Point3D( 231, 3644, 25 ) },
			{ "SavagedEmpire: the Blood Temple", new Point3D( 721, 2490, 12 ) },
			{ "SavagedEmpire: the Catacombs of Azerok", new Point3D( 733, 3365, 20 ) },
			{ "SavagedEmpire: the Corrupt Pass", new Point3D( 147, 2359, -29 ) },
			{ "SavagedEmpire: the Dungeon of the Lich King", new Point3D( 393, 2257, 1 ) },
			{ "SavagedEmpire: the Dungeon of the Mad Archmage", new Point3D( 593, 2060, -3 ) },
			{ "SavagedEmpire: the Forgotten Halls", new Point3D( 280, 3291, 20 ) },
			{ "SavagedEmpire: the Halls of Ogrimar", new Point3D( 815, 2377, -28 ) },
			{ "SavagedEmpire: the Ice Queen Fortress", new Point3D( 272, 2975, 44 ) },
			{ "SavagedEmpire: the Ratmen Mines", new Point3D( 1038, 2165, -3 ) },
			{ "SavagedEmpire: the Scurvy Reef", new Point3D( 480, 4047, 0 ) },
			{ "SavagedEmpire: the Storm Giant Lair", new Point3D( 596, 2765, -28 ) },
			{ "SavagedEmpire: the Tomb of Kazibal", new Point3D( 546, 3317, 0 ) },
			{ "SavagedEmpire: the Tombs", new Point3D( 152, 2726, -23 ) },
			{ "SavagedEmpire: the Undersea Castle", new Point3D( 594, 2760, -28 ) },
			{ "SerpentIsland: Dungeon Ankh", new Point3D( 2038, 94, 0 ) },
			{ "SerpentIsland: Dungeon Bane", new Point3D( 1945, 96, 0 ) },
			{ "SerpentIsland: Dungeon Hate", new Point3D( 2210, 417, 0 ) },
			{ "SerpentIsland: Dungeon Scorn", new Point3D( 2230, 686, 0 ) },
			{ "SerpentIsland: Dungeon Torment", new Point3D( 1973, 696, 0 ) },
			{ "SerpentIsland: Dungeon Vile", new Point3D( 2335, 394, 0 ) },
			{ "SerpentIsland: Dungeon Wicked", new Point3D( 2192, 55, 0 ) },
			{ "SerpentIsland: Dungeon Wrath", new Point3D( 2355, 679, 0 ) },
			{ "SerpentIsland: the Ancient Prison", new Point3D( 1930, 432, 0 ) },
			{ "SerpentIsland: the Cave of Fire", new Point3D( 2081, 793, 2 ) },
			{ "SerpentIsland: the Cave of Souls", new Point3D( 2524, 60, 2 ) },
			{ "SerpentIsland: the Flooded Temple", new Point3D( 2505, 725, 2 ) },
			{ "SerpentIsland: the Gargoyle Crypts", new Point3D( 2100, 361, 0 ) },
			{ "SerpentIsland: the Serpent Sanctum", new Point3D( 2518, 419, 0 ) },
			{ "SerpentIsland: the Tomb of the Fallen Wizard", new Point3D( 2353, 101, 0 ) },
			{ "Sosaria: Dardin's Pit", new Point3D( 5696, 44, 0 ) },
			{ "Sosaria: Dungeon Clues", new Point3D( 5312, 2273, 0 ) },
			{ "Sosaria: Dungeon Doom", new Point3D( 5319, 71, 0 ) },
			{ "Sosaria: Dungeon Exodus", new Point3D( 5927, 588, 0 ) },
			{ "Sosaria: Vordo's Castle", new Point3D( 6936, 2878, 50 ) },
			{ "Sosaria: Vordo's Dungeon", new Point3D( 6276, 166, 0 ) },
			{ "Sosaria: the Ancient Pyramid", new Point3D( 5350, 883, 0 ) },
			{ "Sosaria: the Cave of Banished Mages", new Point3D( 317, 3833, 0 ) },
			{ "Sosaria: the Cave of the Zuluu", new Point3D( 6296, 3334, -1 ) },
			{ "Sosaria: the Caverns of Poseidon", new Point3D( 5199, 2112, 40 ) },
			{ "Sosaria: the City of the Dead", new Point3D( 5817, 3254, 0 ) },
			{ "Sosaria: the Crypts of Kuldar", new Point3D( 6539, 2950, 45 ) },
			{ "Sosaria: the Dragon's Maw", new Point3D( 4747, 3853, 0 ) },
			{ "Sosaria: the Dungeon of Time Awaits", new Point3D( 5629, 593, 0 ) },
			{ "Sosaria: the Fires of Hell", new Point3D( 6031, 2040, 20 ) },
			{ "Sosaria: the Kuldara Sewers", new Point3D( 6211, 2797, 10 ) },
			{ "Sosaria: the Mausoleum", new Point3D( 4029, 3338, 20 ) },
			{ "Sosaria: the Mines of Morinia", new Point3D( 5900, 1221, 0 ) },
			{ "Sosaria: the Perinian Depths", new Point3D( 5945, 86, 0 ) },
			{ "Sosaria: the Pirate Cave", new Point3D( 5459, 1698, 0 ) },
			{ "Sosaria: the Ratmen Lair", new Point3D( 2770, 3745, 0 ) },
			{ "Sosaria: the Tower of Brass", new Point3D( 6948, 3564, 0 ) },
			{ "Sosaria: the Valley of Dark Druids", new Point3D( 6940, 241, 3 ) },
			{ "Underworld: the Glacial Scar", new Point3D( 1873, 1504, 20 ) },
			{ "Underworld: the Stygian Abyss", new Point3D( 1780, 1419, -2 ) }
		};

		[Constructable]
		public DungeonSigil()
		{
			AddComponent( new DungeonSigilPart( 0x1223, "a sigil pedestal" ), 0, 0, 0 );
			AddComponent( new DungeonSigilPart( 0x1869, "a sigil stone" ), 0, 0, PedestalHeight );

			AddonComponent glow = new AddonComponent( 0x1647 );
			glow.Light = LightType.Circle225;
			AddComponent( glow, 0, 0, PedestalHeight );
		}

		public DungeonSigil( Serial serial ) : base( serial )
		{
		}

		public static void Place( Map map, DungeonInstanceType.DungeonInstanceDefinition definition )
		{
			Point3D floor;
			if ( !Spots.TryGetValue( definition.DisplayName, out floor ) )
				floor = FindFloor( map, definition );

			new DungeonSigil().MoveToWorld( floor, map );
		}

		// FOR A DUNGEON WITH NO PICKED SPOT: THE NEAREST CLEAR FLOOR A PLAYER CAN WALK TO FROM THE LANDING, NEVER A CHOKE POINT
		private static Point3D FindFloor( Map map, DungeonInstanceType.DungeonInstanceDefinition definition )
		{
			Point3D landing = definition.Landing;
			Point3D fallback = landing;
			DungeonSigil test = new DungeonSigil();
			Mobile probe = new Mobile();

			try
			{
				foreach ( Point3D p in Reachable( map, probe, definition, landing, SearchRange ) )
				{
					if ( p == landing || !IsClearFloor( map, p ) )
						continue;

					List<Point3D> around = Reachable( map, probe, definition, p, 1 );
					around.Remove( p );

					test.MoveToWorld( p, map );
					bool cuts = CutsOff( map, probe, definition, around );
					test.Internalize();

					if ( !cuts )
						return p;

					if ( fallback == landing )
						fallback = p;
				}

				return fallback;
			}
			finally
			{
				test.Delete();
				probe.Delete();
			}
		}

		// EVERY TILE A PLAYER CAN WALK TO FROM START, NEAREST FIRST
		private static List<Point3D> Reachable( Map map, Mobile probe, DungeonInstanceType.DungeonInstanceDefinition definition, Point3D start, int range )
		{
			List<Point3D> found = new List<Point3D>();
			HashSet<Point2D> seen = new HashSet<Point2D>();
			found.Add( start );
			seen.Add( new Point2D( start.X, start.Y ) );

			for ( int i = 0; i < found.Count; i++ )
			{
				foreach ( Direction d in Sides )
				{
					Point3D next;
					if ( TryStep( map, probe, definition, found[i], d, out next ) && Utility.InRange( start, next, range ) && seen.Add( new Point2D( next.X, next.Y ) ) )
						found.Add( next );
				}
			}

			return found;
		}

		private static bool TryStep( Map map, Mobile probe, DungeonInstanceType.DungeonInstanceDefinition definition, Point3D from, Direction d, out Point3D to )
		{
			int x = from.X, y = from.Y, z;
			Server.Movement.Movement.Offset( d, ref x, ref y );
			to = new Point3D( x, y, from.Z );

			if ( !definition.Contains( to ) || InstanceManager.IsInstanceVoid( map, to ) || !Server.Movement.Movement.CheckMovement( probe, map, from, d, out z ) )
				return false;

			to = new Point3D( x, y, z );
			return true;
		}

		// NO ITEMS, STAIRS OR DECORATION WHERE THE STONE WOULD STAND; WHAT IS BURIED BELOW OR BUILT ABOVE IT DOES NOT MATTER
		private static bool IsClearFloor( Map map, Point3D p )
		{
			foreach ( StaticTile tile in map.Tiles.GetStaticTiles( p.X, p.Y, true ) )
			{
				ItemData id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
				int top = tile.Z + id.CalcHeight;

				if ( top < p.Z || tile.Z >= p.Z + Clearance )
					continue;

				if ( top > p.Z || !id.Surface || id.Bridge )
					return false;
			}

			IPooledEnumerable eable = map.GetItemsInRange( p, 0 );
			bool clear = true;
			foreach ( Item item in eable )
			{
				clear = false;
				break;
			}
			eable.Free();

			return clear;
		}

		// WITH THE PEDESTAL IN PLACE, THE TILES THAT SURROUNDED IT MUST STILL REACH EACH OTHER, BOTH WAYS, BY A SHORT DETOUR
		private static bool CutsOff( Map map, Mobile probe, DungeonInstanceType.DungeonInstanceDefinition definition, List<Point3D> around )
		{
			foreach ( Point3D from in around )
			{
				List<Point3D> reached = Reachable( map, probe, definition, from, DetourRange );
				foreach ( Point3D to in around )
				{
					if ( !reached.Contains( to ) )
						return true;
				}
			}

			return false;
		}

		public override void OnComponentUsed( AddonComponent c, Mobile from )
		{
			if ( !from.InRange( c.GetWorldLocation(), 3 ) )
				from.SendLocalizedMessage( 500446 ); // That is too far away.
			else if ( !DungeonInstanceType.Instance.SendProgress( from ) )
				from.SendMessage( "The sigil is dark." );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public class DungeonSigilPart : AddonComponent
	{
		public DungeonSigilPart( int itemID, string name ) : base( itemID )
		{
			Name = name;
		}

		public DungeonSigilPart( Serial serial ) : base( serial )
		{
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			list.Add( 1070722, "Double-click to read the dungeon's progress" );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
