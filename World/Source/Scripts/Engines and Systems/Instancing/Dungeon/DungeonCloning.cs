using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server;
using Server.Commands;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Instancing
{
	public partial class DungeonInstanceType
	{
		private int m_NextRuntimeSpawnId = -100000000;

		private List<BaseRegion> CopyRegions( DungeonInstanceDefinition definition, Map map, List<SpawnEntry> spawns )
		{
			List<BaseRegion> regions = new List<BaseRegion>();

			for ( int i = 0; i < definition.ConnectedRegions.Length; i++ )
			{
				BaseRegion source = definition.ConnectedRegions[i];
				BaseRegion copy = CopyRegion( source, map, definition.GetRegionArea( i ) );
				if ( copy == null )
					continue;

				copy.Register();

				if ( spawns != null )
					copy.Spawns = CopySpawns( source, copy, spawns );

				regions.Add( copy );
			}

			return regions;
		}

		// SAME TYPE, NAME AND SETTINGS AS THE REAL REGION, SO LIGHT, MUSIC AND REGION CHECKS MATCH
		private static BaseRegion CopyRegion( BaseRegion source, Map map, Rectangle3D[] area )
		{
			BaseRegion copy;
			DungeonRegion dungeon = source as DungeonRegion;

			if ( dungeon != null )
			{
				DungeonRegion d = new DungeonRegion( source.Name, map, source.Priority, area );
				d.EntranceLocation = dungeon.EntranceLocation;
				d.EntranceMap = dungeon.EntranceMap;
				copy = d;
			}
			else if ( source is CaveRegion )
				copy = new CaveRegion( source.Name, map, source.Priority, area );
			else if ( source is DeadRegion )
				copy = new DeadRegion( source.Name, map, source.Priority, area );
			else if ( source is BardDungeonRegion )
				copy = new BardDungeonRegion( source.Name, map, source.Priority, area );
			else if ( source is PublicRegion )
				copy = new PublicRegion( source.Name, map, source.Priority, area );
			else
				return null;

			copy.RuneName = source.RuneName;
			copy.GoLocation = source.GoLocation;
			copy.Music = source.Music;
			copy.SpawnZLevel = source.SpawnZLevel;
			copy.NoLogoutDelay = source.NoLogoutDelay;
			copy.ExcludeFromParentSpawns = source.ExcludeFromParentSpawns;
			return copy;
		}

		private SpawnEntry[] CopySpawns( BaseRegion source, BaseRegion copy, List<SpawnEntry> all )
		{
			if ( source.Spawns == null )
				return null;

			List<SpawnEntry> list = new List<SpawnEntry>();

			foreach ( SpawnEntry e in source.Spawns )
			{
				if ( e == null || e.Definition == null || e.Max <= 0 || SpawnsTreasure( e.Definition ) )
					continue;

				while ( SpawnEntry.Table.Contains( m_NextRuntimeSpawnId ) )
					m_NextRuntimeSpawnId--;

				SpawnEntry entry = new SpawnEntry( m_NextRuntimeSpawnId--, copy, e.HomeLocation, e.HomeRange, e.Direction, e.Definition, e.Max, e.MinSpawnTime, e.MaxSpawnTime );
				list.Add( entry );
				all.Add( entry );
			}

			return list.ToArray();
		}

		// INSTANCES HOLD NO CHESTS, TRAP-DOOR STASHES OR TREASURE HOARDS
		private static bool IsTreasureType( Type type )
		{
			return typeof( Container ).IsAssignableFrom( type ) || typeof( HiddenChest ).IsAssignableFrom( type ) || typeof( DungeonChestSpawner ).IsAssignableFrom( type )
				|| typeof( HoardTile ).IsAssignableFrom( type ) || typeof( HoardPiles ).IsAssignableFrom( type );
		}

		private static bool SpawnsTreasure( SpawnDefinition definition )
		{
			SpawnType single = definition as SpawnType;
			if ( single != null )
				return single.Type != null && IsTreasureType( single.Type );

			return definition.CanSpawn( typeof( HiddenChest ), typeof( DungeonChest ), typeof( DungeonChestSpawner ), typeof( HoardTile ), typeof( HoardPiles ) );
		}

		// PORTALS THAT SEND PLAYERS TO ANOTHER MAP OR A RANDOM SPOT IN THE REAL WORLD
		private static readonly string[] UncopiedPortalTypes = new string[]
		{
			"MagicMirror", "RandomExit", "PrisonExit", "SkullGate", "SerpentPillars", "GateMoon",
			"RaceTeleporter", "BasementDoor", "ElementalExit"
		};

		// SETTING MAP OR LOCATION WOULD PUT THE COPY ON THE REAL MAP
		private static readonly string[] UncopiedProperties = new string[]
		{
			"Map", "Location", "X", "Y", "Z", "Parent", "Spawner"
		};

		// EVERYTHING FIXED IN PLACE, WITH ITEM REFERENCES REMAPPED TO THE COPIES
		private static void CloneWorldContent( Map map, DungeonInstanceDefinition definition )
		{
			Dictionary<Item, Item> clones = new Dictionary<Item, Item>();
			List<Item> sources = new List<Item>();

			foreach ( Item item in World.Items.Values )
			{
				if ( item.Parent == null && Object.ReferenceEquals( item.Map, definition.Map ) && ShouldCloneWorldItem( item, definition ) )
					sources.Add( item );
			}

			foreach ( Item source in sources )
			{
				Item clone = CloneWorldItem( source, definition );
				if ( clone != null )
					clones[source] = clone;
			}

			foreach ( KeyValuePair<Item, Item> kvp in clones )
			{
				RemapItemReferences( kvp.Value, clones );
				Item clone = ApplyDestinationRule( kvp.Value, map, definition );
				clone.MoveToWorld( kvp.Key.Location, map );

				if ( clone is PremiumSpawner )
					((PremiumSpawner)clone).Respawn();
				else if ( clone is Spawner )
					((Spawner)clone).Respawn();
			}

			CloneDecoratedTeleporters( map, definition );
		}

		private static bool ShouldCloneWorldItem( Item item, DungeonInstanceDefinition definition )
		{
			if ( IsTreasureType( item.GetType() ) )
				return false;

			if ( item is Spawner || item is PremiumSpawner )
				return definition.Contains( item.Location );

			if ( item is BaseDoor || IsTeleportItem( item ) )
				return definition.ContainsFunctionalItem( item.Location );

			if ( !definition.Contains( item.Location ) || item.Movable || item.Spawner != null )
				return false;

			if ( item is Corpse || item is BaseMulti || item is DungeonInstanceGate || item is DungeonInstanceExitTeleporter || Array.IndexOf( UncopiedPortalTypes, item.GetType().Name ) >= 0 )
				return false;

			// COMPONENTS COME WITH THEIR ADDON WHEN IT CAN BE COPIED
			AddonComponent component = item as AddonComponent;
			return component == null || component.Addon == null || !IsCloneableAddon( component.Addon, definition );
		}

		private static bool IsTeleportItem( Item item )
		{
			return item is Teleporter || item is moongates || item is QuestTeleporter || item is ThruDoor;
		}

		private static bool IsCloneableAddon( BaseAddon addon, DungeonInstanceDefinition definition )
		{
			return Object.ReferenceEquals( addon.Map, definition.Map ) && addon.Parent == null && definition.Contains( addon.Location )
				&& addon.GetType().GetConstructor( Type.EmptyTypes ) != null;
		}

		private static Item CloneWorldItem( Item source, DungeonInstanceDefinition definition )
		{
			if ( source is BaseDoor )
				return CloneDoor( (BaseDoor)source );

			PremiumSpawner premium = source as PremiumSpawner;
			if ( premium != null )
			{
				PremiumSpawner clone = new PremiumSpawner( premium.Count, premium.CountA, premium.CountB, premium.CountC, premium.CountD, premium.CountE,
					premium.SpawnID, premium.MinDelay, premium.MaxDelay, premium.Team, premium.HomeRange, premium.WalkingRange,
					CopyStrings( premium.CreaturesName ), CopyStrings( premium.SubSpawnerA ), CopyStrings( premium.SubSpawnerB ),
					CopyStrings( premium.SubSpawnerC ), CopyStrings( premium.SubSpawnerD ), CopyStrings( premium.SubSpawnerE ) );

				clone.Group = premium.Group;
				clone.Running = false;
				return clone;
			}

			Spawner classic = source as Spawner;
			if ( classic != null )
			{
				Spawner clone = new Spawner( classic.Count, classic.MinDelay, classic.MaxDelay, classic.Team, classic.HomeRange, CopyStrings( classic.SpawnNames ) );
				clone.WalkingRange = classic.WalkingRange;
				clone.Group = classic.Group;
				clone.Running = false;
				return clone;
			}

			BaseAddon addon = source as BaseAddon;
			if ( addon != null )
				return IsCloneableAddon( addon, definition ) ? CloneItem( addon ) : null;

			Item copy = source is AddonComponent ? null : CloneItem( source );
			return copy ?? CloneAsStatic( source );
		}

		private static Item CloneItem( Item source )
		{
			try
			{
				Item item = Activator.CreateInstance( source.GetType() ) as Item;
				if ( item != null )
					CopyItemProperties( item, source );

				return item;
			}
			catch
			{
				return null;
			}
		}

		private static Item CloneDoor( BaseDoor source )
		{
			Item clone = CloneItem( source );
			if ( clone != null )
				return clone;

			DoorFacing facing = DoorFacing.WestCW;
			foreach ( DoorFacing f in Enum.GetValues( typeof( DoorFacing ) ) )
			{
				if ( BaseDoor.GetOffset( f ) == source.Offset )
				{
					facing = f;
					break;
				}
			}

			try
			{
				BaseDoor door = Activator.CreateInstance( source.GetType(), facing ) as BaseDoor;
				if ( door != null )
					CopyItemProperties( door, source );

				return door;
			}
			catch
			{
				return null;
			}
		}

		private static void CopyItemProperties( Item dest, Item src )
		{
			foreach ( PropertyInfo prop in src.GetType().GetProperties() )
			{
				if ( !prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length > 0 || Array.IndexOf( UncopiedProperties, prop.Name ) >= 0 )
					continue;

				try
				{
					prop.SetValue( dest, prop.GetValue( src, null ), null );
				}
				catch
				{
				}
			}
		}

		private static void RemapItemReferences( Item clone, Dictionary<Item, Item> clones )
		{
			foreach ( PropertyInfo prop in clone.GetType().GetProperties() )
			{
				if ( !prop.CanRead || !prop.CanWrite || !typeof( Item ).IsAssignableFrom( prop.PropertyType ) || prop.GetIndexParameters().Length > 0 )
					continue;

				try
				{
					Item value = prop.GetValue( clone, null ) as Item;
					Item mapped;

					if ( value != null && clones.TryGetValue( value, out mapped ) && prop.PropertyType.IsInstanceOfType( mapped ) )
						prop.SetValue( clone, mapped, null );
				}
				catch
				{
				}
			}
		}

		private static Item CloneAsStatic( Item source )
		{
			Static copy = new Static( source.ItemID );
			copy.Hue = source.Hue;
			copy.Name = source.Name;
			copy.Light = source.Light;
			copy.Visible = source.Visible;
			return copy;
		}

		private static List<string> CopyStrings( List<string> source )
		{
			return source != null ? new List<string>( source ) : new List<string>();
		}

		internal static bool TryGetTeleportDestination( Item item, out Map map, out Point3D p )
		{
			if ( item is Teleporter )
			{
				map = ((Teleporter)item).MapDest;
				p = ((Teleporter)item).PointDest;
			}
			else if ( item is moongates )
			{
				map = ((moongates)item).MapDest;
				p = ((moongates)item).PointDest;
			}
			else if ( item is QuestTeleporter )
			{
				map = ((QuestTeleporter)item).TeleporterMapDest;
				p = ((QuestTeleporter)item).TeleporterPointDest;
			}
			else if ( item is ThruDoor )
			{
				map = ((ThruDoor)item).MapDest;
				p = ((ThruDoor)item).PointDest;
			}
			else
			{
				map = null;
				p = Point3D.Zero;
				return false;
			}

			return map != null || p != Point3D.Zero;
		}

		private static Map ResolveDestinationMap( Map sourceMap, Map destinationMap )
		{
			return destinationMap == null || destinationMap == Map.Internal ? sourceMap : destinationMap;
		}

		// INSIDE DESTINATIONS ARE RETARGETED TO THE INSTANCE; ANY OTHER BECOMES AN EXIT
		private static Item ApplyDestinationRule( Item clone, Map map, DungeonInstanceDefinition definition )
		{
			Map destinationMap;
			Point3D destination;

			if ( !TryGetTeleportDestination( clone, out destinationMap, out destination ) )
				return clone;

			if ( destination != Point3D.Zero && Object.ReferenceEquals( ResolveDestinationMap( definition.Map, destinationMap ), definition.Map ) && definition.Contains( destination ) )
			{
				if ( clone is Teleporter )
					((Teleporter)clone).MapDest = map;
				else if ( clone is moongates )
					((moongates)clone).MapDest = map;
				else if ( clone is QuestTeleporter )
					((QuestTeleporter)clone).TeleporterMapDest = map;
				else if ( clone is ThruDoor )
					((ThruDoor)clone).MapDest = map;

				return clone;
			}

			// A DOOR GRAPHIC IS IMPASSABLE, SO A MAGIC DOOR'S EXIT TAKES THE PLAIN EXIT TILE
			Item exit = clone is BaseDoor ? new DungeonInstanceExitTeleporter() : new DungeonInstanceExitTeleporter( clone.ItemID );
			clone.Delete();
			return exit;
		}

		private sealed class DecorationTeleporter
		{
			public DecorationList List;
			public Point3D Location;
			public Map DestMap;
			public Point3D Dest;
		}

		private static readonly Dictionary<Map, List<DecorationTeleporter>> m_DecorationTeleporters = new Dictionary<Map, List<DecorationTeleporter>>();

		private static List<DecorationTeleporter> GetDecorationTeleporters( Map map )
		{
			List<DecorationTeleporter> list;
			if ( m_DecorationTeleporters.TryGetValue( map, out list ) )
				return list;

			m_DecorationTeleporters[map] = list = new List<DecorationTeleporter>();

			string file = Object.ReferenceEquals( map, Map.IslesDread ) ? "IslesOfDread" : map.Name;
			string path = Path.Combine( Core.BaseDirectory, String.Format( "Data/Decoration/{0}.cfg", file ) );
			if ( !File.Exists( path ) )
				return list;

			foreach ( DecorationList decoration in DecorationList.ReadAll( path ) )
			{
				Item sample = decoration.Construct();
				if ( sample == null )
					continue;

				Map destMap;
				Point3D dest;
				bool teleports = TryGetTeleportDestination( sample, out destMap, out dest ) && dest != Point3D.Zero;
				sample.Delete();

				if ( !teleports )
					continue;

				foreach ( DecorationEntry entry in decoration.Entries )
				{
					DecorationTeleporter tp = new DecorationTeleporter();
					tp.List = decoration;
					tp.Location = entry.Location;
					tp.DestMap = destMap;
					tp.Dest = dest;
					list.Add( tp );
				}
			}

			return list;
		}

		private static void CloneDecoratedTeleporters( Map map, DungeonInstanceDefinition definition )
		{
			foreach ( DecorationTeleporter source in GetDecorationTeleporters( definition.Map ) )
			{
				if ( !definition.ContainsFunctionalItem( source.Location ) || HasTeleporterAt( map, source.Location ) )
					continue;

				Item clone = source.List.Construct();
				if ( clone != null )
					ApplyDestinationRule( clone, map, definition ).MoveToWorld( source.Location, map );
			}
		}

		private static bool HasTeleporterAt( Map map, Point3D p )
		{
			bool found = false;

			IPooledEnumerable eable = map.GetItemsInRange( p, 0 );
			foreach ( Item item in eable )
			{
				if ( item.Z == p.Z && ( IsTeleportItem( item ) || item is DungeonInstanceExitTeleporter ) )
					found = true;
			}
			eable.Free();

			return found;
		}
	}
}
