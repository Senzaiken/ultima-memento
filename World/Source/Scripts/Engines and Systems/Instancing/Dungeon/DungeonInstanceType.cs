using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Misc;
using Server.Engines.PartySystem;
using Server.Regions;
using Server.Timers;

namespace Server.Engines.Instancing
{
	// Transient, party-shared dungeon instances backed by real dungeon region data.
	// A configured entry item chooses the source dungeon and difficulty; this type
	// then assigns a clone map with matching terrain, registers a temporary copy of
	// the selected region, and clones that region's spawn entries into the instance.
	public class DungeonInstanceType : InstanceType
	{
		public static readonly DungeonInstanceType Instance = new DungeonInstanceType();

		public enum DungeonInstanceAvailability
		{
			Allowed,
			BadEntranceOrExit,
			Broken
		}

		private const int CoreBaseMapCount = 7;
		private const int ClonesPerBaseMap = 7;
		private const int FunctionalItemMargin = 8;
		private const int DungeonListingScanLimit = 85;
			private const string ReturnSavePath = "Saves/Instancing/DungeonReturns.bin";
		private const string InstanceSavePath = "Saves/Instancing/DungeonInstances.bin";

		// How long after boot a restored instance waits for its players to log back in.
		private static readonly TimeSpan RestartGrace = TimeSpan.FromMinutes( 15 );

			private static readonly Dictionary<int, List<DecorationTeleporterSource>> m_DecorationTeleportersByMap = new Dictionary<int, List<DecorationTeleporterSource>>();
		private static readonly string[] ExcludedListedDungeonNames = new string[]
		{
			"Stonegate Castle",
			"the Halls of Undermountain",
			"the Ice Fiend Lair"
		};

		// Dungeons taken out of instancing, by DisplayName (catalog indexes shift).
		private static readonly string[] BadEntranceOrExitDungeons = new string[]
		{
		};

		private static readonly string[] BrokenDungeons = new string[]
		{
		};

		private const string BadEntranceOrExitReason = "entrance location incorrect or exit tiles incorrect";
		private const string BrokenReason = "broken in general";
		public const int GoodDungeonGateHue = 0x455;
		public const int BadEntranceOrExitDungeonGateHue = 0x36;
		public const int BrokenDungeonGateHue = 0x25;
			private static readonly string[] EmptyStringArray = new string[0];

		private List<DungeonInstanceDefinition> m_Definitions;
		private readonly Dictionary<Serial, DungeonInstanceSettings> m_SettingsByOwner = new Dictionary<Serial, DungeonInstanceSettings>();
		private readonly Dictionary<int, DungeonInstanceSettings> m_SettingsByLiveMap = new Dictionary<int, DungeonInstanceSettings>();
		private readonly Dictionary<Serial, RuntimeDungeonState> m_Runtimes = new Dictionary<Serial, RuntimeDungeonState>();
		private readonly Dictionary<Serial, DungeonReturnInfo> m_ReturnsByPlayer = new Dictionary<Serial, DungeonReturnInfo>();
		private bool m_ReturnPersistenceConfigured;
		private int m_NextRuntimeSpawnId = -100000000;

		private DungeonInstanceType()
		{
		}

		public void Configure()
		{
			if ( m_ReturnPersistenceConfigured )
				return;

			m_ReturnPersistenceConfigured = true;
			EventSink.WorldLoad += new WorldLoadEventHandler( OnWorldLoad );
			EventSink.WorldSave += new WorldSaveEventHandler( OnWorldSave );
			EventSink.ServerStarted += new ServerStartedEventHandler( OnServerStarted );
		}

		private void OnServerStarted()
		{
			GetDefinitions();
		}

		private void OnWorldLoad()
		{
			m_Definitions = null;
			m_DecorationTeleportersByMap.Clear();
			LoadReturnRecords();

			// Saved instances come back on the maps they were saved on; anything else left on a pool map is cleared.
			HashSet<int> restored = LoadInstances();
			ClearPoolMaps( null, null, restored );
		}

		private void OnWorldSave( WorldSaveEventArgs e )
		{
			SaveReturnRecords();
			SaveInstances();
		}

		private void SaveInstances()
		{
			List<Instance> live = new List<Instance>();
			foreach ( Instance inst in LiveInstances )
			{
				DungeonInstanceSettings settings;
				if ( m_SettingsByOwner.TryGetValue( inst.OwnerSerial, out settings ) && settings != null && settings.Definition != null && m_Runtimes.ContainsKey( inst.OwnerSerial ) )
					live.Add( inst );
			}

			Persistence.Serialize(
				InstanceSavePath,
				delegate( GenericWriter writer )
				{
					writer.Write( (int) 2 ); // version

					writer.Write( (int) live.Count );
					for ( int i = 0; i < live.Count; i++ )
					{
						Instance inst = live[i];
						DungeonInstanceSettings settings = m_SettingsByOwner[inst.OwnerSerial];

						writer.Write( (int) inst.OwnerSerial );
						writer.Write( (int) inst.OwnerKind );
						writer.Write( (int) inst.LiveMapIndex );
						writer.Write( (string) settings.Definition.DisplayName );
						writer.Write( (int) settings.Difficulty );
						writer.Write( (Map) settings.ReturnMap );
						writer.Write( (Point3D) settings.ReturnPoint );
						writer.Write( (TimeSpan) settings.TimeLimit );

						writer.Write( (int) inst.Members.Count );
						for ( int j = 0; j < inst.Members.Count; j++ )
							writer.Write( (int) inst.Members[j] );

						writer.Write( (int) inst.Items.Count );
						for ( int j = 0; j < inst.Items.Count; j++ )
						{
							writer.Write( (Item) inst.Items[j].Item );
							writer.Write( (Point3D) inst.Items[j].Loc );
						}

						m_Runtimes[inst.OwnerSerial].Serialize( writer );
					}
				} );
		}

		// Returns the pool map indexes now held by restored instances.
		private HashSet<int> LoadInstances()
		{
			HashSet<int> restored = new HashSet<int>();
			List<SavedDungeonInstance> saved = new List<SavedDungeonInstance>();

			try
			{
				Persistence.Deserialize(
					InstanceSavePath,
					delegate( GenericReader reader )
					{
						if ( reader.End() )
							return;

						int version = reader.ReadInt();
						int count = reader.ReadInt();

						for ( int i = 0; i < count; i++ )
							saved.Add( SavedDungeonInstance.Read( reader, version ) );
					} );
			}
			catch ( Exception e )
			{
				Console.WriteLine( "[Instancing] Dungeon instances could not be loaded and will be cleared: {0}", e.Message );
				saved.Clear();
			}

			if ( saved.Count == 0 )
				return restored;

			Dictionary<int, List<Mobile>> creaturesByMap = new Dictionary<int, List<Mobile>>();
			foreach ( Mobile m in World.Mobiles.Values )
			{
				if ( m == null || m.Deleted || m.Player || !IsPoolMap( m.Map ) )
					continue;

				List<Mobile> list;
				if ( !creaturesByMap.TryGetValue( m.Map.MapIndex, out list ) )
					creaturesByMap[m.Map.MapIndex] = list = new List<Mobile>();

				list.Add( m );
			}

			for ( int i = 0; i < saved.Count; i++ )
			{
				if ( RestoreInstance( saved[i], creaturesByMap ) )
					restored.Add( saved[i].MapIndex );
			}

			Console.WriteLine( "[Instancing] Restored {0} of {1} saved dungeon instance(s).", restored.Count, saved.Count );
			return restored;
		}

		private bool RestoreInstance( SavedDungeonInstance saved, Dictionary<int, List<Mobile>> creaturesByMap )
		{
			DungeonInstanceDefinition definition = FindDefinitionByDisplayName( saved.DefinitionName );
			Map map = saved.MapIndex >= 0 && saved.MapIndex < Map.Maps.Length ? Map.Maps[saved.MapIndex] : null;

			if ( definition == null || !IsPoolMap( map ) || !IsCloneOf( map, definition.Map ) || IsMapIndexLive( saved.MapIndex ) || GetByKey( saved.Key ) != null )
				return false;

			Instance inst = GetOrCreateByKey( saved.Key, saved.OwnerKind );
			inst.Members.AddRange( saved.Members );
			inst.Items.AddRange( saved.Items );
			inst.LastTouched = DateTime.Now + RestartGrace - UnloadAfter;
			RestoreLive( inst, saved.MapIndex );

			DungeonInstanceSettings settings = new DungeonInstanceSettings( definition, saved.Difficulty, saved.ReturnMap, saved.ReturnPoint, saved.TimeLimit );
			m_SettingsByOwner[inst.OwnerSerial] = settings;
			m_SettingsByLiveMap[saved.MapIndex] = settings;

			List<BaseRegion> regions = new List<BaseRegion>();
			for ( int i = 0; i < definition.ConnectedRegions.Length; i++ )
			{
				BaseRegion copy = CopyRegion( definition.ConnectedRegions[i], map, definition.GetRegionArea( i ) );
				if ( copy == null )
					continue;

				copy.Register();
				regions.Add( copy );
			}

			RuntimeDungeonState state = new RuntimeDungeonState( settings, regions, map, definition.Area );

			// The saved creatures were scaled before the save.
			List<Mobile> creatures;
			if ( creaturesByMap.TryGetValue( saved.MapIndex, out creatures ) )
			{
				for ( int i = 0; i < creatures.Count; i++ )
					state.MarkScaled( creatures[i] );
			}

			state.RestoreProgress( saved.KilledPoints, saved.Cleared, saved.Monsters );
			state.RestoreBoss( saved.BossTypeName, saved.BossPhase, saved.Boss );
			state.RestoreClock( saved.TimeRemaining );
			m_Runtimes[inst.OwnerSerial] = state;
			state.StartScaler();
			return true;
		}

		private DungeonInstanceDefinition FindDefinitionByDisplayName( string displayName )
		{
			List<DungeonInstanceDefinition> defs = GetDefinitions();

			for ( int i = 0; i < defs.Count; i++ )
			{
				if ( String.Equals( defs[i].DisplayName, displayName, StringComparison.OrdinalIgnoreCase ) )
					return defs[i];
			}

			return null;
		}

		private void LoadReturnRecords()
		{
			m_ReturnsByPlayer.Clear();

			Persistence.Deserialize(
				ReturnSavePath,
				delegate( GenericReader reader )
				{
					if ( reader.End() )
						return;

					reader.ReadInt(); // version
					int count = reader.ReadInt();

					for ( int i = 0; i < count; i++ )
					{
						Serial player = (Serial)reader.ReadInt();
						Serial instanceKey = (Serial)reader.ReadInt();
						InstanceOwnerKind ownerKind = (InstanceOwnerKind)reader.ReadInt();
							Map returnMap = NormalizeExternalMap( reader.ReadMap() );
						Point3D returnPoint = reader.ReadPoint3D();
						DateTime lastTouched = reader.ReadDateTime();

						if ( returnMap == null )
							continue;

						m_ReturnsByPlayer[player] = new DungeonReturnInfo( instanceKey, ownerKind, returnMap, returnPoint, lastTouched );
					}
				} );

			PruneReturnRecords();
		}

		private void SaveReturnRecords()
		{
			PruneReturnRecords();

			Persistence.Serialize(
				ReturnSavePath,
				delegate( GenericWriter writer )
				{
					writer.Write( (int) 0 ); // version

					writer.Write( (int) m_ReturnsByPlayer.Count );
					foreach ( KeyValuePair<Serial, DungeonReturnInfo> kvp in m_ReturnsByPlayer )
					{
						DungeonReturnInfo info = kvp.Value;

						writer.Write( (int) kvp.Key );
						writer.Write( (int) info.InstanceKey );
						writer.Write( (int) info.OwnerKind );
						writer.Write( (Map) info.ReturnMap );
						writer.Write( (Point3D) info.ReturnPoint );
						writer.Write( (DateTime) info.LastTouched );
					}
				} );
		}

		private void PruneReturnRecords()
		{
			if ( m_ReturnsByPlayer.Count == 0 )
				return;

			List<Serial> remove = null;

			foreach ( KeyValuePair<Serial, DungeonReturnInfo> kvp in m_ReturnsByPlayer )
			{
				DungeonReturnInfo info = kvp.Value;
				if ( info == null || info.ReturnMap == null || World.FindMobile( kvp.Key ) == null )
				{
					if ( remove == null )
						remove = new List<Serial>();

					remove.Add( kvp.Key );
				}
			}

			if ( remove == null )
				return;

			for ( int i = 0; i < remove.Count; i++ )
				m_ReturnsByPlayer.Remove( remove[i] );
		}

		// Dungeon owns Map.Maps[] slots 72-120, clear of the sky pool (40-71) and
		// the reserved internal slot (127). The range is split into seven clones for
		// each core terrain map.
		public override string Key { get { return "Dungeon"; } }
		public override int PoolBaseIndex { get { return 72; } }
		public override int PoolSize { get { return CoreBaseMapCount * ClonesPerBaseMap; } }

		public override Map BaseMap { get { return Map.SerpentIsland; } }
		public override int MapWidth { get { return 2560; } }
		public override int MapHeight { get { return 2048; } }

		public override Point3D Landing { get { return new Point3D( 1974, 1977, 0 ); } }

		public override Map ExitMap { get { return Map.Sosaria; } }
		public override Point3D ExitPoint { get { return new Point3D( 3884, 2879, 0 ); } }

		public override string RegionName { get { return "Dungeon Instance"; } }

		// Transient: not saved, freed when empty.
		public override bool Persistent { get { return false; } }

		// Reclaim an emptied dungeon fairly quickly.
		public override TimeSpan UnloadAfter { get { return TimeSpan.FromMinutes( 5 ); } }

		// Dungeons register their own region copies; nothing else on a pool map is playable.
		public override bool UsesFootprintRegion { get { return false; } }

		// Walking off the edge leaves only dungeons with no exit teleporter; elsewhere it's a wall.
		public override void OnWalkIntoVoid( Mobile m, Map map )
		{
			if ( !m.Player )
				return;

			DungeonInstanceSettings settings = GetSettingsForMap( map );
			if ( settings == null || settings.Definition == null || !settings.Definition.WalkOutExits )
				return;

			Timer.DelayCall( TimeSpan.Zero, delegate
			{
				if ( !m.Deleted && Object.ReferenceEquals( m.Map, map ) )
					LeaveInstance( m );
			} );
		}

		// Monsters leave no corpse or loot here; their points go to the dungeon's total. Pets keep theirs.
		public override void OnCreatureDeath( BaseCreature bc, Container corpse )
		{
			if ( bc.Controlled || bc.ControlMaster != null || bc.IsBonded )
				return;

			RuntimeDungeonState state = GetRuntime( bc.Map );
			if ( state != null )
				state.CreditKill( bc );

			// Deferred: overrides still add loot to the corpse after this.
			if ( corpse != null )
			{
				Timer.DelayCall( TimeSpan.Zero, delegate
				{
					if ( !corpse.Deleted )
						corpse.Delete();
				} );
			}
		}

		private RuntimeDungeonState GetRuntime( Map map )
		{
			Instance inst = GetLiveInstance( map );
			RuntimeDungeonState state;
			return inst != null && m_Runtimes.TryGetValue( inst.OwnerSerial, out state ) ? state : null;
		}

		public override bool TryGetRandomPlayablePoint( Map map, out Point3D p )
		{
			DungeonInstanceSettings settings = GetSettingsForMap( map );
			if ( settings != null && settings.Definition != null && settings.Definition.TryGetRandomStandable( map, out p ) )
				return true;

			p = settings != null && settings.Definition != null ? settings.Definition.Landing : Point3D.Zero;
			return settings != null && settings.Definition != null;
		}

		// Drop return records of players who left by recall or gate, so no later exit uses an old gate.
		protected override void OnSweep()
		{
			List<Serial> stale = null;

			foreach ( KeyValuePair<Serial, DungeonReturnInfo> kvp in m_ReturnsByPlayer )
			{
				Mobile m = World.FindMobile( kvp.Key );
				if ( m != null && m.NetState != null && !IsPoolMap( m.Map ) )
				{
					if ( stale == null )
						stale = new List<Serial>();

					stale.Add( kvp.Key );
				}
			}

			if ( stale != null )
			{
				for ( int i = 0; i < stale.Count; i++ )
					m_ReturnsByPlayer.Remove( stale[i] );
			}
		}

		// ----- Pool / terrain -----

		public override void RegisterMaps()
		{
			Map[] bases = GetCloneBaseMaps();

			for ( int b = 0; b < bases.Length; b++ )
			{
				Map baseMap = bases[b];
				if ( baseMap == null )
					continue;

				for ( int i = 0; i < ClonesPerBaseMap; i++ )
				{
					int idx = PoolBaseIndex + ( b * ClonesPerBaseMap ) + i;
					if ( idx >= Map.Maps.Length || Map.Maps[idx] != null )
						continue;

					Map map = new Map( baseMap.MapID, idx, GetFileIndexFor( baseMap ), baseMap.Width, baseMap.Height, 1,
						String.Format( "Dungeon{0}{1}", baseMap.Name, i ), Rules );

					map.BaseMap = baseMap;

					Map.Maps[idx] = map;
					Map.AllMaps.Add( map );
				}
			}
		}

		private static Map[] GetCloneBaseMaps()
		{
			return new Map[]
			{
				Map.Lodor,
				Map.Sosaria,
				Map.Underworld,
				Map.SerpentIsland,
				Map.IslesDread,
				Map.SavagedEmpire,
				Map.Atlantis
			};
		}

		private static int GetFileIndexFor( Map map )
		{
			if ( Object.ReferenceEquals( map, Map.Atlantis ) )
				return 1;

			return map.MapID;
		}

		private static bool IsCloneOf( Map map, Map baseMap )
		{
			return map != null && baseMap != null && Object.ReferenceEquals( map.BaseMap, baseMap );
		}

		public static Map NormalizeExternalMap( Map map )
		{
			if ( map != null && map.BaseMap != null )
				return map.BaseMap;

			return map;
		}

		// ----- Dungeon catalog -----

		public List<DungeonInstanceDefinition> Definitions
		{
			get { return GetDefinitions(); }
		}

		public int DefinitionCount
		{
			get { return GetDefinitions().Count; }
		}

		private List<DungeonInstanceDefinition> GetDefinitions()
		{
			if ( m_Definitions != null )
				return m_Definitions;

			List<DungeonInstanceDefinition> list = new List<DungeonInstanceDefinition>();
			HashSet<string> listedDungeonNames = BuildListedDungeonNames();
			CatalogIndex catalog = CatalogIndex.Build();

			foreach ( Region region in Region.Regions )
			{
				BaseRegion br = region as BaseRegion;
				if ( br == null || br.Map == null || br.Map == Map.Internal || br.Area == null || br.Area.Length == 0 || IsPoolMap( br.Map ) )
					continue;

				if ( !IsDungeonCatalogRegion( br ) || !IsListedDungeonDefinition( br, listedDungeonNames ) )
					continue;

				list.Add( new DungeonInstanceDefinition( br, catalog ) );
			}

			list.Sort( delegate( DungeonInstanceDefinition a, DungeonInstanceDefinition b )
			{
				int map = String.Compare( a.MapName, b.MapName, StringComparison.OrdinalIgnoreCase );
				if ( map != 0 )
					return map;

				return String.Compare( a.Name, b.Name, StringComparison.OrdinalIgnoreCase );
			} );

			for ( int i = 0; i < list.Count; i++ )
				list[i].Index = i;

			m_Definitions = list;
			return m_Definitions;
		}

		private static HashSet<string> BuildListedDungeonNames()
		{
			HashSet<string> names = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

			for ( int i = 1; i <= DungeonListingScanLimit; i++ )
			{
				string world;
				string location;
				Map placer;
				int x;
				int y;
				string dungeon = Worlds.GetDungeonListing( i, out world, out location, out placer, out x, out y );

				AddListedDungeonName( names, dungeon );
			}

			return names;
		}

		private static void AddListedDungeonName( HashSet<string> names, string dungeon )
		{
			if ( String.IsNullOrEmpty( dungeon ) )
				return;

			dungeon = dungeon.Trim();
			if ( dungeon.Length == 0 )
				return;

			names.Add( dungeon );

			string normalized = NormalizeDungeonListingName( dungeon );
			if ( normalized != null && normalized.Length > 0 )
				names.Add( normalized );
		}

		private static bool IsDungeonCatalogRegion( BaseRegion region )
		{
			return region is DungeonRegion || region is BardDungeonRegion || region is CaveRegion || region is DeadRegion;
		}

		private static bool IsListedDungeonDefinition( BaseRegion region, HashSet<string> listedDungeonNames )
		{
			if ( region == null || String.IsNullOrEmpty( region.Name ) || listedDungeonNames == null )
				return false;

			if ( IsExcludedListedDungeon( region.Name ) )
				return false;

			string normalized = NormalizeDungeonListingName( region.Name );
			return listedDungeonNames.Contains( region.Name )
				|| ( normalized != null && listedDungeonNames.Contains( normalized ) );
		}

		private static bool IsExcludedListedDungeon( string name )
		{
			if ( String.IsNullOrEmpty( name ) )
				return false;

			string normalized = NormalizeDungeonListingName( name );

			for ( int i = 0; i < ExcludedListedDungeonNames.Length; i++ )
			{
				string excluded = ExcludedListedDungeonNames[i];
				if ( String.Equals( name, excluded, StringComparison.OrdinalIgnoreCase ) )
					return true;

				if ( String.Equals( normalized, NormalizeDungeonListingName( excluded ), StringComparison.OrdinalIgnoreCase ) )
					return true;
			}

			return false;
		}

		private static string NormalizeDungeonListingName( string name )
		{
			if ( name == null )
				return null;

			name = name.Trim();

			if ( name.StartsWith( "the Dungeon of ", StringComparison.OrdinalIgnoreCase ) )
				return name.Substring( 4 );

			return name;
		}

		public static DungeonInstanceAvailability GetDefinitionAvailability( string displayName )
		{
			if ( ContainsName( BrokenDungeons, displayName ) )
				return DungeonInstanceAvailability.Broken;

			if ( ContainsName( BadEntranceOrExitDungeons, displayName ) )
				return DungeonInstanceAvailability.BadEntranceOrExit;

			return DungeonInstanceAvailability.Allowed;
		}

		public static string GetAvailabilityLabel( DungeonInstanceAvailability availability )
		{
			switch ( availability )
			{
				case DungeonInstanceAvailability.BadEntranceOrExit:
					return "Bad entrance/exit";
				case DungeonInstanceAvailability.Broken:
					return "Broken";
				default:
					return "Good";
			}
		}

		public static string GetAvailabilityReason( DungeonInstanceAvailability availability )
		{
			switch ( availability )
			{
				case DungeonInstanceAvailability.BadEntranceOrExit:
					return BadEntranceOrExitReason;
				case DungeonInstanceAvailability.Broken:
					return BrokenReason;
				default:
					return null;
			}
		}

		public static int GetAvailabilityHue( DungeonInstanceAvailability availability )
		{
			switch ( availability )
			{
				case DungeonInstanceAvailability.BadEntranceOrExit:
					return BadEntranceOrExitDungeonGateHue;
				case DungeonInstanceAvailability.Broken:
					return BrokenDungeonGateHue;
				default:
					return GoodDungeonGateHue;
			}
		}

		private static bool ContainsName( string[] names, string name )
		{
			for ( int i = 0; i < names.Length; i++ )
			{
				if ( String.Equals( names[i], name, StringComparison.OrdinalIgnoreCase ) )
					return true;
			}

			return false;
		}

		public DungeonInstanceDefinition GetDefinition( int index )
		{
			List<DungeonInstanceDefinition> defs = GetDefinitions();

			if ( index < 0 || index >= defs.Count )
				return null;

			return defs[index];
		}

		public DungeonInstanceDefinition FindDefinition( string value )
		{
			if ( value == null )
				return null;

			value = value.Trim();
			if ( value.Length == 0 )
				return null;

			int index;
			if ( Int32.TryParse( value, out index ) )
				return GetDefinition( index );

			List<DungeonInstanceDefinition> defs = GetDefinitions();

			for ( int i = 0; i < defs.Count; i++ )
			{
				DungeonInstanceDefinition def = defs[i];
				if ( String.Equals( def.Name, value, StringComparison.OrdinalIgnoreCase ) ||
					String.Equals( def.DisplayName, value, StringComparison.OrdinalIgnoreCase ) )
					return def;
			}

			for ( int i = 0; i < defs.Count; i++ )
			{
				DungeonInstanceDefinition def = defs[i];
				if ( def.DisplayName.IndexOf( value, StringComparison.OrdinalIgnoreCase ) >= 0 )
					return def;
			}

			return null;
		}

		public DungeonInstanceDefinition GetDefaultDefinition()
		{
			List<DungeonInstanceDefinition> defs = GetDefinitions();

			for ( int i = 0; i < defs.Count; i++ )
			{
				if ( defs[i].CanSpawnInstance && defs[i].SpawnCount > 0 )
					return defs[i];
			}

			for ( int i = 0; i < defs.Count; i++ )
			{
				if ( defs[i].CanSpawnInstance )
					return defs[i];
			}

			return defs.Count > 0 ? defs[0] : null;
		}

		public static Difficulty ClampDifficulty( Difficulty difficulty )
		{
			if ( difficulty < Difficulty.Easy )
				return Difficulty.Easy;

			if ( difficulty > Difficulty.Deadly )
				return Difficulty.Deadly;

			return difficulty;
		}

		// ----- Ownership model -----

		// Party members share one instance (keyed to the leader); solo players get a
		// private one (keyed to themselves).
		protected override Serial OwnerKey( Mobile m )
		{
			Party p = Party.Get( m );
			if ( p != null && p.Active && p.Leader != null )
				return p.Leader.Serial;

			return m.Serial;
		}

		public bool SendToConfiguredInstance( Mobile from, DungeonInstanceDefinition definition, Difficulty difficulty )
		{
			if ( from == null )
				return false;

			return SendToConfiguredInstance( from, OwnerKey( from ), definition, difficulty, null, Point3D.Zero, InstanceOwnerKind.Mobile );
		}

		public bool SendToConfiguredInstance( Mobile from, Serial instanceKey, DungeonInstanceDefinition definition, Difficulty difficulty )
		{
			return SendToConfiguredInstance( from, instanceKey, definition, difficulty, null, Point3D.Zero, InstanceOwnerKind.Mobile );
		}

		public bool SendToConfiguredInstance( Mobile from, Serial instanceKey, DungeonInstanceDefinition definition, Difficulty difficulty, Map returnMap, Point3D returnPoint )
		{
			return SendToConfiguredInstance( from, instanceKey, definition, difficulty, returnMap, returnPoint, InstanceOwnerKind.Mobile );
		}

		public bool SendToConfiguredInstance( Mobile from, Serial instanceKey, DungeonInstanceDefinition definition, Difficulty difficulty, Map returnMap, Point3D returnPoint, InstanceOwnerKind ownerKind )
		{
			return SendToConfiguredInstance( from, instanceKey, definition, difficulty, returnMap, returnPoint, ownerKind, TimeSpan.Zero );
		}

		public bool SendToConfiguredInstance( Mobile from, Serial instanceKey, DungeonInstanceDefinition definition, Difficulty difficulty, Map returnMap, Point3D returnPoint, InstanceOwnerKind ownerKind, TimeSpan timeLimit )
		{
			if ( from == null )
				return false;

			if ( definition == null )
			{
				from.SendMessage( "This dungeon instance gate is not configured with a valid dungeon." );
				return false;
			}

			if ( !definition.CanSpawnInstance )
			{
				from.SendMessage( "This dungeon is temporarily disabled for instance spawning: {0}.", definition.InstanceBlockReason );
				return false;
			}

			difficulty = ClampDifficulty( difficulty );

			DungeonInstanceSettings requested = new DungeonInstanceSettings( definition, difficulty, NormalizeExternalMap( returnMap ), returnPoint, timeLimit );
			Instance existing = GetByKey( instanceKey );
			if ( existing != null )
				existing.OwnerKind = ownerKind;

			if ( existing != null )
			{
				DungeonInstanceSettings active = GetSettings( existing );
				bool changed = active == null || !active.Matches( requested );

				if ( changed )
				{
					if ( existing.IsLive && HasPlayersInside( existing ) )
					{
						if ( active != null && active.Definition != null )
							from.SendMessage( "This gate already has an active {0} instance; sending you there.", active.Definition.Name );

							return SendToInstanceAndRecordReturn( from, existing, requested );
					}

					FreeInstance( existing );
					existing = null;
				}
			}

			Instance inst = existing != null ? existing : GetOrCreateByKey( instanceKey, ownerKind );
			m_SettingsByOwner[inst.OwnerSerial] = requested;
			if ( inst.IsLive )
				m_SettingsByLiveMap[inst.LiveMapIndex] = requested;

			if ( !SendToInstanceAndRecordReturn( from, inst, requested ) )
			{
				FreeInstance( inst );
				return false;
			}

			return true;
		}

		public bool SendToDefaultInstance( Mobile from, Map returnMap, Point3D returnPoint )
		{
			if ( from == null )
				return false;

			return SendToConfiguredInstance( from, OwnerKey( from ), GetDefaultDefinition(), Difficulty.Normal, returnMap, returnPoint, InstanceOwnerKind.Mobile );
		}

		private bool SendToInstanceAndRecordReturn( Mobile from, Instance inst, DungeonInstanceSettings settings )
		{
			if ( !SendToInstance( from, inst ) )
				return false;

			RegisterReturn( from, inst, settings );

			RuntimeDungeonState state;
			if ( m_Runtimes.TryGetValue( inst.OwnerSerial, out state ) )
				state.SendTimeRemaining( from );

			return true;
		}

		public bool HasInstance( Serial instanceKey )
		{
			return GetByKey( instanceKey ) != null;
		}

		// Any dungeon that can be instanced, for gates that roll their destination.
		public DungeonInstanceDefinition GetRandomDefinition()
		{
			List<DungeonInstanceDefinition> defs = GetDefinitions();
			List<DungeonInstanceDefinition> pool = new List<DungeonInstanceDefinition>();

			for ( int i = 0; i < defs.Count; i++ )
			{
				if ( defs[i].CanSpawnInstance && defs[i].SpawnCount > 0 )
					pool.Add( defs[i] );
			}

			return pool.Count > 0 ? pool[Utility.Random( pool.Count )] : null;
		}

		// Staff testing: wakes the boss of the instance this mobile stands in, next to them.
		public bool SummonBoss( Mobile near )
		{
			RuntimeDungeonState state = near != null ? GetRuntime( near.Map ) : null;
			return state != null && state.ForceBoss( near.Location );
		}

		// The run on this map ran out of time: everyone goes home and the copy is dissolved.
		private void ExpireInstance( Map map )
		{
			Instance inst = GetLiveInstance( map );
			if ( inst == null )
				return;

			EjectPlayers( inst );
			FreeInstance( inst );
		}

		// For staff audits: builds an instance without sending anyone in, ignoring availability.
		public Instance BuildAuditInstance( DungeonInstanceDefinition definition, Serial key )
		{
			Instance old = GetByKey( key );
			if ( old != null )
				FreeInstance( old );

			Instance inst = GetOrCreateByKey( key, InstanceOwnerKind.PublicGateway );
			m_SettingsByOwner[key] = new DungeonInstanceSettings( definition, Difficulty.Normal );

			if ( !EnsureLive( inst ) )
			{
				FreeInstance( inst );
				return null;
			}

			return inst;
		}

			public bool CleanupInstance( Serial instanceKey )
			{
				Instance inst = GetByKey( instanceKey );
				if ( inst == null )
					return false;

			EjectPlayers( inst );
				FreeInstance( inst );
				return true;
			}

			public int ClearAllInstances()
			{
				List<Instance> instances = new List<Instance>( AllInstances );

				for ( int i = 0; i < instances.Count; i++ )
				{
					Instance inst = instances[i];
					EjectPlayers( inst );
					FreeInstance( inst );
				}

				return instances.Count;
			}

			private static void ClearTrackedInstanceItems( Instance inst )
			{
				if ( inst == null || inst.Items.Count == 0 )
					return;

				for ( int i = 0; i < inst.Items.Count; i++ )
				{
					InstanceItem item = inst.Items[i];
					if ( item != null && item.Item != null && !item.Item.Deleted )
						item.Item.Delete();
				}

				inst.Items.Clear();
			}

			private void EjectPlayers( Instance inst )
			{
				if ( inst == null || !inst.IsLive )
					return;

				Map map = inst.LiveMap;
				if ( map == null )
					return;

				
				List<Mobile> players = null;
				foreach ( NetState ns in NetState.Instances )
				{
					Mobile m = ns.Mobile;
					if ( m != null && m.Map == map )
					{
						if ( players == null )
							players = new List<Mobile>();

						players.Add( m );
					}
				}

				if ( players == null )
					return;

				for ( int i = 0; i < players.Count; i++ )
				{
					Mobile m = players[i];
					if ( m == null || m.Deleted || m.Map != map )
						continue;

					DungeonReturnInfo info = GetReturnInfoFor( m, inst );
					Map exitMap = info != null ? info.ReturnMap : GetReturnMap( inst );
					Point3D exitPoint = info != null ? info.ReturnPoint : GetReturnPoint( inst );
					exitMap = NormalizeExternalMap( exitMap );

					BaseCreature.TeleportPets( m, exitPoint, exitMap, false );
					m.MoveToWorld( exitPoint, exitMap );
					ClearReturn( m );
				}
			}

		// Where a player's corpse or pet goes when its instance is cleared.
		private void GetReturnLocationFor( Instance inst, Mobile owner, out Map returnMap, out Point3D returnPoint )
		{
			DungeonReturnInfo info = GetReturnInfoFor( owner, inst );

			if ( info != null )
			{
				returnMap = NormalizeExternalMap( info.ReturnMap );
				returnPoint = info.ReturnPoint;
			}
			else
			{
				returnMap = GetReturnMap( inst );
				returnPoint = GetReturnPoint( inst );
			}

			if ( returnMap == null )
			{
				returnMap = ExitMap;
				returnPoint = ExitPoint;
			}
		}

		private static void RemoveTrackedInstanceItem( Instance inst, Item item )
		{
			if ( inst == null || item == null )
				return;

			for ( int i = inst.Items.Count - 1; i >= 0; i-- )
			{
				InstanceItem instanceItem = inst.Items[i];
				if ( instanceItem != null && Object.ReferenceEquals( instanceItem.Item, item ) )
					inst.Items.RemoveAt( i );
			}
		}

		public string GetInstanceStatus( Serial instanceKey )
		{
			Instance inst = GetByKey( instanceKey );
			if ( inst == null )
				return "None";

			DungeonInstanceSettings settings = GetSettings( inst );
			string dungeon = settings != null && settings.Definition != null ? settings.Definition.Name : "Unknown";

			RuntimeDungeonState state;
			if ( inst.IsLive && m_Runtimes.TryGetValue( inst.OwnerSerial, out state ) )
				return String.Format( "Live: {0} on map {1}, {2}/{3} points, {4}, {5}", dungeon, inst.LiveMapIndex, state.KilledPoints, state.TotalPoints, state.BossLabel, state.ClockLabel );

			if ( inst.IsLive )
				return String.Format( "Live: {0} on map {1}", dungeon, inst.LiveMapIndex );

			return String.Format( "Parked: {0}", dungeon );
		}

		public override bool LeaveInstance( Mobile m )
		{
			if ( m == null || !IsPoolMap( m.Map ) )
				return false;

			DungeonReturnInfo info = GetReturnInfoFor( m, GetLiveInstance( m.Map ) );
			Map exitMap = info != null ? info.ReturnMap : GetExitMap( m );
			Point3D exitPoint = info != null ? info.ReturnPoint : GetExitPoint( m );
			exitMap = NormalizeExternalMap( exitMap );

			BaseCreature.TeleportPets( m, exitPoint, exitMap, false );
			m.MoveToWorld( exitPoint, exitMap );
			ClearReturn( m );
			return true;
		}

		public override void OnLogin( Mobile m )
		{
			if ( m == null || !IsPoolMap( m.Map ) )
				return;

			DungeonReturnInfo info = GetReturnInfo( m );

			// Still standing in the instance they entered; parties don't survive a restart, so this is by record.
			Instance entered = info != null ? GetByKey( info.InstanceKey ) : null;
			if ( entered != null && Object.ReferenceEquals( entered.LiveMap, m.Map ) )
			{
				entered.Touch();
				return;
			}

			if ( info != null && info.OwnerKind == InstanceOwnerKind.PublicGateway )
			{
				BounceToReturn( m, info );
				ClearReturn( m );
				return;
			}

			Instance privateInst = GetByOwner( m );
			if ( privateInst != null )
			{
				if ( Object.ReferenceEquals( privateInst.LiveMap, m.Map ) )
					privateInst.Touch();
				else
					SendToInstance( m, privateInst );

				return;
			}

			if ( info != null )
			{
				BounceToReturn( m, info );
				ClearReturn( m );
				return;
			}

			BounceToReturn( m, null );
		}

		protected override void OnReleaseDeadOwner( Instance inst )
		{
			EjectPlayers( inst );
		}

			protected override Map GetExitMap( Mobile m )
			{
				DungeonInstanceSettings settings = GetSettingsForMap( m != null ? m.Map : null );
				return NormalizeExternalMap( settings != null && settings.ReturnMap != null ? settings.ReturnMap : base.GetExitMap( m ) );
			}

		protected override Point3D GetExitPoint( Mobile m )
		{
			DungeonInstanceSettings settings = GetSettingsForMap( m != null ? m.Map : null );
			return settings != null && settings.ReturnMap != null ? settings.ReturnPoint : base.GetExitPoint( m );
		}

		private DungeonInstanceSettings GetSettingsForMap( Map map )
		{
			if ( map == null )
				return null;

			DungeonInstanceSettings settings;
			if ( m_SettingsByLiveMap.TryGetValue( map.MapIndex, out settings ) )
				return settings;

			return null;
		}

		private Map GetReturnMap( Instance inst )
		{
			DungeonInstanceSettings settings = GetSettings( inst );
			return NormalizeExternalMap( settings != null && settings.ReturnMap != null ? settings.ReturnMap : ExitMap );
		}

		private Point3D GetReturnPoint( Instance inst )
		{
			DungeonInstanceSettings settings = GetSettings( inst );
			return settings != null && settings.ReturnMap != null ? settings.ReturnPoint : ExitPoint;
		}

		private void RegisterReturn( Mobile from, Instance inst, DungeonInstanceSettings settings )
		{
			if ( from == null || inst == null || settings == null || settings.ReturnMap == null )
				return;

			Map returnMap = NormalizeExternalMap( settings.ReturnMap );
			if ( returnMap == null )
				return;

			m_ReturnsByPlayer[from.Serial] = new DungeonReturnInfo(
				inst.OwnerSerial,
				inst.OwnerKind,
				returnMap,
				settings.ReturnPoint,
				DateTime.Now );
		}

		private DungeonReturnInfo GetReturnInfo( Mobile m )
		{
			if ( m == null )
				return null;

			DungeonReturnInfo info;
			m_ReturnsByPlayer.TryGetValue( m.Serial, out info );
			return info;
		}

		// Ignores a return record left over from an earlier visit.
		private DungeonReturnInfo GetReturnInfoFor( Mobile m, Instance inst )
		{
			DungeonReturnInfo info = GetReturnInfo( m );
			if ( info == null || info.ReturnMap == null )
				return null;

			if ( inst != null && info.InstanceKey != inst.OwnerSerial )
				return null;

			return info;
		}

		private void ClearReturn( Mobile m )
		{
			if ( m != null )
				m_ReturnsByPlayer.Remove( m.Serial );
		}

		private void BounceToReturn( Mobile m, DungeonReturnInfo info )
		{
			if ( m == null )
				return;

			Map map = NormalizeExternalMap( info != null && info.ReturnMap != null ? info.ReturnMap : ExitMap );
			Point3D point = info != null && info.ReturnMap != null ? info.ReturnPoint : ExitPoint;

			BaseCreature.TeleportPets( m, point, map, false );
			m.MoveToWorld( point, map );
		}

		private DungeonInstanceSettings GetSettings( Instance inst )
		{
			if ( inst == null )
				return null;

			DungeonInstanceSettings settings;
			if ( m_SettingsByOwner.TryGetValue( inst.OwnerSerial, out settings ) && settings != null && settings.Definition != null )
				return settings;

			DungeonInstanceDefinition def = GetDefaultDefinition();
			if ( def == null )
				return null;

			settings = new DungeonInstanceSettings( def, Difficulty.Normal );
			m_SettingsByOwner[inst.OwnerSerial] = settings;
			return settings;
		}

		// ----- Live map selection / entry -----

		protected override int AcquireMapIndex( Instance inst )
		{
			DungeonInstanceSettings settings = GetSettings( inst );
			Map requiredBase = settings != null && settings.Definition != null ? settings.Definition.Map : BaseMap;

			for ( int i = 0; i < PoolSize; i++ )
			{
				int idx = PoolBaseIndex + i;
				if ( idx >= Map.Maps.Length )
					continue;

				Map map = Map.Maps[idx];
				if ( IsCloneOf( map, requiredBase ) && !IsMapIndexLive( idx ) )
					return idx;
			}

			Instance victim = null;
			foreach ( Instance live in LiveInstances )
			{
				if ( HasPlayersInside( live ) )
					continue;

				if ( !IsCloneOf( live.LiveMap, requiredBase ) )
					continue;

				if ( victim == null || live.LastTouched < victim.LastTouched )
					victim = live;
			}

			if ( victim == null )
				return -1;

			int freed = victim.LiveMapIndex;
			FreeInstance( victim );
			return freed;
		}

		protected override Point3D GetLandingPoint( Instance inst, Map map )
		{
			DungeonInstanceSettings settings = GetSettings( inst );
			if ( settings != null && settings.Definition != null )
				return settings.Definition.Landing;

			return base.GetLandingPoint( inst, map );
		}

		// ----- Contents -----

		protected override void BuildContents( Instance inst, Map map )
		{
			CleanupRuntime( inst.OwnerSerial );

			DungeonInstanceSettings settings = GetSettings( inst );
			if ( settings == null || settings.Definition == null || map == null )
				return;

			ClearTrackedInstanceItems( inst );
			ClearPoolMap( map, inst );

			m_SettingsByLiveMap[map.MapIndex] = settings;

			DungeonInstanceDefinition definition = settings.Definition;
			List<BaseRegion> regions = new List<BaseRegion>();
			List<SpawnEntry> entries = new List<SpawnEntry>();

			for ( int i = 0; i < definition.ConnectedRegions.Length; i++ )
			{
				BaseRegion source = definition.ConnectedRegions[i];
				BaseRegion copy = CopyRegion( source, map, definition.GetRegionArea( i ) );
				if ( copy == null )
					continue;

				copy.Register();
				copy.Spawns = CopySpawns( source, copy, entries );
				regions.Add( copy );
			}

			// Everything spawns once, up front, and nothing respawns.
			for ( int i = 0; i < entries.Count; i++ )
				entries[i].Respawn();

			DetachRegionSpawns( regions );
			CloneWorldContent( inst, map, definition );

			RuntimeDungeonState state = new RuntimeDungeonState( settings, regions, map, definition.Area );
			m_Runtimes[inst.OwnerSerial] = state;
			state.ApplyDifficulty();
			state.TrackMonsters();
			state.StartClock();
			state.StartScaler();
		}

		// Frees what the region entries spawned from them, then drops the entries.
		private static void DetachRegionSpawns( List<BaseRegion> regions )
		{
			for ( int i = 0; i < regions.Count; i++ )
			{
				SpawnEntry[] spawns = regions[i].Spawns;
				if ( spawns == null )
					continue;

				for ( int j = 0; j < spawns.Length; j++ )
				{
					List<ISpawnable> spawned = spawns[j].SpawnedObjects;
					for ( int k = 0; k < spawned.Count; k++ )
						spawned[k].Spawner = null;

					spawned.Clear();
				}

				regions[i].Spawns = null;
			}
		}

		// Same type, name and settings as the real region, so light, music and region checks match.
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
			SpawnEntry[] src = source.Spawns;
			if ( src == null )
				return null;

			List<SpawnEntry> list = new List<SpawnEntry>();

			for ( int i = 0; i < src.Length; i++ )
			{
				SpawnEntry e = src[i];
				if ( e == null || e.Definition == null || e.Max <= 0 || SpawnsTreasure( e.Definition ) )
					continue;

				SpawnEntry entry = new SpawnEntry( NextRuntimeSpawnId(), copy, e.HomeLocation, e.HomeRange, e.Direction,
					e.Definition, e.Max, e.MinSpawnTime, e.MaxSpawnTime );

				list.Add( entry );
				all.Add( entry );
			}

			return list.ToArray();
		}

		// Copies everything fixed in place (not loose items or spawner output), remapping item references to the copies.
		private void CloneWorldContent( Instance inst, Map map, DungeonInstanceDefinition definition )
		{
			List<Item> sources = new List<Item>();

			foreach ( Item item in World.Items.Values )
			{
				if ( item == null || item.Deleted || item.Parent != null || !Object.ReferenceEquals( item.Map, definition.Map ) )
					continue;

				if ( ShouldCloneWorldItem( item, definition ) )
					sources.Add( item );
			}

			Dictionary<Item, Item> clones = new Dictionary<Item, Item>();

			for ( int i = 0; i < sources.Count; i++ )
			{
				Item clone = CloneWorldItem( sources[i], definition );
				if ( clone != null )
					clones[sources[i]] = clone;
			}

			foreach ( KeyValuePair<Item, Item> kvp in clones )
			{
				Item source = kvp.Key;
				Item clone = kvp.Value;

				RemapItemReferences( clone, clones );
				clone = ApplyDestinationRule( clone, map, definition );

				clone.MoveToWorld( source.Location, map );
				inst.Items.Add( new InstanceItem( clone, source.Location ) );
				SpawnOnce( clone );
			}

			CloneDecoratedTeleporters( inst, map, definition );
		}

		// Portals that send players to another map or a random spot in the real world, skipping the
		// gate; a real-world mirror could also pick an instance's copy as its destination.
		private static readonly string[] UncopiedPortalTypes = new string[]
		{
			"MagicMirror", "RandomExit", "PrisonExit", "SkullGate", "SerpentPillars", "GateMoon",
			"RaceTeleporter", "BasementDoor", "ElementalExit"
		};

		// Instances hold no chests, trap-door stashes or treasure hoards: any container, the hidden chests
		// that turn into trap doors, the dungeon chest respawner, and the markers hoards appear on.
		private static bool IsTreasureType( Type type )
		{
			return type != null && ( typeof( Container ).IsAssignableFrom( type ) || typeof( HiddenChest ).IsAssignableFrom( type )
				|| typeof( DungeonChestSpawner ).IsAssignableFrom( type ) || typeof( HoardTile ).IsAssignableFrom( type )
				|| typeof( HoardPiles ).IsAssignableFrom( type ) );
		}

		private static bool SpawnsTreasure( SpawnDefinition definition )
		{
			SpawnType single = definition as SpawnType;
			if ( single != null )
				return IsTreasureType( single.Type );

			return definition.CanSpawn( typeof( HiddenChest ), typeof( DungeonChest ), typeof( DungeonChestSpawner ), typeof( HoardTile ), typeof( HoardPiles ) );
		}

		private static bool ShouldCloneWorldItem( Item item, DungeonInstanceDefinition definition )
		{
			if ( IsTreasureType( item.GetType() ) )
				return false;

			if ( item is Spawner || item is PremiumSpawner )
				return definition.Contains( item.Location );

			if ( item is BaseDoor || IsTeleportItem( item ) )
				return definition.ContainsFunctionalItem( item.Location );

			if ( !definition.Contains( item.Location ) )
				return false;

			if ( item.Movable || item.Spawner != null )
				return false;

			if ( item is Corpse || item is BaseMulti || item is DungeonInstanceGate || item is DungeonInstanceExitTeleporter || item is DungeonEntrance )
				return false;

			if ( Array.IndexOf( UncopiedPortalTypes, item.GetType().Name ) >= 0 )
				return false;

			// Components come with their addon when it can be copied.
			AddonComponent component = item as AddonComponent;
			if ( component != null && component.Addon != null && IsCloneableAddon( component.Addon, definition ) )
				return false;

			return true;
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

		// Internal destinations are retargeted to the instance; any other (another map, surface, town) becomes an exit.
		private static Item ApplyDestinationRule( Item clone, Map map, DungeonInstanceDefinition definition )
		{
			Map destinationMap;
			Point3D destination;

			if ( !TryGetTeleportDestination( clone, out destinationMap, out destination ) )
				return clone;

			if ( IsInternalDestination( definition.Map, destinationMap, destination, definition ) )
			{
				SetTeleportDestinationMap( clone, map );
				return clone;
			}

			// A door graphic is impassable, so a magic door's exit takes the plain exit tile.
			Item exit = clone is BaseDoor ? new DungeonInstanceExitTeleporter() : new DungeonInstanceExitTeleporter( clone.ItemID );
			clone.Delete();
			return exit;
		}

		private static void RemapItemReferences( Item clone, Dictionary<Item, Item> clones )
		{
			PropertyInfo[] props = clone.GetType().GetProperties();

			for ( int i = 0; i < props.Length; i++ )
			{
				PropertyInfo prop = props[i];
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

			private void CloneDecoratedTeleporters( Instance inst, Map map, DungeonInstanceDefinition definition )
			{
				List<DecorationTeleporterSource> sources = GetDecorationTeleporterSources( definition.Map );
				if ( sources == null || sources.Count == 0 )
					return;

				for ( int i = 0; i < sources.Count; i++ )
				{
					DecorationTeleporterSource source = sources[i];

					if ( source == null || !definition.ContainsFunctionalItem( source.Location ) )
						continue;

					bool internalDestination = IsInternalDestination( source.SourceMap, source.DestinationMap, source.Destination, definition );

					Item clone;
					if ( internalDestination )
					{
						if ( HasTeleporterCloneAt( map, source ) )
							continue;

						clone = source.Construct();
						if ( clone == null )
							continue;

						RetargetClone( source.SourceMap, clone, map, definition );
					}
					else
					{
						if ( !ShouldCloneExitTeleporter( source.SourceMap, source.DestinationMap, source.Destination, definition ) )
							continue;

						if ( HasExitTeleporterCloneAt( map, source ) )
							continue;

						clone = source.ConstructExitTeleporter();
					}

					if ( clone == null )
						continue;

					clone.MoveToWorld( source.Location, map );
					inst.Items.Add( new InstanceItem( clone, source.Location ) );
				}
			}

			private static bool HasTeleporterCloneAt( Map map, DecorationTeleporterSource source )
			{
				IPooledEnumerable eable = map.GetItemsInRange( source.Location, 0 );
				try
				{
					foreach ( Item item in eable )
					{
						if ( item == null || item.Deleted || item.Parent != null )
							continue;

						if ( item.Z == source.Location.Z && item.ItemID == source.ItemID && item.GetType() == source.Type )
							return true;
					}
				}
				finally
				{
					eable.Free();
				}

				return false;
			}

			private static bool HasExitTeleporterCloneAt( Map map, DecorationTeleporterSource source )
			{
				IPooledEnumerable eable = map.GetItemsInRange( source.Location, 0 );
				try
				{
					foreach ( Item item in eable )
					{
						if ( item == null || item.Deleted || item.Parent != null )
							continue;

						if ( item.Z == source.Location.Z && item.ItemID == source.ItemID && item is DungeonInstanceExitTeleporter )
							return true;
					}
				}
				finally
				{
					eable.Free();
				}

				return false;
			}

		internal static bool TryGetTeleportDestination( Item item, out Map destinationMap, out Point3D destination )
		{
			destinationMap = null;
			destination = Point3D.Zero;

			Teleporter teleporter = item as Teleporter;
			if ( teleporter != null )
			{
				destinationMap = teleporter.MapDest;
				destination = teleporter.PointDest;
				return destinationMap != null || destination != Point3D.Zero;
			}

			moongates gate = item as moongates;
			if ( gate != null )
			{
				destinationMap = gate.MapDest;
				destination = gate.PointDest;
				return destinationMap != null || destination != Point3D.Zero;
			}

			QuestTeleporter quest = item as QuestTeleporter;
			if ( quest != null )
			{
				destinationMap = quest.TeleporterMapDest;
				destination = quest.TeleporterPointDest;
				return destinationMap != null || destination != Point3D.Zero;
			}

			ThruDoor thruDoor = item as ThruDoor;
			if ( thruDoor != null )
			{
				destinationMap = thruDoor.MapDest;
				destination = thruDoor.PointDest;
				return destinationMap != null || destination != Point3D.Zero;
			}

			return false;
		}

		private static bool IsInternalDestination( Map sourceMap, Map destinationMap, Point3D destination, DungeonInstanceDefinition definition )
		{
			if ( sourceMap == null || definition == null || destination == Point3D.Zero )
				return false;

			Map resolvedMap = ResolveDestinationMap( sourceMap, destinationMap );
			return Object.ReferenceEquals( resolvedMap, definition.Map ) && definition.Contains( destination );
		}

		private static bool ShouldCloneExitTeleporter( Map sourceMap, Map destinationMap, Point3D destination, DungeonInstanceDefinition definition )
		{
			return sourceMap != null && definition != null && destination != Point3D.Zero
				&& !IsInternalDestination( sourceMap, destinationMap, destination, definition );
		}

		private static Map ResolveDestinationMap( Map sourceMap, Map destinationMap )
		{
			if ( destinationMap == null || destinationMap == Map.Internal )
				return sourceMap;

			return destinationMap;
		}

		private static void RetargetClone( Map sourceMap, Item clone, Map map, DungeonInstanceDefinition definition )
		{
			Map destinationMap;
			Point3D destination;
			if ( !TryGetTeleportDestination( clone, out destinationMap, out destination ) )
				return;

			if ( !IsInternalDestination( sourceMap, destinationMap, destination, definition ) )
				return;

			SetTeleportDestinationMap( clone, map );
		}

			private static void SetTeleportDestinationMap( Item clone, Map map )
			{
				Teleporter teleporter = clone as Teleporter;
				if ( teleporter != null )
			{
				teleporter.MapDest = map;
				return;
			}

			moongates gate = clone as moongates;
			if ( gate != null )
			{
				gate.MapDest = map;
				return;
			}

			QuestTeleporter quest = clone as QuestTeleporter;
			if ( quest != null )
			{
				quest.TeleporterMapDest = map;
				return;
			}

			ThruDoor thruDoor = clone as ThruDoor;
				if ( thruDoor != null )
					thruDoor.MapDest = map;
			}

			private static List<DecorationTeleporterSource> GetDecorationTeleporterSources( Map map )
			{
				if ( map == null )
					return null;

				List<DecorationTeleporterSource> sources;
				if ( !m_DecorationTeleportersByMap.TryGetValue( map.MapIndex, out sources ) )
				{
					sources = LoadDecorationTeleporterSources( map );
					m_DecorationTeleportersByMap[map.MapIndex] = sources;
				}

				return sources;
			}

			private static List<DecorationTeleporterSource> LoadDecorationTeleporterSources( Map map )
			{
				List<DecorationTeleporterSource> sources = new List<DecorationTeleporterSource>();
				string fileName = GetDecorationFileName( map );
				if ( fileName == null )
					return sources;

				string path = Path.Combine( Core.BaseDirectory, Path.Combine( "Data/Decoration", fileName ) );
				if ( !File.Exists( path ) )
				{
					path = Path.Combine( "Data/Decoration", fileName );
					if ( !File.Exists( path ) )
						return sources;
				}

				try
				{
					using ( StreamReader reader = new StreamReader( path ) )
						ReadDecorationTeleporterSources( reader, map, sources );
				}
				catch
				{
				}

				return sources;
			}

			private static string GetDecorationFileName( Map map )
			{
				if ( Object.ReferenceEquals( map, Map.Lodor ) )
					return "Lodor.cfg";

				if ( Object.ReferenceEquals( map, Map.Sosaria ) )
					return "Sosaria.cfg";

				if ( Object.ReferenceEquals( map, Map.Underworld ) )
					return "Underworld.cfg";

				if ( Object.ReferenceEquals( map, Map.SerpentIsland ) )
					return "SerpentIsland.cfg";

				if ( Object.ReferenceEquals( map, Map.IslesDread ) )
					return "IslesOfDread.cfg";

				if ( Object.ReferenceEquals( map, Map.SavagedEmpire ) )
					return "SavagedEmpire.cfg";

				if ( Object.ReferenceEquals( map, Map.Atlantis ) )
					return "Atlantis.cfg";

				return null;
			}

			private static void ReadDecorationTeleporterSources( StreamReader reader, Map sourceMap, List<DecorationTeleporterSource> sources )
			{
				string line;
				while ( TryReadDecorationHeader( reader, out line ) )
				{
					string typeName;
					int itemID;
					string[] parameters;
					bool useEntries = false;
					Type type = null;

					if ( TryParseDecorationHeader( line, out typeName, out itemID, out parameters ) )
					{
						type = ScriptCompiler.FindTypeByName( typeName, true );
						useEntries = type != null && IsDecorationTeleporterType( type ) && HasDecorationParameter( parameters, "PointDest" );
					}

					while ( (line = reader.ReadLine()) != null )
					{
						line = line.Trim();

						if ( line.Length == 0 )
							break;

						if ( line.StartsWith( "#" ) )
							continue;

						if ( !useEntries )
							continue;

						Point3D location;
						if ( !TryParseDecorationLocation( line, out location ) )
							continue;

						DecorationTeleporterSource source = new DecorationTeleporterSource( sourceMap, type, itemID, parameters, location );
						if ( source.Destination != Point3D.Zero )
							sources.Add( source );
					}
				}
			}

			private static bool TryReadDecorationHeader( StreamReader reader, out string line )
			{
				while ( (line = reader.ReadLine()) != null )
				{
					line = line.Trim();

					if ( line.Length > 0 && !line.StartsWith( "#" ) )
						return true;
				}

				return false;
			}

			private static bool TryParseDecorationHeader( string line, out string typeName, out int itemID, out string[] parameters )
			{
				typeName = null;
				itemID = 0;
				parameters = EmptyStringArray;

				int typeEnd = line.IndexOf( ' ' );
				if ( typeEnd <= 0 )
					return false;

				typeName = line.Substring( 0, typeEnd ).Trim();

				string rest = line.Substring( typeEnd + 1 ).Trim();
				int paramStart = rest.IndexOf( '(' );

				if ( paramStart >= 0 )
				{
					itemID = Utility.ToInt32( rest.Substring( 0, paramStart ).Trim() );

					string paramText = rest.Substring( paramStart + 1 ).Trim();
					if ( paramText.EndsWith( ")" ) )
						paramText = paramText.Substring( 0, paramText.Length - 1 );

					parameters = SplitDecorationParameters( paramText );
				}
				else
				{
					itemID = Utility.ToInt32( rest );
				}

				return typeName.Length > 0;
			}

			private static string[] SplitDecorationParameters( string paramText )
			{
				if ( paramText == null || paramText.Length == 0 )
					return EmptyStringArray;

				string[] parameters = paramText.Split( ';' );
				for ( int i = 0; i < parameters.Length; i++ )
					parameters[i] = parameters[i].Trim();

				return parameters;
			}

			private static bool TryParseDecorationLocation( string line, out Point3D location )
			{
				location = Point3D.Zero;

				string[] parts = line.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
				if ( parts.Length < 3 )
					return false;

				try
				{
					location = new Point3D( Utility.ToInt32( parts[0] ), Utility.ToInt32( parts[1] ), Utility.ToInt32( parts[2] ) );
					return true;
				}
				catch
				{
					return false;
				}
			}

			private static bool IsDecorationTeleporterType( Type type )
			{
				return typeof( Teleporter ).IsAssignableFrom( type )
					|| typeof( moongates ).IsAssignableFrom( type )
					|| typeof( QuestTeleporter ).IsAssignableFrom( type );
			}

			private static bool HasDecorationParameter( string[] parameters, string parameterName )
			{
				for ( int i = 0; i < parameters.Length; i++ )
				{
					string name;
					string value;
					if ( TrySplitDecorationParameter( parameters[i], out name, out value ) && String.Equals( name, parameterName, StringComparison.OrdinalIgnoreCase ) )
						return true;
				}

				return false;
			}

			private static bool TrySplitDecorationParameter( string parameter, out string name, out string value )
			{
				name = null;
				value = null;

				if ( parameter == null )
					return false;

				int index = parameter.IndexOf( '=' );
				if ( index < 0 )
				{
					name = parameter.Trim();
					return name.Length > 0;
				}

				name = parameter.Substring( 0, index ).Trim();
				value = parameter.Substring( index + 1 ).Trim();

				return name.Length > 0;
			}

			private static Map ParseDecorationMap( string value )
			{
				if ( value == null || value.Length == 0 )
					return null;

				try
				{
					return Map.Parse( value );
				}
				catch
				{
					return null;
				}
			}

			private static Point3D ParseDecorationPoint( string value )
			{
				if ( value == null || value.Length == 0 )
					return Point3D.Zero;

				try
				{
					return Point3D.Parse( value );
				}
				catch
				{
					return Point3D.Zero;
				}
			}

		private static Item CloneItem( Item source )
		{
			ConstructorInfo c = source.GetType().GetConstructor( Type.EmptyTypes );
			if ( c == null )
				return null;

			try
			{
				Item newItem = c.Invoke( null ) as Item;
				if ( newItem != null )
					CopyItemProperties( newItem, source );

				return newItem;
			}
			catch
			{
				return null;
			}
		}

		// Dupe.CopyProperties minus placement: setting Map or Location would put the copy on the real map.
		private static readonly string[] UncopiedProperties = new string[]
		{
			"Map", "Location", "X", "Y", "Z", "Parent", "Spawner"
		};

		private static void CopyItemProperties( Item dest, Item src )
		{
			PropertyInfo[] props = src.GetType().GetProperties();

			for ( int i = 0; i < props.Length; i++ )
			{
				PropertyInfo prop = props[i];
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

		private static Item CloneAsStatic( Item source )
		{
			Static copy = new Static( source.ItemID );
			copy.Hue = source.Hue;
			copy.Name = source.Name;
			copy.Light = source.Light;
			copy.Visible = source.Visible;
			return copy;
		}

		private static Item CloneWorldItem( Item source, DungeonInstanceDefinition definition )
		{
			BaseDoor door = source as BaseDoor;
			if ( door != null )
				return CloneDoor( door );

			PremiumSpawner premium = source as PremiumSpawner;
			if ( premium != null )
			{
				PremiumSpawner clone = new PremiumSpawner(
					premium.Count,
					premium.CountA,
					premium.CountB,
					premium.CountC,
					premium.CountD,
					premium.CountE,
					premium.SpawnID,
					premium.MinDelay,
					premium.MaxDelay,
					premium.Team,
					premium.HomeRange,
					premium.WalkingRange,
					CopyStrings( premium.CreaturesName ),
					CopyStrings( premium.SubSpawnerA ),
					CopyStrings( premium.SubSpawnerB ),
					CopyStrings( premium.SubSpawnerC ),
					CopyStrings( premium.SubSpawnerD ),
					CopyStrings( premium.SubSpawnerE ) );

				clone.Group = premium.Group;
				clone.Running = false;
				return clone;
			}

			Spawner classic = source as Spawner;
			if ( classic != null )
			{
				Spawner clone = new Spawner(
					classic.Count,
					classic.MinDelay,
					classic.MaxDelay,
					classic.Team,
					classic.HomeRange,
					CopyStrings( classic.SpawnNames ) );

				clone.WalkingRange = classic.WalkingRange;
				clone.Group = classic.Group;
				clone.Running = false;
				return clone;
			}

			BaseAddon addon = source as BaseAddon;
			if ( addon != null )
				return IsCloneableAddon( addon, definition ) ? CloneItem( addon ) : null;

			if ( source is AddonComponent )
				return CloneAsStatic( source );

			Item copy = CloneItem( source );
			return copy != null ? copy : CloneAsStatic( source );
		}

		private static Item CloneDoor( BaseDoor source )
		{
			Item clone = CloneItem( source );
			if ( clone != null )
				return clone;

			ConstructorInfo c = source.GetType().GetConstructor( new Type[] { typeof( DoorFacing ) } );
			if ( c == null )
				return null;

			try
			{
				BaseDoor door = c.Invoke( new object[] { GuessDoorFacing( source ) } ) as BaseDoor;
				if ( door == null )
					return null;

				CopyItemProperties( door, source );
				return door;
			}
			catch
			{
				return null;
			}
		}

		private static DoorFacing GuessDoorFacing( BaseDoor door )
		{
			Array values = Enum.GetValues( typeof( DoorFacing ) );
			for ( int i = 0; i < values.Length; i++ )
			{
				DoorFacing facing = (DoorFacing)values.GetValue( i );
				if ( BaseDoor.GetOffset( facing ) == door.Offset )
					return facing;
			}

			return DoorFacing.WestCW;
		}

		private static List<string> CopyStrings( List<string> source )
		{
			return source != null ? new List<string>( source ) : new List<string>();
		}

		// The copy stays stopped, so it never respawns.
		private static void SpawnOnce( Item clone )
		{
			PremiumSpawner premium = clone as PremiumSpawner;
			if ( premium != null )
			{
				premium.Respawn();
				return;
			}

			Spawner classic = clone as Spawner;
			if ( classic != null )
				classic.Respawn();
		}

		private int NextRuntimeSpawnId()
		{
			while ( SpawnEntry.Table.Contains( m_NextRuntimeSpawnId ) )
				m_NextRuntimeSpawnId--;

			return m_NextRuntimeSpawnId--;
		}

		protected override void OnFreeInstance( Instance inst, Map liveMap )
		{
			CleanupRuntime( inst.OwnerSerial );

			// Clear before dropping the settings: clearing reads them for return points.
			if ( liveMap != null )
			{
				ClearPoolMap( liveMap, inst );
				m_SettingsByLiveMap.Remove( liveMap.MapIndex );
			}

			m_SettingsByOwner.Remove( inst.OwnerSerial );
		}

		private void CleanupRuntime( Serial owner )
		{
			RuntimeDungeonState state;
			if ( m_Runtimes.TryGetValue( owner, out state ) )
			{
				state.Delete();
				m_Runtimes.Remove( owner );
			}
		}

		private void ClearPoolMap( Map map, Instance inst )
		{
			ClearPoolMaps( map, inst, null );
		}

		// Clears a pool map (null: every pool map not in keep); player corpses and tamed pets go to their owner's return point.
		private void ClearPoolMaps( Map map, Instance inst, HashSet<int> keep )
		{
			List<Item> items = new List<Item>();
			foreach ( Item item in World.Items.Values )
			{
				if ( item != null && !item.Deleted && item.Parent == null && IsClearTarget( item.Map, map, keep ) )
					items.Add( item );
			}

			List<Mobile> mobiles = new List<Mobile>();
			foreach ( Mobile m in World.Mobiles.Values )
			{
				if ( m != null && !m.Deleted && !m.Player && IsClearTarget( m.Map, map, keep ) )
					mobiles.Add( m );
			}

			Map returnMap;
			Point3D returnPoint;

			for ( int i = 0; i < items.Count; i++ )
			{
				Item item = items[i];
				if ( item.Deleted )
					continue;

				Corpse corpse = item as Corpse;
				if ( corpse != null && corpse.Owner != null && corpse.Owner.Player && corpse.TotalItems > 0 )
				{
					GetReturnLocationFor( inst, corpse.Owner, out returnMap, out returnPoint );
					RemoveTrackedInstanceItem( inst, corpse );
					corpse.MoveToWorld( returnPoint, returnMap );
					continue;
				}

				item.Delete();
			}

			for ( int i = 0; i < mobiles.Count; i++ )
			{
				Mobile m = mobiles[i];
				if ( m.Deleted )
					continue;

				BaseCreature pet = m as BaseCreature;
				if ( pet != null && pet.Controlled && !pet.Summoned && pet.ControlMaster != null )
				{
					GetReturnLocationFor( inst, pet.ControlMaster, out returnMap, out returnPoint );
					pet.ControlTarget = null;
					pet.ControlOrder = OrderType.Stay;
					pet.MoveToWorld( returnPoint, returnMap );
					continue;
				}

				m.Delete();
			}
		}

		private bool IsClearTarget( Map itemMap, Map map, HashSet<int> keep )
		{
			if ( itemMap == null )
				return false;

			if ( map != null )
				return Object.ReferenceEquals( itemMap, map );

			return IsPoolMap( itemMap ) && ( keep == null || !keep.Contains( itemMap.MapIndex ) );
		}

		internal sealed class TeleportLink
		{
			public Map SourceMap;
			public Point3D Location;
			public Map DestMap;
			public Point3D Dest;
			public bool ViaDoor;
		}

		// One world scan of teleporters and dungeon regions, shared by the whole catalog build.
		internal sealed class CatalogIndex
		{
			private readonly Dictionary<int, List<TeleportLink>> m_LinksBySourceMap = new Dictionary<int, List<TeleportLink>>();
			private readonly List<TeleportLink> m_AllLinks = new List<TeleportLink>();
			private readonly Dictionary<int, List<BaseRegion>> m_RegionsByMap = new Dictionary<int, List<BaseRegion>>();
			private readonly Dictionary<int, List<BaseRegion>> m_ChambersByMap = new Dictionary<int, List<BaseRegion>>();
			private static readonly List<TeleportLink> NoLinks = new List<TeleportLink>();

			public List<TeleportLink> AllLinks { get { return m_AllLinks; } }

			public static CatalogIndex Build()
			{
				CatalogIndex index = new CatalogIndex();

				foreach ( Item item in World.Items.Values )
				{
					if ( item == null || item.Deleted || item.Parent != null || item.Map == null || item.Map == Map.Internal || Instance.IsPoolMap( item.Map ) )
						continue;

					Map destinationMap;
					Point3D destination;
					if ( TryGetTeleportDestination( item, out destinationMap, out destination ) && destination != Point3D.Zero )
						index.AddLink( item.Map, item.Location, destinationMap, destination, item is ThruDoor );
				}

				Map[] bases = GetCloneBaseMaps();
				for ( int i = 0; i < bases.Length; i++ )
				{
					List<DecorationTeleporterSource> sources = GetDecorationTeleporterSources( bases[i] );
					for ( int j = 0; sources != null && j < sources.Count; j++ )
						index.AddLink( sources[j].SourceMap, sources[j].Location, sources[j].DestinationMap, sources[j].Destination, false );
				}

				foreach ( Region region in Region.Regions )
				{
					BaseRegion br = region as BaseRegion;
					if ( br == null || br.Map == null || br.Map == Map.Internal || br.Area == null || br.Area.Length == 0 || Instance.IsPoolMap( br.Map ) )
						continue;

					Dictionary<int, List<BaseRegion>> byMap;
					if ( IsDungeonCatalogRegion( br ) )
						byMap = index.m_RegionsByMap;
					else if ( br is PublicRegion )
						byMap = index.m_ChambersByMap;
					else
						continue;

					List<BaseRegion> list;
					if ( !byMap.TryGetValue( br.Map.MapIndex, out list ) )
						byMap[br.Map.MapIndex] = list = new List<BaseRegion>();

					list.Add( br );
				}

				return index;
			}

			private void AddLink( Map sourceMap, Point3D location, Map destinationMap, Point3D destination, bool viaDoor )
			{
				TeleportLink link = new TeleportLink();
				link.ViaDoor = viaDoor;
				link.SourceMap = sourceMap;
				link.Location = location;
				link.DestMap = ResolveDestinationMap( sourceMap, destinationMap );
				link.Dest = destination;

				List<TeleportLink> list;
				if ( !m_LinksBySourceMap.TryGetValue( sourceMap.MapIndex, out list ) )
					m_LinksBySourceMap[sourceMap.MapIndex] = list = new List<TeleportLink>();

				list.Add( link );
				m_AllLinks.Add( link );
			}

			public List<TeleportLink> LinksFrom( Map map )
			{
				List<TeleportLink> list;
				return m_LinksBySourceMap.TryGetValue( map.MapIndex, out list ) ? list : NoLinks;
			}

			// A magic door (ThruDoor) may also lead into a public chamber: a quest or guardian room.
			public BaseRegion FindRegion( Map map, Point3D p, bool viaDoor )
			{
				BaseRegion best = FindIn( m_RegionsByMap, map, p );
				return best == null && viaDoor ? FindIn( m_ChambersByMap, map, p ) : best;
			}

			private static BaseRegion FindIn( Dictionary<int, List<BaseRegion>> byMap, Map map, Point3D p )
			{
				List<BaseRegion> list;
				if ( !byMap.TryGetValue( map.MapIndex, out list ) )
					return null;

				BaseRegion best = null;
				for ( int i = 0; i < list.Count; i++ )
				{
					if ( list[i].Contains( p ) && ( best == null || list[i].Priority > best.Priority ) )
						best = list[i];
				}

				return best;
			}
		}

		public sealed class DungeonInstanceDefinition
		{
			private int m_Index;
			private readonly string m_Name;
			private readonly string m_DisplayName;
			private readonly Map m_Map;
			private readonly string m_MapName;
			private readonly Point3D m_Landing;
			private readonly Rectangle3D[] m_Area;
			private readonly int m_Priority;
			private readonly SpawnZLevel m_SpawnZLevel;
			private readonly SpawnEntry[] m_Spawns;
			private readonly BaseRegion[] m_ConnectedRegions;
			private readonly Rectangle3D[][] m_RegionAreas;
			private readonly bool m_WalkOutExits;

			public bool WalkOutExits { get { return m_WalkOutExits; } }

			public BaseRegion[] ConnectedRegions { get { return m_ConnectedRegions; } }
			public int Index { get { return m_Index; } set { m_Index = value; } }
			public string Name { get { return m_Name; } }
			public string DisplayName { get { return m_DisplayName; } }
			public Map Map { get { return m_Map; } }
			public string MapName { get { return m_MapName; } }
			public Point3D Landing { get { return m_Landing; } }
			public Rectangle3D[] Area { get { return m_Area; } }
			public int Priority { get { return m_Priority; } }
			public SpawnZLevel SpawnZLevel { get { return m_SpawnZLevel; } }
			public SpawnEntry[] Spawns { get { return m_Spawns; } }
			public int SpawnCount { get { return m_Spawns == null ? 0 : m_Spawns.Length; } }
			public DungeonInstanceAvailability Availability { get { return DungeonInstanceType.GetDefinitionAvailability( m_DisplayName ); } }
			public string AvailabilityLabel { get { return DungeonInstanceType.GetAvailabilityLabel( Availability ); } }
			public string InstanceBlockReason { get { return DungeonInstanceType.GetAvailabilityReason( Availability ); } }
			public bool CanSpawnInstance { get { return Availability == DungeonInstanceAvailability.Allowed; } }

			// The region's rectangles plus pads for its teleporters just outside them.
			public Rectangle3D[] GetRegionArea( int index )
			{
				return (Rectangle3D[])m_RegionAreas[index].Clone();
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

			public bool Contains( Point3D loc )
			{
				return Contains( m_Area, loc, 0 );
			}

			public bool ContainsFunctionalItem( Point3D loc )
			{
				return Contains( m_Area, loc, FunctionalItemMargin );
			}

			internal DungeonInstanceDefinition( BaseRegion region, CatalogIndex catalog )
			{
				m_Name = region.Name != null ? region.Name : region.ToString();
				m_Map = region.Map;
				m_MapName = m_Map != null && m_Map.Name != null ? m_Map.Name : "Unknown";
				m_DisplayName = String.Format( "{0}: {1}", m_MapName, m_Name );

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
				m_Landing = GetLandingFor( region, catalog, m_Area );
				m_WalkOutExits = !HasTeleporterExit( catalog, m_Map, m_Area );
				m_Priority = region.Priority;
				m_SpawnZLevel = region.SpawnZLevel;
				m_Spawns = BuildSpawnsForConnectedRegions( m_ConnectedRegions );
			}

			// The root region plus the dungeon-type regions its teleporters lead into; any other destination is an exit.
			private static void BuildCloneArea( BaseRegion root, CatalogIndex catalog, out List<BaseRegion> connected, out List<List<Rectangle3D>> regionAreas )
			{
				connected = new List<BaseRegion>();
				regionAreas = new List<List<Rectangle3D>>();
				List<Rectangle3D> all = new List<Rectangle3D>();

				connected.Add( root );
				regionAreas.Add( new List<Rectangle3D>( root.Area ) );
				all.AddRange( root.Area );

				List<TeleportLink> links = catalog.LinksFrom( root.Map );
				bool changed = true;
				int pass = 0;

				while ( changed && pass++ < 16 )
				{
					changed = false;

					for ( int i = 0; i < links.Count; i++ )
					{
						TeleportLink link = links[i];
						if ( !Object.ReferenceEquals( link.DestMap, root.Map ) || !Contains( all, link.Location, FunctionalItemMargin ) )
							continue;

						BaseRegion destination = catalog.FindRegion( root.Map, link.Dest, link.ViaDoor );
						if ( destination == null || !ShouldAddConnectedRegion( root, destination ) || connected.Contains( destination ) )
							continue;

						connected.Add( destination );
						regionAreas.Add( new List<Rectangle3D>( destination.Area ) );
						all.AddRange( destination.Area );
						changed = true;
					}
				}

				// A teleporter into the dungeon just outside its rectangles is a stair the region data missed.
				for ( int i = 0; i < links.Count; i++ )
				{
					TeleportLink link = links[i];
					if ( Contains( all, link.Location, 0 ) || !Contains( all, link.Location, FunctionalItemMargin ) )
						continue;

					if ( !Object.ReferenceEquals( link.DestMap, root.Map ) || !Contains( all, link.Dest, 0 ) )
						continue;

					Rectangle3D pad = new Rectangle3D(
						new Point3D( link.Location.X - 1, link.Location.Y - 1, Region.MinZ ),
						new Point3D( link.Location.X + 2, link.Location.Y + 2, Region.MaxZ ) );

					regionAreas[NearestRegion( regionAreas, link.Location )].Add( pad );
				}
			}

			private static bool HasTeleporterExit( CatalogIndex catalog, Map map, Rectangle3D[] area )
			{
				List<TeleportLink> links = catalog.LinksFrom( map );
				for ( int i = 0; i < links.Count; i++ )
				{
					TeleportLink link = links[i];
					if ( !Contains( area, link.Location, 0 ) )
						continue;

					if ( !Object.ReferenceEquals( link.DestMap, map ) || !Contains( area, link.Dest, 0 ) )
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
					for ( int j = 0; j < regionAreas[i].Count; j++ )
					{
						Rectangle3D r = regionAreas[i][j];
						int dx = Math.Max( 0, Math.Max( r.Start.X - p.X, p.X - ( r.End.X - 1 ) ) );
						int dy = Math.Max( 0, Math.Max( r.Start.Y - p.Y, p.Y - ( r.End.Y - 1 ) ) );
						int distance = dx + dy;

						if ( distance < bestDistance )
						{
							bestDistance = distance;
							best = i;
						}
					}
				}

				return best;
			}

			private static SpawnEntry[] BuildSpawnsForConnectedRegions( BaseRegion[] connectedRegions )
			{
				List<SpawnEntry> spawns = new List<SpawnEntry>();

				for ( int r = 0; r < connectedRegions.Length; r++ )
				{
					SpawnEntry[] entries = connectedRegions[r].Spawns;
					if ( entries != null )
						spawns.AddRange( entries );
				}

				return spawns.ToArray();
			}

			private static bool ShouldAddConnectedRegion( BaseRegion root, BaseRegion destinationRegion )
			{
				if ( Object.ReferenceEquals( root, destinationRegion ) )
					return true;

				return !( destinationRegion.Priority < root.Priority && AreasIntersect( root.Area, destinationRegion.Area ) );
			}

			// Prefers where real entrances drop players, then the recorded entrance, go location, any internal destination,
			// any standable tile.
			private static Point3D GetLandingFor( BaseRegion region, CatalogIndex catalog, Rectangle3D[] area )
			{
				Map map = region.Map;
				Point3D standable;

				List<TeleportLink> links = catalog.AllLinks;
				for ( int i = 0; i < links.Count; i++ )
				{
					TeleportLink link = links[i];
					if ( !Object.ReferenceEquals( link.DestMap, map ) || !region.Contains( link.Dest ) )
						continue;

					if ( Object.ReferenceEquals( link.SourceMap, map ) && Contains( area, link.Location, 0 ) )
						continue; // a teleporter inside the dungeon, not an entrance

					if ( TryStandable( map, area, link.Dest, out standable ) )
						return standable;
				}

				DungeonRegion dungeon = region as DungeonRegion;
				if ( dungeon != null && Object.ReferenceEquals( dungeon.EntranceMap, map ) && TryStandable( map, area, dungeon.EntranceLocation, out standable ) )
					return standable;

				if ( TryStandable( map, area, region.GoLocation, out standable ) )
					return standable;

				for ( int i = 0; i < links.Count; i++ )
				{
					if ( Object.ReferenceEquals( links[i].DestMap, map ) && TryStandable( map, area, links[i].Dest, out standable ) )
						return standable;
				}

				for ( int i = 0; i < region.Area.Length; i++ )
				{
					if ( FindStandableIn( map, area, region.Area[i], out standable ) )
						return standable;
				}

				return region.GoLocation;
			}

			private static bool TryStandable( Map map, Rectangle3D[] area, Point3D p, out Point3D standable )
			{
				standable = p;

				if ( p == Point3D.Zero || !Contains( area, p, 0 ) )
					return false;

				if ( map.CanFit( p.X, p.Y, p.Z, 16, false, false, true ) )
					return true;

				int z;
				if ( FindStandableZ( map, p.X, p.Y, p.Z, out z ) )
				{
					standable = new Point3D( p.X, p.Y, z );
					return Contains( area, standable, 0 );
				}

				return false;
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
							if ( Math.Max( Math.Abs( x - cx ), Math.Abs( y - cy ) ) != radius )
								continue;

							int z;
							if ( FindStandableZ( map, x, y, rect.Start.Z, out z ) && Contains( area, new Point3D( x, y, z ), 0 ) )
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

				StaticTile[] tiles = map.Tiles.GetStaticTiles( x, y, true );
				for ( int i = 0; i < tiles.Length; i++ )
				{
					ItemData data = TileData.ItemTable[tiles[i].ID & TileData.MaxItemValue];
					if ( data.Surface )
						candidates.Add( tiles[i].Z + data.CalcHeight );
				}

				candidates.Sort( delegate( int a, int b ) { return Math.Abs( a - preferZ ).CompareTo( Math.Abs( b - preferZ ) ); } );

				for ( int i = 0; i < candidates.Count; i++ )
				{
					if ( map.CanFit( x, y, candidates[i], 16, false, false, true ) )
					{
						z = candidates[i];
						return true;
					}
				}

				z = preferZ;
				return false;
			}

			private static bool Contains( List<Rectangle3D> areas, Point3D location, int margin )
			{
				for ( int i = 0; i < areas.Count; i++ )
				{
					if ( Contains( areas[i], location, margin ) )
						return true;
				}

				return false;
			}

			private static bool Contains( Rectangle3D[] areas, Point3D location, int margin )
			{
				for ( int i = 0; i < areas.Length; i++ )
				{
					if ( Contains( areas[i], location, margin ) )
						return true;
				}

				return false;
			}

			private static bool Contains( Rectangle3D area, Point3D location, int margin )
			{
				if ( margin <= 0 )
					return area.Contains( location );

				return location.X >= area.Start.X - margin && location.X < area.End.X + margin
					&& location.Y >= area.Start.Y - margin && location.Y < area.End.Y + margin
					&& location.Z >= area.Start.Z - margin && location.Z < area.End.Z + margin;
			}

			private static bool AreasIntersect( Rectangle3D[] a, Rectangle3D[] b )
			{
				for ( int i = 0; i < a.Length; i++ )
				{
					for ( int j = 0; j < b.Length; j++ )
					{
						if ( a[i].Start.X < b[j].End.X && a[i].End.X > b[j].Start.X && a[i].Start.Y < b[j].End.Y && a[i].End.Y > b[j].Start.Y )
							return true;
					}
				}

				return false;
			}
		}

			private sealed class DecorationTeleporterSource
			{
				public readonly Map SourceMap;
				public readonly Type Type;
				public readonly int ItemID;
				public readonly Point3D Location;
				public readonly Map DestinationMap;
				public readonly Point3D Destination;
				private readonly string[] m_Parameters;

				public DecorationTeleporterSource( Map sourceMap, Type type, int itemID, string[] parameters, Point3D location )
				{
					SourceMap = sourceMap;
					Type = type;
					ItemID = itemID;
					Location = location;
					m_Parameters = parameters != null ? parameters : EmptyStringArray;

					Map destinationMap;
					Point3D destination;
					ReadDestination( m_Parameters, out destinationMap, out destination );

					DestinationMap = destinationMap;
					Destination = destination;
				}

				public Item Construct()
				{
					Item item;

					try
					{
						item = (Item) Activator.CreateInstance( Type );
					}
					catch
					{
						return null;
					}

					if ( item == null )
						return null;

					if ( ItemID > 0 )
						item.ItemID = ItemID;

					for ( int i = 0; i < m_Parameters.Length; i++ )
					{
						try
						{
							ApplyParameter( item, m_Parameters[i] );
						}
						catch
						{
						}
					}

					item.Movable = false;
					item.Weight = -2;

					return item;
				}

				public Item ConstructExitTeleporter()
				{
					return new DungeonInstanceExitTeleporter( ItemID );
				}

				private static void ReadDestination( string[] parameters, out Map destinationMap, out Point3D destination )
				{
					destinationMap = null;
					destination = Point3D.Zero;

					for ( int i = 0; i < parameters.Length; i++ )
					{
						string name;
						string value;
						if ( !TrySplitDecorationParameter( parameters[i], out name, out value ) )
							continue;

						if ( String.Equals( name, "PointDest", StringComparison.OrdinalIgnoreCase ) )
							destination = ParseDecorationPoint( value );
						else if ( String.Equals( name, "MapDest", StringComparison.OrdinalIgnoreCase ) )
							destinationMap = ParseDecorationMap( value );
					}
				}

				private static void ApplyParameter( Item item, string parameter )
				{
					string name;
					string value;
					if ( !TrySplitDecorationParameter( parameter, out name, out value ) )
						return;

					QuestTransporter transporter = item as QuestTransporter;
					if ( transporter != null && ApplyQuestTransporterParameter( transporter, name, value ) )
						return;

					SkillTeleporter skill = item as SkillTeleporter;
					if ( skill != null && ApplySkillTeleporterParameter( skill, name, value ) )
						return;

					KeywordTeleporter keyword = item as KeywordTeleporter;
					if ( keyword != null && ApplyKeywordTeleporterParameter( keyword, name, value ) )
						return;

					Teleporter teleporter = item as Teleporter;
					if ( teleporter != null && ApplyTeleporterParameter( teleporter, name, value ) )
						return;

					moongates gate = item as moongates;
					if ( gate != null && ApplyMoongateParameter( gate, name, value ) )
						return;

					QuestTeleporter quest = item as QuestTeleporter;
					if ( quest != null && ApplyQuestTeleporterParameter( quest, name, value ) )
						return;

					ApplyItemParameter( item, name, value );
				}

				private static bool ApplyQuestTransporterParameter( QuestTransporter transporter, string name, string value )
				{
					if ( String.Equals( name, "TeleportName", StringComparison.OrdinalIgnoreCase ) )
					{
						transporter.TeleportName = value;
						return true;
					}

					if ( String.Equals( name, "Required", StringComparison.OrdinalIgnoreCase ) )
					{
						transporter.Required = value;
						return true;
					}

					if ( String.Equals( name, "MessageString", StringComparison.OrdinalIgnoreCase ) )
					{
						transporter.MessageString = value;
						return true;
					}

					return false;
				}

				private static bool ApplySkillTeleporterParameter( SkillTeleporter skill, string name, string value )
				{
					if ( String.Equals( name, "Skill", StringComparison.OrdinalIgnoreCase ) )
					{
						skill.Skill = (SkillName) Enum.Parse( typeof( SkillName ), value, true );
						return true;
					}

					if ( String.Equals( name, "RequiredFixedPoint", StringComparison.OrdinalIgnoreCase ) )
					{
						skill.Required = Utility.ToInt32( value ) * 0.01;
						return true;
					}

					if ( String.Equals( name, "Required", StringComparison.OrdinalIgnoreCase ) )
					{
						skill.Required = Utility.ToDouble( value );
						return true;
					}

					if ( String.Equals( name, "MessageString", StringComparison.OrdinalIgnoreCase ) )
					{
						skill.MessageString = value;
						return true;
					}

					if ( String.Equals( name, "MessageNumber", StringComparison.OrdinalIgnoreCase ) )
					{
						skill.MessageNumber = Utility.ToInt32( value );
						return true;
					}

					return false;
				}

				private static bool ApplyKeywordTeleporterParameter( KeywordTeleporter keyword, string name, string value )
				{
					if ( String.Equals( name, "Substring", StringComparison.OrdinalIgnoreCase ) )
					{
						keyword.Substring = value;
						return true;
					}

					if ( String.Equals( name, "Keyword", StringComparison.OrdinalIgnoreCase ) )
					{
						keyword.Keyword = Utility.ToInt32( value );
						return true;
					}

					if ( String.Equals( name, "Range", StringComparison.OrdinalIgnoreCase ) )
					{
						keyword.Range = Utility.ToInt32( value );
						return true;
					}

					return false;
				}

				private static bool ApplyTeleporterParameter( Teleporter teleporter, string name, string value )
				{
					if ( String.Equals( name, "PointDest", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.PointDest = ParseDecorationPoint( value );
						return true;
					}

					if ( String.Equals( name, "MapDest", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.MapDest = ParseDecorationMap( value );
						return true;
					}

					if ( String.Equals( name, "Creatures", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.Creatures = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "SourceEffect", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.SourceEffect = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "DestEffect", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.DestEffect = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "SoundID", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.SoundID = Utility.ToInt32( value );
						return true;
					}

					if ( String.Equals( name, "Delay", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.Delay = TimeSpan.Parse( value );
						return true;
					}

					if ( String.Equals( name, "Active", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.Active = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "CombatCheck", StringComparison.OrdinalIgnoreCase ) )
					{
						teleporter.CombatCheck = Utility.ToBoolean( value );
						return true;
					}

					return false;
				}

				private static bool ApplyMoongateParameter( moongates gate, string name, string value )
				{
					if ( String.Equals( name, "PointDest", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.PointDest = ParseDecorationPoint( value );
						return true;
					}

					if ( String.Equals( name, "MapDest", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.MapDest = ParseDecorationMap( value );
						return true;
					}

					if ( String.Equals( name, "Creatures", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.Creatures = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "SourceEffect", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.SourceEffect = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "DestEffect", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.DestEffect = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "SoundID", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.SoundID = Utility.ToInt32( value );
						return true;
					}

					if ( String.Equals( name, "Delay", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.Delay = TimeSpan.Parse( value );
						return true;
					}

					if ( String.Equals( name, "Active", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.Active = Utility.ToBoolean( value );
						return true;
					}

					if ( String.Equals( name, "CombatCheck", StringComparison.OrdinalIgnoreCase ) )
					{
						gate.CombatCheck = Utility.ToBoolean( value );
						return true;
					}

					return false;
				}

				private static bool ApplyQuestTeleporterParameter( QuestTeleporter quest, string name, string value )
				{
					if ( String.Equals( name, "PointDest", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterPointDest = ParseDecorationPoint( value );
						return true;
					}

					if ( String.Equals( name, "MapDest", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterMapDest = ParseDecorationMap( value );
						return true;
					}

					if ( String.Equals( name, "TeleporterOpen", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Open", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterOpen = Utility.ToInt32( value );
						return true;
					}

					if ( String.Equals( name, "TeleporterSound", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Sound", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterSound = Utility.ToInt32( value );
						return true;
					}

					if ( String.Equals( name, "TeleporterItem", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Item", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterItem = Utility.ToInt32( value );
						return true;
					}

					if ( String.Equals( name, "TeleporterMessage", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Message", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterMessage = value;
						return true;
					}

					if ( String.Equals( name, "TeleporterFail", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Fail", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterFail = value;
						return true;
					}

					if ( String.Equals( name, "TeleporterQuest", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Quest", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterQuest = value;
						return true;
					}

					if ( String.Equals( name, "TeleporterLock", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_Lock", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterLock = value;
						return true;
					}

					if ( String.Equals( name, "TeleporterLockMsg", StringComparison.OrdinalIgnoreCase ) || String.Equals( name, "Teleporter_LockMsg", StringComparison.OrdinalIgnoreCase ) )
					{
						quest.TeleporterLockMsg = value;
						return true;
					}

					return false;
				}

				private static void ApplyItemParameter( Item item, string name, string value )
				{
					if ( String.Equals( name, "Light", StringComparison.OrdinalIgnoreCase ) )
					{
						item.Light = (LightType) Enum.Parse( typeof( LightType ), value, true );
					}
					else if ( String.Equals( name, "Hue", StringComparison.OrdinalIgnoreCase ) )
					{
						item.Hue = Utility.ToInt32( value );
					}
					else if ( String.Equals( name, "Name", StringComparison.OrdinalIgnoreCase ) )
					{
						item.Name = value;
					}
					else if ( String.Equals( name, "Visible", StringComparison.OrdinalIgnoreCase ) )
					{
						item.Visible = Utility.ToBoolean( value );
					}
					else if ( String.Equals( name, "Movable", StringComparison.OrdinalIgnoreCase ) )
					{
						item.Movable = Utility.ToBoolean( value );
					}
					else if ( String.Equals( name, "Amount", StringComparison.OrdinalIgnoreCase ) )
					{
						item.Amount = Utility.ToInt32( value );
					}
				}
			}

			private sealed class DungeonInstanceSettings
			{
				public readonly DungeonInstanceDefinition Definition;
			public readonly Difficulty Difficulty;
			public readonly Map ReturnMap;
			public readonly Point3D ReturnPoint;

			// Zero for an untimed run.
			public readonly TimeSpan TimeLimit;

			public DungeonInstanceSettings( DungeonInstanceDefinition definition, Difficulty difficulty )
				: this( definition, difficulty, null, Point3D.Zero )
			{
			}

			public DungeonInstanceSettings( DungeonInstanceDefinition definition, Difficulty difficulty, Map returnMap, Point3D returnPoint )
				: this( definition, difficulty, returnMap, returnPoint, TimeSpan.Zero )
			{
			}

			public DungeonInstanceSettings( DungeonInstanceDefinition definition, Difficulty difficulty, Map returnMap, Point3D returnPoint, TimeSpan timeLimit )
			{
				Definition = definition;
				Difficulty = ClampDifficulty( difficulty );
				ReturnMap = NormalizeExternalMap( returnMap );
				ReturnPoint = returnPoint;
				TimeLimit = timeLimit > TimeSpan.Zero ? timeLimit : TimeSpan.Zero;
			}

			public bool Timed { get { return TimeLimit > TimeSpan.Zero; } }

			public bool Matches( DungeonInstanceSettings other )
			{
				return other != null && Object.ReferenceEquals( Definition, other.Definition ) && Difficulty == other.Difficulty && TimeLimit == other.TimeLimit;
			}
		}

		private sealed class DungeonReturnInfo
		{
			public readonly Serial InstanceKey;
			public readonly InstanceOwnerKind OwnerKind;
			public readonly Map ReturnMap;
			public readonly Point3D ReturnPoint;
			public readonly DateTime LastTouched;

			public DungeonReturnInfo( Serial instanceKey, InstanceOwnerKind ownerKind, Map returnMap, Point3D returnPoint, DateTime lastTouched )
			{
				InstanceKey = instanceKey;
				OwnerKind = ownerKind;
				ReturnMap = returnMap;
				ReturnPoint = returnPoint;
				LastTouched = lastTouched;
			}
		}

		// Read in full before anything is restored, so one bad record can't misalign the rest.
		private sealed class SavedDungeonInstance
		{
			public Serial Key;
			public InstanceOwnerKind OwnerKind;
			public int MapIndex;
			public string DefinitionName;
			public Difficulty Difficulty;
			public Map ReturnMap;
			public Point3D ReturnPoint;
			public TimeSpan TimeLimit;
			public readonly List<Serial> Members = new List<Serial>();
			public readonly List<InstanceItem> Items = new List<InstanceItem>();
			public int KilledPoints;
			public bool Cleared;
			public readonly Dictionary<Mobile, int> Monsters = new Dictionary<Mobile, int>();
			public TimeSpan TimeRemaining;
			public string BossTypeName;
			public int BossPhase;
			public Mobile Boss;

			public static SavedDungeonInstance Read( GenericReader reader, int version )
			{
				SavedDungeonInstance s = new SavedDungeonInstance();

				s.Key = (Serial)reader.ReadInt();
				s.OwnerKind = (InstanceOwnerKind)reader.ReadInt();
				s.MapIndex = reader.ReadInt();
				s.DefinitionName = reader.ReadString();
				s.Difficulty = (Difficulty)reader.ReadInt();
				s.ReturnMap = NormalizeExternalMap( reader.ReadMap() );
				s.ReturnPoint = reader.ReadPoint3D();

				if ( version >= 1 )
					s.TimeLimit = reader.ReadTimeSpan();

				int members = reader.ReadInt();
				for ( int i = 0; i < members; i++ )
					s.Members.Add( (Serial)reader.ReadInt() );

				int items = reader.ReadInt();
				for ( int i = 0; i < items; i++ )
				{
					Item item = reader.ReadItem();
					Point3D loc = reader.ReadPoint3D();
					if ( item != null && !item.Deleted )
						s.Items.Add( new InstanceItem( item, loc ) );
				}

				s.KilledPoints = reader.ReadInt();
				s.Cleared = reader.ReadBool();

				int monsters = reader.ReadInt();
				for ( int i = 0; i < monsters; i++ )
				{
					Mobile m = reader.ReadMobile();
					int points = reader.ReadInt();
					if ( m != null && !m.Deleted )
						s.Monsters[m] = points;
				}

				if ( version >= 1 )
					s.TimeRemaining = reader.ReadTimeSpan();

				if ( version >= 2 )
				{
					s.BossTypeName = reader.ReadString();
					s.BossPhase = reader.ReadInt();
					s.Boss = reader.ReadMobile();
				}

				return s;
			}
		}

		private sealed class RuntimeDungeonState
		{
			private readonly DungeonInstanceSettings m_Settings;
			private readonly List<BaseRegion> m_Regions;
			private readonly Map m_Map;
			private readonly Rectangle3D[] m_Area;
			private readonly HashSet<Serial> m_Scaled = new HashSet<Serial>();
			private readonly List<Mobile> m_Mobiles = new List<Mobile>();

			// Every monster spawned with the instance and its point value. A kill moves its points to
			// m_KilledPoints; a monster gone any other way (tamed, deleted, left the map) leaves the total.
			private readonly Dictionary<Mobile, int> m_Monsters = new Dictionary<Mobile, int>();
			private int m_KilledPoints;
			private int m_RemainingPoints;
			private bool m_Cleared;
			private Timer m_Timer;
			private bool m_Deleted;

			// Killing this share of the points wakes the boss; killing the boss clears the run.
			private const int BossThresholdPercent = 70;

			private enum BossPhase { Dormant, Awake, Slain }

			private BossPhase m_BossPhase;
			private BaseCreature m_Boss;
			private Type m_BossType;
			private bool m_BossPending;

			private static readonly TimeSpan[] ClockWarnings = new TimeSpan[] { TimeSpan.FromMinutes( 10 ), TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 1 ) };
			private static readonly TimeSpan MinimumRestoredTime = TimeSpan.FromMinutes( 2 );

			// A timed run counts down to m_Deadline; clearing the dungeon stops the clock at m_StoppedAt.
			private DateTime m_Deadline;
			private TimeSpan m_StoppedAt;
			private int m_NextWarning;
			private bool m_Expired;

			public int KilledPoints { get { return m_KilledPoints; } }
			public int TotalPoints { get { return m_KilledPoints + m_RemainingPoints; } }
			public bool Cleared { get { return m_Cleared; } }

			public int ProgressPercent
			{
				get
				{
					int total = TotalPoints;
					return total > 0 ? (int)( m_KilledPoints * 100L / total ) : 0;
				}
			}

			public string BossLabel
			{
				get
				{
					switch ( m_BossPhase )
					{
						case BossPhase.Awake: return m_Boss != null && !m_Boss.Deleted ? String.Format( "boss {0} up ({1}/{2} hits)", m_Boss.Name, m_Boss.Hits, m_Boss.HitsMax ) : "boss waking";
						case BossPhase.Slain: return "boss slain";
						default: return String.Format( "boss at {0}% ({1})", BossThresholdPercent, m_BossType != null ? m_BossType.Name : "none" );
					}
				}
			}

			public RuntimeDungeonState( DungeonInstanceSettings settings, List<BaseRegion> regions, Map map, Rectangle3D[] area )
			{
				m_Settings = settings;
				m_Regions = regions;
				m_Map = map;
				m_Area = area;
			}

			// A creature's difficulty, its 1-125 level after instance scaling, is its point value.
			private static int GetMonsterPoints( BaseCreature bc )
			{
				return Math.Max( 1, IntelligentAction.GetCreatureLevel( bc ) );
			}

			private static bool IsCountedMonster( BaseCreature bc )
			{
				return bc != null && !bc.Deleted && !bc.Blessed && !bc.Controlled && !bc.Summoned && bc.ControlMaster == null
					&& bc.SummonMaster == null && !( bc is BaseVendor ) && bc.AI != AIType.AI_Citizen;
			}

			// After the initial spawn and scaling.
			public void TrackMonsters()
			{
				foreach ( Mobile m in World.Mobiles.Values )
				{
					BaseCreature bc = m as BaseCreature;
					if ( bc == null || !Object.ReferenceEquals( bc.Map, m_Map ) )
						continue;

					// A premium spawner restarts itself when its creature dies.
					bc.SpawnerID = 0;

					if ( !IsCountedMonster( bc ) || m_Monsters.ContainsKey( bc ) )
						continue;

					int points = GetMonsterPoints( bc );
					m_Monsters[bc] = points;
					m_RemainingPoints += points;
				}

				ChooseBossType();
			}

			// The boss is a stronger copy of the toughest creature the dungeon spawned.
			private void ChooseBossType()
			{
				int best = -1;

				foreach ( KeyValuePair<Mobile, int> kvp in m_Monsters )
				{
					BaseCreature bc = kvp.Key as BaseCreature;
					if ( bc == null || bc.NoWalker || kvp.Value <= best || bc.GetType().GetConstructor( Type.EmptyTypes ) == null )
						continue;

					best = kvp.Value;
					m_BossType = bc.GetType();
				}
			}

			public void RestoreBoss( string typeName, int phase, Mobile boss )
			{
				m_BossPhase = Enum.IsDefined( typeof( BossPhase ), phase ) ? (BossPhase)phase : BossPhase.Dormant;
				m_Boss = boss as BaseCreature;

				if ( m_Boss != null && ( m_Boss.Deleted || !Object.ReferenceEquals( m_Boss.Map, m_Map ) ) )
					m_Boss = null;

				if ( !String.IsNullOrEmpty( typeName ) )
					m_BossType = ScriptCompiler.FindTypeByName( typeName );

				if ( m_BossType == null )
					ChooseBossType();

				// Runs saved before bosses existed counted "cleared" as every creature slain.
				if ( m_Cleared )
					m_BossPhase = BossPhase.Slain;
			}

			public void RestoreProgress( int killedPoints, bool cleared, Dictionary<Mobile, int> monsters )
			{
				m_KilledPoints = killedPoints;
				m_Cleared = cleared;

				foreach ( KeyValuePair<Mobile, int> kvp in monsters )
				{
					BaseCreature bc = kvp.Key as BaseCreature;
					if ( IsCountedMonster( bc ) && Object.ReferenceEquals( bc.Map, m_Map ) )
					{
						m_Monsters[bc] = kvp.Value;
						m_RemainingPoints += kvp.Value;
					}
				}
			}

			public bool CreditKill( BaseCreature bc )
			{
				if ( bc != null && Object.ReferenceEquals( bc, m_Boss ) )
				{
					OnBossSlain( bc );
					return true;
				}

				int points;
				if ( bc == null || !m_Monsters.TryGetValue( bc, out points ) )
					return false;

				m_Monsters.Remove( bc );
				m_RemainingPoints -= points;
				m_KilledPoints += points;

				if ( m_BossPhase == BossPhase.Dormant && !m_Expired )
				{
					Broadcast( 0x3B2, String.Format( "Slain: {0}/{1} points ({2}%). The master of this place awakens at {3}%.", m_KilledPoints, TotalPoints, ProgressPercent, BossThresholdPercent ) );
					CheckBoss( bc.Location, true );
				}

				return true;
			}

			private void PruneMonsters()
			{
				List<Mobile> gone = null;

				foreach ( KeyValuePair<Mobile, int> kvp in m_Monsters )
				{
					if ( !IsCountedMonster( kvp.Key as BaseCreature ) || !Object.ReferenceEquals( kvp.Key.Map, m_Map ) )
					{
						if ( gone == null )
							gone = new List<Mobile>();

						gone.Add( kvp.Key );
					}
				}

				if ( gone == null )
					return;

				for ( int i = 0; i < gone.Count; i++ )
				{
					m_RemainingPoints -= m_Monsters[gone[i]];
					m_Monsters.Remove( gone[i] );
				}
			}

			// ----- Boss -----

			private bool BossThresholdReached
			{
				get
				{
					int total = TotalPoints;
					return total > 0 && m_KilledPoints * 100L >= (long)total * BossThresholdPercent;
				}
			}

			private void CheckBoss( Point3D near, bool hasNear )
			{
				if ( m_Deleted || m_Expired || m_BossPending )
					return;

				if ( m_BossPhase == BossPhase.Dormant )
				{
					if ( !BossThresholdReached )
						return;

					m_BossPhase = BossPhase.Awake;
					Broadcast( 0x22, "The ground trembles. The master of this place has been roused!" );

					if ( hasNear )
						Effects.PlaySound( near, m_Map, 0x2F3 );

					ScheduleBoss( near, hasNear, TimeSpan.FromSeconds( 3.0 ) );
				}
				else if ( m_BossPhase == BossPhase.Awake && ( m_Boss == null || m_Boss.Deleted ) )
				{
					// Lost to a restart or a staff delete: bring it back.
					ScheduleBoss( near, hasNear, TimeSpan.Zero );
				}
			}

			public bool ForceBoss( Point3D near )
			{
				if ( m_Deleted || m_Expired || m_BossPending || m_BossPhase != BossPhase.Dormant )
					return false;

				m_BossPhase = BossPhase.Awake;
				Broadcast( 0x22, "The ground trembles. The master of this place has been roused!" );
				Effects.PlaySound( near, m_Map, 0x2F3 );
				ScheduleBoss( near, true, TimeSpan.FromSeconds( 3.0 ) );
				return true;
			}

			private void ScheduleBoss( Point3D near, bool hasNear, TimeSpan delay )
			{
				m_BossPending = true;

				Timer.DelayCall( delay, delegate
				{
					m_BossPending = false;

					if ( m_Deleted || m_Expired || m_BossPhase != BossPhase.Awake || ( m_Boss != null && !m_Boss.Deleted ) )
						return;

					SpawnBoss( hasNear ? near : PickBossOrigin() );
				} );
			}

			// Near a player in the dungeon, or anywhere standable when nobody is there.
			private Point3D PickBossOrigin()
			{
				foreach ( NetState ns in NetState.Instances )
				{
					Mobile m = ns.Mobile;
					if ( m != null && m.Alive && Object.ReferenceEquals( m.Map, m_Map ) && m.AccessLevel == AccessLevel.Player )
						return m.Location;
				}

				Point3D p;
				if ( m_Settings.Definition != null && m_Settings.Definition.TryGetRandomStandable( m_Map, out p ) )
					return p;

				return m_Settings.Definition != null ? m_Settings.Definition.Landing : Point3D.Zero;
			}

			private bool InArea( int x, int y )
			{
				if ( m_Area == null )
					return true;

				for ( int i = 0; i < m_Area.Length; i++ )
				{
					Rectangle3D r = m_Area[i];
					if ( x >= r.Start.X && y >= r.Start.Y && x < r.End.X && y < r.End.Y )
						return true;
				}

				return false;
			}

			// A free spot a few steps away that can be seen from where the threshold kill happened.
			private Point3D FindBossSpot( Point3D near )
			{
				for ( int i = 0; i < 30; i++ )
				{
					int x = near.X + Utility.RandomMinMax( -4, 4 );
					int y = near.Y + Utility.RandomMinMax( -4, 4 );

					if ( ( x == near.X && y == near.Y ) || !InArea( x, y ) )
						continue;

					int z = near.Z;
					if ( !m_Map.CanFit( x, y, z, 16, false, false ) )
					{
						z = m_Map.GetAverageZ( x, y );
						if ( Math.Abs( z - near.Z ) > 8 || !m_Map.CanFit( x, y, z, 16, false, false ) )
							continue;
					}

					Point3D p = new Point3D( x, y, z );
					if ( m_Map.LineOfSight( new Point3D( near.X, near.Y, near.Z + 10 ), new Point3D( x, y, z + 10 ) ) )
						return p;
				}

				return near;
			}

			private BaseCreature CreateBoss()
			{
				BaseCreature bc = null;

				if ( m_BossType != null )
				{
					try { bc = Activator.CreateInstance( m_BossType ) as BaseCreature; }
					catch { bc = null; }
				}

				return bc ?? new Daemon();
			}

			private void Empower( BaseCreature boss )
			{
				int d = Math.Max( 0, Math.Min( (int)Difficulty.Epic, (int)m_Settings.Difficulty ) );

				string name = NameList.RandomName( "demonic" );
				if ( !String.IsNullOrEmpty( name ) )
					boss.Name = name;

				boss.Title = RandomThings.RandomEvilTitle();
				boss.NameHue = 0x22;
				boss.Tamable = false;

				int statFloor = 150 + 50 * d;
				boss.RawStr = Math.Max( (int)( boss.RawStr * 1.5 ), statFloor );
				boss.RawDex = Math.Max( (int)( boss.RawDex * 1.5 ), statFloor );
				boss.RawInt = Math.Max( (int)( boss.RawInt * 1.5 ), statFloor );

				boss.HitsMaxSeed = Math.Min( 15000, Math.Max( boss.HitsMax * 3, 300 + 150 * d ) );

				double skillFloor = Math.Min( 120.0, 75.0 + 9.0 * d );
				for ( int i = 0; i < boss.Skills.Length; i++ )
				{
					Skill skill = boss.Skills[i];
					if ( skill.Base > 0.0 && skill.Base < skillFloor )
						skill.Base = skillFloor;
				}

				boss.DamageMin = Math.Max( boss.DamageMin, 8 + 3 * d );
				boss.DamageMax = Math.Max( boss.DamageMax, boss.DamageMin + 8 );
				boss.VirtualArmor = Math.Max( boss.VirtualArmor, 30 + 5 * d );

				boss.Hits = boss.HitsMax;
				boss.Stam = boss.StamMax;
				boss.Mana = boss.ManaMax;
			}

			private void SpawnBoss( Point3D near )
			{
				Point3D p = FindBossSpot( near );
				BaseCreature boss = CreateBoss();

				BaseCreature.BeefUp( boss, m_Settings.Difficulty, false );
				Empower( boss );
				boss.Home = p;
				boss.RangeHome = 10;
				boss.MoveToWorld( p, m_Map );
				MarkScaled( boss );
				m_Boss = boss;

				Effects.SendLocationParticles( EffectItem.Create( p, m_Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 2023 );
				Effects.SendLocationParticles( EffectItem.Create( p, m_Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 5052 );
				Effects.PlaySound( p, m_Map, 0x1FE );

				Mobile target = null;
				IPooledEnumerable eable = m_Map.GetMobilesInRange( p, 12 );
				try
				{
					foreach ( Mobile m in eable )
					{
						if ( m.Player && m.Alive && m.AccessLevel == AccessLevel.Player && ( target == null || m.GetDistanceToSqrt( p ) < target.GetDistanceToSqrt( p ) ) )
							target = m;
					}
				}
				finally
				{
					eable.Free();
				}

				if ( target != null )
					boss.Combatant = target;

				Broadcast( 0x22, String.Format( "{0} {1} has risen to defend this place!", boss.Name, boss.Title ) );
			}

			private void OnBossSlain( BaseCreature boss )
			{
				m_Boss = null;
				m_BossPhase = BossPhase.Slain;

				TimeSpan left = TimeRemaining( DateTime.Now );
				m_Cleared = true;
				if ( m_Settings.Timed )
					m_StoppedAt = left;

				Point3D loc = boss.Location;
				int d = Math.Max( 0, Math.Min( (int)Difficulty.Epic, (int)m_Settings.Difficulty ) );

				DungeonBossChest chest = new DungeonBossChest( Math.Min( 10, 4 + d ) );
				chest.SetOwner( boss );
				chest.MoveToWorld( loc, m_Map );

				Effects.SendLocationParticles( EffectItem.Create( loc, m_Map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 5008 );
				Effects.PlaySound( loc, m_Map, 0x1F7 );

				ScatterGoodies( loc, 7 + d );

				if ( m_Settings.Timed )
					Broadcast( 0x3B2, String.Format( "{0} {1} has fallen with {2} to spare! Its treasure is yours.", boss.Name, boss.Title, FormatTime( m_StoppedAt ) ) );
				else
					Broadcast( 0x3B2, String.Format( "{0} {1} has fallen! Its treasure is yours.", boss.Name, boss.Title ) );
			}

			// The quest villain's shower of coins and gems, kept inside the dungeon.
			private void ScatterGoodies( Point3D center, int radius )
			{
				for ( int x = -radius; x <= radius; x++ )
				{
					for ( int y = -radius; y <= radius; y++ )
					{
						if ( Math.Sqrt( x * x + y * y ) <= radius && InArea( center.X + x, center.Y + y ) )
							new GoodiesTimer( m_Map, center.X + x, center.Y + y ).Start();
					}
				}
			}

			private void Broadcast( int hue, string text )
			{
				foreach ( NetState ns in NetState.Instances )
				{
					Mobile m = ns.Mobile;
					if ( m != null && Object.ReferenceEquals( m.Map, m_Map ) )
						m.SendMessage( hue, text );
				}
			}

			// ----- Time limit -----

			public void StartClock()
			{
				if ( m_Settings.Timed )
					SetDeadline( m_Settings.TimeLimit );
			}

			public void RestoreClock( TimeSpan remaining )
			{
				if ( !m_Settings.Timed )
					return;

				if ( m_Cleared )
				{
					m_StoppedAt = remaining;
					return;
				}

				// The clock stood still while the server was down; give everyone time to log back in.
				SetDeadline( remaining > MinimumRestoredTime ? remaining : MinimumRestoredTime );
			}

			private void SetDeadline( TimeSpan remaining )
			{
				m_Deadline = DateTime.Now + remaining;
				m_NextWarning = 0;

				while ( m_NextWarning < ClockWarnings.Length && ClockWarnings[m_NextWarning] >= remaining )
					m_NextWarning++;
			}

			private TimeSpan TimeRemaining( DateTime now )
			{
				if ( !m_Settings.Timed )
					return TimeSpan.Zero;

				if ( m_Cleared )
					return m_StoppedAt;

				TimeSpan left = m_Deadline - now;
				return left > TimeSpan.Zero ? left : TimeSpan.Zero;
			}

			public string ClockLabel
			{
				get
				{
					if ( !m_Settings.Timed )
						return "untimed";

					string left = FormatTime( TimeRemaining( DateTime.Now ) );
					return m_Cleared ? String.Format( "won with {0} left", left ) : String.Format( "{0} left", left );
				}
			}

			public void SendTimeRemaining( Mobile m )
			{
				if ( m == null || !m_Settings.Timed || m_Cleared || m_Expired )
					return;

				m.SendMessage( 0x22, "You have {0} to defeat the master of this place.", FormatTime( TimeRemaining( DateTime.Now ) ) );
			}

			private static string FormatTime( TimeSpan time )
			{
				int minutes = (int)time.TotalMinutes;
				int seconds = time.Seconds;

				if ( minutes > 0 && seconds == 0 )
					return String.Format( "{0} minute{1}", minutes, minutes == 1 ? "" : "s" );

				if ( minutes > 0 )
					return String.Format( "{0}:{1:00}", minutes, seconds );

				return String.Format( "{0} second{1}", seconds, seconds == 1 ? "" : "s" );
			}

			private void CheckClock()
			{
				if ( !m_Settings.Timed || m_Cleared || m_Expired )
					return;

				TimeSpan left = TimeRemaining( DateTime.Now );

				if ( left <= TimeSpan.Zero )
				{
					m_Expired = true;
					Broadcast( 0x22, "Time has run out! The dungeon crumbles away around you." );

					Map map = m_Map;
					Timer.DelayCall( TimeSpan.Zero, delegate { DungeonInstanceType.Instance.ExpireInstance( map ); } );
					return;
				}

				if ( m_NextWarning < ClockWarnings.Length && left <= ClockWarnings[m_NextWarning] )
				{
					Broadcast( 0x22, String.Format( "{0} remain before this place crumbles away.", FormatTime( ClockWarnings[m_NextWarning] ) ) );

					while ( m_NextWarning < ClockWarnings.Length && left <= ClockWarnings[m_NextWarning] )
						m_NextWarning++;
				}
			}

			public void Serialize( GenericWriter writer )
			{
				writer.Write( (int) m_KilledPoints );
				writer.Write( (bool) m_Cleared );

				writer.Write( (int) m_Monsters.Count );
				foreach ( KeyValuePair<Mobile, int> kvp in m_Monsters )
				{
					writer.Write( (Mobile) kvp.Key );
					writer.Write( (int) kvp.Value );
				}

				writer.Write( (TimeSpan) TimeRemaining( DateTime.Now ) );

				writer.Write( (string) ( m_BossType != null ? m_BossType.Name : "" ) );
				writer.Write( (int) m_BossPhase );
				writer.Write( (Mobile) ( m_Boss != null && !m_Boss.Deleted ? m_Boss : null ) );
			}

			public void StartScaler()
			{
				if ( m_Timer != null )
					return;

				m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromSeconds( 5.0 ), new TimerCallback( OnTick ) );
			}

			private void OnTick()
			{
				ApplyDifficulty();
				PruneMonsters();
				CheckClock();
				CheckBoss( Point3D.Zero, false );
			}

			public void ApplyDifficulty()
			{
				if ( m_Map == null || m_Area == null )
					return;

				for ( int i = 0; i < m_Area.Length; i++ )
				{
					Rectangle3D rect = m_Area[i];
					Rectangle2D bounds = new Rectangle2D( rect.Start.X, rect.Start.Y, rect.Width, rect.Height );

					IPooledEnumerable eable = m_Map.GetMobilesInBounds( bounds );
					try
					{
						foreach ( Mobile m in eable )
							ScaleMobile( m );
					}
					finally
					{
						eable.Free();
					}
				}
			}

			private static bool IsScalable( BaseCreature bc )
			{
				return bc != null && !bc.Deleted && !bc.Controlled && !bc.Summoned && bc.ControlMaster == null && bc.SummonMaster == null;
			}

			private void ScaleMobile( Mobile m )
			{
				BaseCreature bc = m as BaseCreature;
				if ( !IsScalable( bc ) || m_Scaled.Contains( bc.Serial ) )
					return;

				m_Mobiles.Add( m );
				BaseCreature.BeefUp( bc, m_Settings.Difficulty, false );
				m_Scaled.Add( bc.Serial );
			}

			// For creatures that come back from a save already scaled.
			public void MarkScaled( Mobile m )
			{
				BaseCreature bc = m as BaseCreature;
				if ( IsScalable( bc ) && m_Scaled.Add( bc.Serial ) )
					m_Mobiles.Add( bc );
			}

			public void Delete()
			{
				m_Deleted = true;
				m_Boss = null;

				if ( m_Timer != null )
				{
					m_Timer.Stop();
					m_Timer = null;
				}

				// Unregistering a region deletes its spawn entries.
				for ( int i = 0; i < m_Regions.Count; i++ )
				{
					if ( m_Regions[i].Registered )
						m_Regions[i].Unregister();
				}

				for ( int i = 0; i < m_Mobiles.Count; i++ )
				{
					Mobile m = m_Mobiles[i];
					if ( m != null && !m.Deleted )
						m.Delete();
				}

				m_Mobiles.Clear();
				m_Scaled.Clear();
				m_Monsters.Clear();
			}
		}
	}
}
