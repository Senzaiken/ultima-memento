using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Misc;
using Server.Regions;

namespace Server.Engines.Instancing
{
	public partial class DungeonInstanceType
	{
		private const int FunctionalItemMargin = 8;

		private static readonly string[] ExcludedDungeons = new string[]
		{
			"Stonegate Castle",
			"the Halls of Undermountain",
			"the Ice Fiend Lair"
		};

		private List<DungeonInstanceDefinition> m_Definitions;

		// EVERY LISTED DUNGEON REGION, SORTED BY MAP THEN NAME
		public List<DungeonInstanceDefinition> Definitions
		{
			get
			{
				if ( m_Definitions != null )
					return m_Definitions;

				HashSet<string> listed = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
				for ( int i = 1; i <= 85; i++ )
				{
					string world, location;
					Map placer;
					int x, y;
					string dungeon = Worlds.GetDungeonListing( i, out world, out location, out placer, out x, out y );

					if ( !String.IsNullOrEmpty( dungeon ) )
						listed.Add( ListingName( dungeon ) );
				}

				foreach ( string excluded in ExcludedDungeons )
					listed.Remove( ListingName( excluded ) );

				CatalogIndex catalog = CatalogIndex.Build();
				List<DungeonInstanceDefinition> list = new List<DungeonInstanceDefinition>();

				foreach ( Region region in Region.Regions )
				{
					BaseRegion br = region as BaseRegion;
					if ( br != null && IsDungeonRegion( br ) && br.Name != null && br.Area.Length > 0 && !InstanceManager.IsInstanceMap( br.Map ) && listed.Contains( ListingName( br.Name ) ) )
						list.Add( new DungeonInstanceDefinition( br, catalog ) );
				}

				list.Sort( delegate( DungeonInstanceDefinition a, DungeonInstanceDefinition b )
				{
					int map = String.Compare( a.MapName, b.MapName, StringComparison.OrdinalIgnoreCase );
					return map != 0 ? map : String.Compare( a.Name, b.Name, StringComparison.OrdinalIgnoreCase );
				} );

				for ( int i = 0; i < list.Count; i++ )
					list[i].Index = i;

				return m_Definitions = list;
			}
		}

		private static string ListingName( string name )
		{
			name = name.Trim();
			return name.StartsWith( "the Dungeon of ", StringComparison.OrdinalIgnoreCase ) ? name.Substring( 4 ) : name;
		}

		private static bool IsDungeonRegion( BaseRegion region )
		{
			return region is DungeonRegion || region is BardDungeonRegion || region is CaveRegion || region is DeadRegion;
		}

		public DungeonInstanceDefinition FindDefinition( string value )
		{
			if ( String.IsNullOrEmpty( value ) )
				return null;

			List<DungeonInstanceDefinition> defs = Definitions;
			value = value.Trim();

			int index;
			if ( Int32.TryParse( value, out index ) )
				return index >= 0 && index < defs.Count ? defs[index] : null;

			DungeonInstanceDefinition match = defs.Find( delegate( DungeonInstanceDefinition def )
			{
				return String.Equals( def.Name, value, StringComparison.OrdinalIgnoreCase ) || String.Equals( def.DisplayName, value, StringComparison.OrdinalIgnoreCase );
			} );

			return match ?? defs.Find( delegate( DungeonInstanceDefinition def ) { return def.DisplayName.IndexOf( value, StringComparison.OrdinalIgnoreCase ) >= 0; } );
		}

		internal sealed class TeleportLink
		{
			public Map SourceMap;
			public Point3D Location;
			public Map DestMap;
			public Point3D Dest;
			public bool ViaDoor;
		}

		// ONE WORLD SCAN OF TELEPORTERS AND DUNGEON REGIONS, SHARED BY THE WHOLE CATALOG BUILD
		internal sealed class CatalogIndex
		{
			private readonly Dictionary<Map, List<TeleportLink>> m_LinksBySourceMap = new Dictionary<Map, List<TeleportLink>>();
			private readonly List<TeleportLink> m_AllLinks = new List<TeleportLink>();
			private readonly Dictionary<Map, List<BaseRegion>> m_RegionsByMap = new Dictionary<Map, List<BaseRegion>>();
			private readonly Dictionary<Map, List<BaseRegion>> m_ChambersByMap = new Dictionary<Map, List<BaseRegion>>();

			public List<TeleportLink> AllLinks { get { return m_AllLinks; } }

			public static CatalogIndex Build()
			{
				CatalogIndex index = new CatalogIndex();

				foreach ( Item item in World.Items.Values )
				{
					Map destinationMap;
					Point3D destination;

					if ( item.Parent == null && item.Map != null && item.Map != Map.Internal && !InstanceManager.IsInstanceMap( item.Map )
						&& TryGetTeleportDestination( item, out destinationMap, out destination ) && destination != Point3D.Zero )
						index.AddLink( item.Map, item.Location, destinationMap, destination, item is ThruDoor );
				}

				foreach ( Map map in CloneBases )
				{
					foreach ( DecorationTeleporter tp in GetDecorationTeleporters( map ) )
						index.AddLink( map, tp.Location, tp.DestMap, tp.Dest, false );
				}

				foreach ( Region region in Region.Regions )
				{
					BaseRegion br = region as BaseRegion;
					if ( br == null || br.Map == null || br.Map == Map.Internal || br.Area.Length == 0 || InstanceManager.IsInstanceMap( br.Map ) )
						continue;

					if ( IsDungeonRegion( br ) )
						Add( index.m_RegionsByMap, br.Map, br );
					else if ( br is PublicRegion )
						Add( index.m_ChambersByMap, br.Map, br );
				}

				return index;
			}

			private static void Add<T>( Dictionary<Map, List<T>> byMap, Map map, T value )
			{
				List<T> list;
				if ( !byMap.TryGetValue( map, out list ) )
					byMap[map] = list = new List<T>();

				list.Add( value );
			}

			private void AddLink( Map sourceMap, Point3D location, Map destinationMap, Point3D destination, bool viaDoor )
			{
				TeleportLink link = new TeleportLink();
				link.SourceMap = sourceMap;
				link.Location = location;
				link.DestMap = ResolveDestinationMap( sourceMap, destinationMap );
				link.Dest = destination;
				link.ViaDoor = viaDoor;

				Add( m_LinksBySourceMap, sourceMap, link );
				m_AllLinks.Add( link );
			}

			public List<TeleportLink> LinksFrom( Map map )
			{
				List<TeleportLink> list;
				return m_LinksBySourceMap.TryGetValue( map, out list ) ? list : new List<TeleportLink>();
			}

			// A MAGIC DOOR MAY ALSO LEAD INTO A PUBLIC CHAMBER: A QUEST OR GUARDIAN ROOM
			public BaseRegion FindRegion( Map map, Point3D p, bool viaDoor )
			{
				BaseRegion best = FindIn( m_RegionsByMap, map, p );
				return best == null && viaDoor ? FindIn( m_ChambersByMap, map, p ) : best;
			}

			private static BaseRegion FindIn( Dictionary<Map, List<BaseRegion>> byMap, Map map, Point3D p )
			{
				List<BaseRegion> list;
				BaseRegion best = null;

				if ( byMap.TryGetValue( map, out list ) )
				{
					foreach ( BaseRegion region in list )
					{
						if ( region.Contains( p ) && ( best == null || region.Priority > best.Priority ) )
							best = region;
					}
				}

				return best;
			}
		}

		public sealed class DungeonInstanceDefinition
		{
			private readonly BaseRegion[] m_ConnectedRegions;
			private readonly Rectangle3D[][] m_RegionAreas;
			private readonly Rectangle3D[] m_Area;

			public int Index;
			public readonly string Name;
			public readonly string DisplayName;
			public readonly Map Map;
			public readonly string MapName;
			public readonly Point3D Landing;
			public readonly bool WalkOutExits;
			public readonly int SpawnCount;

			public BaseRegion[] ConnectedRegions { get { return m_ConnectedRegions; } }
			public Rectangle3D[] Area { get { return m_Area; } }

			internal DungeonInstanceDefinition( BaseRegion region, CatalogIndex catalog )
			{
				Name = region.Name;
				Map = region.Map;
				MapName = Map.Name;
				DisplayName = String.Format( "{0}: {1}", MapName, Name );

				List<BaseRegion> connected;
				List<List<Rectangle3D>> regionAreas;
				BuildCloneArea( region, catalog, out connected, out regionAreas );

				m_ConnectedRegions = connected.ToArray();
				m_RegionAreas = new Rectangle3D[regionAreas.Count][];
				List<Rectangle3D> all = new List<Rectangle3D>();

				for ( int i = 0; i < regionAreas.Count; i++ )
				{
					m_RegionAreas[i] = regionAreas[i].ToArray();
					all.AddRange( regionAreas[i] );
				}

				m_Area = all.ToArray();
				Landing = GetLandingFor( region, catalog, m_Area );
				WalkOutExits = !HasTeleporterExit( catalog, Map, m_Area );

				foreach ( BaseRegion r in m_ConnectedRegions )
				{
					if ( r.Spawns != null )
						SpawnCount += r.Spawns.Length;
				}
			}

			// THE REGION'S RECTANGLES PLUS PADS FOR ITS TELEPORTERS JUST OUTSIDE THEM
			public Rectangle3D[] GetRegionArea( int index )
			{
				return (Rectangle3D[])m_RegionAreas[index].Clone();
			}

			public bool Contains( Point3D loc )
			{
				return Contains( m_Area, loc, 0 );
			}

			public bool ContainsFunctionalItem( Point3D loc )
			{
				return Contains( m_Area, loc, FunctionalItemMargin );
			}

			public bool TryGetRandomStandable( Map map, out Point3D p )
			{
				for ( int attempt = 0; attempt < 100; attempt++ )
				{
					Rectangle3D rect = m_Area[Utility.Random( m_Area.Length )];
					int x = rect.Start.X + Utility.Random( Math.Max( 1, rect.Width ) );
					int y = rect.Start.Y + Utility.Random( Math.Max( 1, rect.Height ) );
					int z;

					if ( FindStandableZ( map, x, y, rect.Start.Z, out z ) && Contains( new Point3D( x, y, z ) ) && !InstanceManager.IsInstanceVoid( map, new Point3D( x, y, z ) ) )
					{
						p = new Point3D( x, y, z );
						return true;
					}
				}

				p = Point3D.Zero;
				return false;
			}

			// THE ROOT REGION PLUS THE DUNGEON REGIONS ITS TELEPORTERS LEAD INTO; ANY OTHER DESTINATION IS AN EXIT
			private static void BuildCloneArea( BaseRegion root, CatalogIndex catalog, out List<BaseRegion> connected, out List<List<Rectangle3D>> regionAreas )
			{
				connected = new List<BaseRegion>();
				regionAreas = new List<List<Rectangle3D>>();
				List<Rectangle3D> all = new List<Rectangle3D>( root.Area );

				connected.Add( root );
				regionAreas.Add( new List<Rectangle3D>( root.Area ) );

				List<TeleportLink> links = catalog.LinksFrom( root.Map );
				bool changed = true;

				for ( int pass = 0; changed && pass < 16; pass++ )
				{
					changed = false;

					foreach ( TeleportLink link in links )
					{
						if ( !Object.ReferenceEquals( link.DestMap, root.Map ) || !Contains( all, link.Location, FunctionalItemMargin ) )
							continue;

						BaseRegion destination = catalog.FindRegion( root.Map, link.Dest, link.ViaDoor );
						if ( destination == null || connected.Contains( destination ) || ( destination.Priority < root.Priority && AreasIntersect( root.Area, destination.Area ) ) )
							continue;

						connected.Add( destination );
						regionAreas.Add( new List<Rectangle3D>( destination.Area ) );
						all.AddRange( destination.Area );
						changed = true;
					}
				}

				// A TELEPORTER INTO THE DUNGEON JUST OUTSIDE ITS RECTANGLES IS A STAIR THE REGION DATA MISSED
				foreach ( TeleportLink link in links )
				{
					if ( Contains( all, link.Location, 0 ) || !Contains( all, link.Location, FunctionalItemMargin ) )
						continue;

					if ( !Object.ReferenceEquals( link.DestMap, root.Map ) || !Contains( all, link.Dest, 0 ) )
						continue;

					Rectangle3D pad = new Rectangle3D( new Point3D( link.Location.X - 1, link.Location.Y - 1, Region.MinZ ), new Point3D( link.Location.X + 2, link.Location.Y + 2, Region.MaxZ ) );
					regionAreas[NearestRegion( regionAreas, link.Location )].Add( pad );
				}
			}

			private static bool HasTeleporterExit( CatalogIndex catalog, Map map, Rectangle3D[] area )
			{
				foreach ( TeleportLink link in catalog.LinksFrom( map ) )
				{
					if ( Contains( area, link.Location, 0 ) && ( !Object.ReferenceEquals( link.DestMap, map ) || !Contains( area, link.Dest, 0 ) ) )
						return true;
				}

				return false;
			}

			private static int NearestRegion( List<List<Rectangle3D>> regionAreas, Point3D p )
			{
				int best = 0;
				int bestDistance = int.MaxValue;

				for ( int i = 0; i < regionAreas.Count; i++ )
				{
					foreach ( Rectangle3D r in regionAreas[i] )
					{
						int dx = Math.Max( 0, Math.Max( r.Start.X - p.X, p.X - ( r.End.X - 1 ) ) );
						int dy = Math.Max( 0, Math.Max( r.Start.Y - p.Y, p.Y - ( r.End.Y - 1 ) ) );

						if ( dx + dy < bestDistance )
						{
							bestDistance = dx + dy;
							best = i;
						}
					}
				}

				return best;
			}

			// DUNGEONS NO TELEPORTER LEADS INTO: WHERE THE GAME'S OWN PORTALS AND MAPS BRING PLAYERS IN
			private static readonly Dictionary<string, Point3D> KnownEntrances = new Dictionary<string, Point3D>
			{
				{ "the Azure Castle", new Point3D( 231, 3650, 25 ) },
				{ "the City of the Dead", new Point3D( 5815, 3260, 0 ) }
			};

			// WHERE PLAYERS REALLY COME IN, ELSE THE GO LOCATION, ANY INSIDE DESTINATION OR ANY STANDABLE TILE
			private static Point3D GetLandingFor( BaseRegion region, CatalogIndex catalog, Rectangle3D[] area )
			{
				Map map = region.Map;
				Point3D standable;
				Point3D known;

				if ( KnownEntrances.TryGetValue( region.Name, out known ) && TryStandable( map, area, known, out standable ) )
					return standable;

				// THE MAIN WAY IN FROM OUTSIDE, ELSE THE RECORDED ENTRANCE, ELSE THE DOORWAY FROM THE DUNGEON THIS ONE IS REACHED THROUGH
				if ( TryBestEntrance( catalog, map, area, delegate( TeleportLink link ) { return region.Contains( link.Dest ) && !( Object.ReferenceEquals( link.SourceMap, map ) && Contains( area, link.Location, 0 ) ); }, out standable ) )
					return standable;

				DungeonRegion dungeon = region as DungeonRegion;
				if ( dungeon != null && Object.ReferenceEquals( dungeon.EntranceMap, map ) && TryStandable( map, area, dungeon.EntranceLocation, out standable ) )
					return standable;

				if ( TryBestEntrance( catalog, map, area, delegate( TeleportLink link ) { return region.Contains( link.Dest ) && !( Object.ReferenceEquals( link.SourceMap, map ) && region.Contains( link.Location ) ); }, out standable ) )
					return standable;

				if ( TryStandable( map, area, region.GoLocation, out standable ) )
					return standable;

				foreach ( TeleportLink link in catalog.AllLinks )
				{
					if ( Object.ReferenceEquals( link.DestMap, map ) && TryStandable( map, area, link.Dest, out standable ) )
						return standable;
				}

				foreach ( Rectangle3D rect in region.Area )
				{
					if ( FindStandableIn( map, area, rect, out standable ) )
						return standable;
				}

				return region.GoLocation;
			}

			// AMONG THE GIVEN LINKS INTO THIS MAP: ONE THAT STARTS ABOVE GROUND RATHER THAN IN ANOTHER DUNGEON OR A MAGIC CHAMBER, THEN THE WIDEST
			private static bool TryBestEntrance( CatalogIndex catalog, Map map, Rectangle3D[] area, Predicate<TeleportLink> into, out Point3D landing )
			{
				List<TeleportLink> entrances = catalog.AllLinks.FindAll( delegate( TeleportLink link ) { return Object.ReferenceEquals( link.DestMap, map ) && into( link ); } );
				int bestScore = -1;
				landing = Point3D.Zero;

				foreach ( TeleportLink link in entrances )
				{
					Point3D standable;
					if ( !TryStandable( map, area, link.Dest, out standable ) )
						continue;

					int score = ( StartsAboveGround( link ) ? 1000 : 0 ) + entrances.FindAll( delegate( TeleportLink other ) { return Utility.InRange( other.Dest, link.Dest, 2 ); } ).Count;
					if ( score > bestScore )
					{
						bestScore = score;
						landing = standable;
					}
				}

				return bestScore >= 0;
			}

			private static bool StartsAboveGround( TeleportLink link )
			{
				BaseRegion from = Region.Find( link.Location, link.SourceMap ) as BaseRegion;
				return from == null || !( IsDungeonRegion( from ) || from is PublicRegion );
			}

			private static bool TryStandable( Map map, Rectangle3D[] area, Point3D p, out Point3D standable )
			{
				standable = p;

				if ( p == Point3D.Zero || !Contains( area, p, 0 ) )
					return false;

				if ( map.CanFit( p.X, p.Y, p.Z, 16, false, false, true ) )
					return true;

				int z;
				if ( !FindStandableZ( map, p.X, p.Y, p.Z, out z ) )
					return false;

				standable = new Point3D( p.X, p.Y, z );
				return Contains( area, standable, 0 );
			}

			private static bool FindStandableIn( Map map, Rectangle3D[] area, Rectangle3D rect, out Point3D standable )
			{
				int cx = rect.Start.X + rect.Width / 2;
				int cy = rect.Start.Y + rect.Height / 2;
				int maxRadius = Math.Max( rect.Width, rect.Height ) / 2 + 1;

				for ( int radius = 0; radius <= maxRadius; radius++ )
				{
					for ( int x = cx - radius; x <= cx + radius; x++ )
					{
						for ( int y = cy - radius; y <= cy + radius; y++ )
						{
							int z;
							if ( Math.Max( Math.Abs( x - cx ), Math.Abs( y - cy ) ) == radius && FindStandableZ( map, x, y, rect.Start.Z, out z ) && Contains( area, new Point3D( x, y, z ), 0 ) )
							{
								standable = new Point3D( x, y, z );
								return true;
							}
						}
					}
				}

				standable = Point3D.Zero;
				return false;
			}

			private static bool FindStandableZ( Map map, int x, int y, int preferZ, out int z )
			{
				List<int> candidates = new List<int>();
				candidates.Add( map.GetAverageZ( x, y ) );

				foreach ( StaticTile tile in map.Tiles.GetStaticTiles( x, y, true ) )
				{
					ItemData data = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
					if ( data.Surface )
						candidates.Add( tile.Z + data.CalcHeight );
				}

				candidates.Sort( delegate( int a, int b ) { return Math.Abs( a - preferZ ).CompareTo( Math.Abs( b - preferZ ) ); } );

				foreach ( int candidate in candidates )
				{
					if ( map.CanFit( x, y, candidate, 16, false, false, true ) )
					{
						z = candidate;
						return true;
					}
				}

				z = preferZ;
				return false;
			}

			private static bool Contains( IList<Rectangle3D> areas, Point3D p, int margin )
			{
				foreach ( Rectangle3D r in areas )
				{
					if ( margin <= 0 ? r.Contains( p ) : p.X >= r.Start.X - margin && p.X < r.End.X + margin && p.Y >= r.Start.Y - margin && p.Y < r.End.Y + margin && p.Z >= r.Start.Z - margin && p.Z < r.End.Z + margin )
						return true;
				}

				return false;
			}

			private static bool AreasIntersect( Rectangle3D[] a, Rectangle3D[] b )
			{
				foreach ( Rectangle3D x in a )
				{
					foreach ( Rectangle3D y in b )
					{
						if ( x.Start.X < y.End.X && x.End.X > y.Start.X && x.Start.Y < y.End.Y && x.End.Y > y.Start.Y )
							return true;
					}
				}

				return false;
			}
		}
	}
}
