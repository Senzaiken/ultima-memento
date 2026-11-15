using System;
using System.Collections.Generic;
using Server;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Instancing
{
	// The Dungeon Master's hall that a demonic rift leads to: a stone room built from items on a
	// black stretch of SerpentIsland, one copy per party. The Dungeon Master in the middle opens a
	// portal on the dais to a randomly chosen dungeon instance. Live halls survive a restart.
	public class DungeonLobbyInstanceType : InstanceType
	{
		public static readonly DungeonLobbyInstanceType Instance = new DungeonLobbyInstanceType();

		private const string LobbySavePath = "Saves/Instancing/DungeonLobbies.bin";

		// Centre of the hall. Everything within 56 tiles of it is void with no statics.
		public const int CenterX = 2424;
		public const int CenterY = 1800;
		public const int FloorZ = 0;

		// The floor runs HalfSize tiles out from the centre; the walls stand one tile further.
		private const int HalfSize = 8;

		private const int DaisOffsetY = -5;
		private const int DaisHeight = 5;
		private const int DaisHue = 0x497;

		private const int WallEastWest = 0x241;
		private const int WallNorthSouth = 0x242;
		private const int WallSoutheastPost = 0x243;
		private const int WallNorthwestCorner = 0x244;

		private readonly Dictionary<Serial, LobbyState> m_States = new Dictionary<Serial, LobbyState>();
		private readonly Dictionary<Serial, LobbyReturn> m_ReturnsByPlayer = new Dictionary<Serial, LobbyReturn>();

		private DungeonLobbyInstanceType()
		{
		}

		public static void Configure()
		{
			EventSink.WorldLoad += new WorldLoadEventHandler( Instance.OnWorldLoad );
			EventSink.WorldSave += new WorldSaveEventHandler( Instance.OnWorldSave );
		}

		// ----- Pool / terrain -----

		// Dungeons own 72-120 and index 127 is reserved, so the halls start at 128.
		public override string Key { get { return "DungeonLobby"; } }
		public override int PoolBaseIndex { get { return 128; } }
		public override int PoolSize { get { return 16; } }

		public override Map BaseMap { get { return Map.SerpentIsland; } }
		public override int MapWidth { get { return 2560; } }
		public override int MapHeight { get { return 2048; } }

		public override Point3D Landing { get { return new Point3D( CenterX, CenterY + HalfSize - 3, FloorZ ); } }
		public override Map ExitMap { get { return Map.Sosaria; } }
		public override Point3D ExitPoint { get { return new Point3D( 3884, 2879, 0 ); } }

		public override string RegionName { get { return "the Dungeon Master's Hall"; } }
		public override int RegionHalfExtent { get { return HalfSize + 2; } }

		public override Rectangle3D RegionFootprint()
		{
			return new Rectangle3D(
				new Point3D( CenterX - RegionHalfExtent, CenterY - RegionHalfExtent, sbyte.MinValue ),
				new Point3D( CenterX + RegionHalfExtent + 1, CenterY + RegionHalfExtent + 1, sbyte.MaxValue ) );
		}
		public override TimeSpan UnloadAfter { get { return TimeSpan.FromMinutes( 10 ); } }

		public static Point3D DaisTop { get { return new Point3D( CenterX, CenterY + DaisOffsetY, FloorZ + DaisHeight ); } }

		// Party members share one hall, keyed to the leader; a solo player gets their own.
		protected override Serial OwnerKey( Mobile m )
		{
			Party p = Party.Get( m );
			if ( p != null && p.Active && p.Leader != null )
				return p.Leader.Serial;

			return m.Serial;
		}

		// A hall is kept while its dungeon runs: that dungeon is keyed to the hall's portal.
		protected override bool IsInUse( Instance inst )
		{
			if ( HasPlayersInside( inst ) )
				return true;

			LobbyState state;
			if ( inst == null || !m_States.TryGetValue( inst.OwnerSerial, out state ) || state.Master == null || state.Master.Deleted )
				return false;

			Item portal = state.Master.Portal;
			return portal != null && !portal.Deleted && DungeonInstanceType.Instance.HasInstance( portal.Serial );
		}

		// ----- Entry / exit -----

		public bool HasLiveLobby( Mobile m )
		{
			Instance inst = m != null ? GetByKey( OwnerKey( m ) ) : null;
			return inst != null && inst.IsLive;
		}

		public bool Enter( Mobile from, Map returnMap, Point3D returnPoint )
		{
			if ( from == null )
				return false;

			returnMap = DungeonInstanceType.NormalizeExternalMap( returnMap );
			if ( returnMap == null )
			{
				returnMap = ExitMap;
				returnPoint = ExitPoint;
			}

			Serial key = OwnerKey( from );
			bool created = GetByKey( key ) == null;
			Instance inst = GetOrCreateByKey( key, InstanceOwnerKind.Mobile );

			LobbyState state;
			if ( !m_States.TryGetValue( key, out state ) )
				m_States[key] = state = new LobbyState( returnMap, returnPoint );

			if ( !SendToInstance( from, inst ) )
			{
				if ( created )
					FreeInstance( inst );

				return false;
			}

			m_ReturnsByPlayer[from.Serial] = new LobbyReturn( state.ReturnMap, state.ReturnPoint );
			from.SendMessage( 0x22, "The rift pulls you through into a silent hall of black stone." );
			return true;
		}

		// Where a player leaving this hall (or anything sent on from it) goes back to.
		public void GetReturn( Mobile m, out Map map, out Point3D point )
		{
			LobbyState state = m != null ? GetState( m.Map ) : null;
			LobbyReturn ret;

			if ( state != null )
			{
				map = state.ReturnMap;
				point = state.ReturnPoint;
			}
			else if ( m != null && m_ReturnsByPlayer.TryGetValue( m.Serial, out ret ) )
			{
				map = ret.Map;
				point = ret.Point;
			}
			else
			{
				map = ExitMap;
				point = ExitPoint;
			}

			if ( map == null )
			{
				map = ExitMap;
				point = ExitPoint;
			}
		}

		protected override Map GetExitMap( Mobile m )
		{
			Map map;
			Point3D point;
			GetReturn( m, out map, out point );
			return map;
		}

		protected override Point3D GetExitPoint( Mobile m )
		{
			Map map;
			Point3D point;
			GetReturn( m, out map, out point );
			return point;
		}

		public override bool LeaveInstance( Mobile m )
		{
			if ( !base.LeaveInstance( m ) )
				return false;

			m_ReturnsByPlayer.Remove( m.Serial );
			return true;
		}

		public override void OnLogin( Mobile m )
		{
			if ( m == null || !IsPoolMap( m.Map ) )
				return;

			Instance inst = GetLiveInstance( m.Map );
			if ( inst != null )
			{
				inst.Touch();
				return;
			}

			LeaveInstance( m );
		}

		public DungeonMaster GetMaster( Map map )
		{
			LobbyState state = GetState( map );
			return state != null ? state.Master : null;
		}

		private LobbyState GetState( Map map )
		{
			Instance inst = GetLiveInstance( map );
			LobbyState state;
			return inst != null && m_States.TryGetValue( inst.OwnerSerial, out state ) ? state : null;
		}

		// ----- Building -----

		protected override void BuildContents( Instance inst, Map map )
		{
			ClearMap( map, null );

			int cx = CenterX, cy = CenterY, z = FloorZ;
			int dx = cx, dy = cy + DaisOffsetY;

			for ( int x = cx - HalfSize; x <= cx + HalfSize; x++ )
			{
				for ( int y = cy - HalfSize; y <= cy + HalfSize; y++ )
				{
					bool underDais = Math.Abs( x - dx ) <= 2 && Math.Abs( y - dy ) <= 2;
					bool underPentagram = Math.Abs( x - cx ) <= 1 && Math.Abs( y - cy ) <= 1;

					if ( !underDais && !underPentagram )
						Place( inst, map, new Static( 0x500 + Utility.Random( 4 ) ), x, y, z, 0 );
				}
			}

			BuildWalls( inst, map, cx, cy, z );
			BuildDais( inst, map, dx, dy, z );
			BuildPentagram( inst, map, cx, cy, z );

			int[][] corners = new int[][] { new int[] { -1, -1 }, new int[] { 1, -1 }, new int[] { -1, 1 }, new int[] { 1, 1 } };
			for ( int i = 0; i < corners.Length; i++ )
				PlaceLight( inst, map, new BrazierTall(), cx + corners[i][0] * ( HalfSize - 1 ), cy + corners[i][1] * ( HalfSize - 1 ), z );

			PlaceLight( inst, map, new Brazier(), dx - 3, dy, z );
			PlaceLight( inst, map, new Brazier(), dx + 3, dy, z );

			DungeonLobbyExit exit = new DungeonLobbyExit();
			Place( inst, map, exit, cx, cy + HalfSize, z, -1 );

			DungeonMaster master = new DungeonMaster();
			master.Direction = Direction.South;
			master.MoveToWorld( new Point3D( cx, cy, z ), map );

			LobbyState state;
			if ( m_States.TryGetValue( inst.OwnerSerial, out state ) )
				state.Master = master;
		}

		private void BuildWalls( Instance inst, Map map, int cx, int cy, int z )
		{
			int west = cx - HalfSize - 1, east = cx + HalfSize + 1;
			int north = cy - HalfSize - 1, south = cy + HalfSize + 1;

			for ( int x = west + 1; x < east; x++ )
			{
				Place( inst, map, new Static( WallEastWest ), x, north, z, 0 );
				Place( inst, map, new Static( WallEastWest ), x, south, z, 0 );
			}

			for ( int y = north + 1; y < south; y++ )
			{
				Place( inst, map, new Static( WallNorthSouth ), west, y, z, 0 );
				Place( inst, map, new Static( WallNorthSouth ), east, y, z, 0 );
			}

			Place( inst, map, new Static( WallNorthwestCorner ), west, north, z, 0 );
			Place( inst, map, new Static( WallNorthSouth ), east, north, z, 0 );
			Place( inst, map, new Static( WallEastWest ), west, south, z, 0 );
			Place( inst, map, new Static( WallSoutheastPost ), east, south, z, 0 );
		}

		// The champion platform's shape: a 3x3 top reached by stairs on every side.
		private void BuildDais( Instance inst, Map map, int dx, int dy, int z )
		{
			for ( int x = -2; x <= 2; x++ )
				for ( int y = -2; y <= 2; y++ )
					Place( inst, map, new Static( 0x750 ), dx + x, dy + y, z - DaisHeight, DaisHue );

			for ( int x = -1; x <= 1; x++ )
				for ( int y = -1; y <= 1; y++ )
					Place( inst, map, new Static( 0x750 ), dx + x, dy + y, z, DaisHue );

			for ( int i = -1; i <= 1; i++ )
			{
				Place( inst, map, new Static( 0x751 ), dx + i, dy + 2, z, DaisHue );
				Place( inst, map, new Static( 0x752 ), dx + 2, dy + i, z, DaisHue );
				Place( inst, map, new Static( 0x753 ), dx + i, dy - 2, z, DaisHue );
				Place( inst, map, new Static( 0x754 ), dx - 2, dy + i, z, DaisHue );
			}

			Place( inst, map, new Static( 0x759 ), dx - 2, dy - 2, z, DaisHue );
			Place( inst, map, new Static( 0x75A ), dx + 2, dy + 2, z, DaisHue );
			Place( inst, map, new Static( 0x75B ), dx - 2, dy + 2, z, DaisHue );
			Place( inst, map, new Static( 0x75C ), dx + 2, dy - 2, z, DaisHue );
		}

		private void BuildPentagram( Instance inst, Map map, int cx, int cy, int z )
		{
			int[,] parts = new int[,]
			{
				{ 0xFE7, -1, -1 }, { 0xFE8, 0, -1 }, { 0xFEB, 1, -1 },
				{ 0xFE6, -1, 0 }, { 0xFEA, 0, 0 }, { 0xFEE, 1, 0 },
				{ 0xFE9, -1, 1 }, { 0xFEC, 0, 1 }, { 0xFED, 1, 1 }
			};

			for ( int i = 0; i < parts.GetLength( 0 ); i++ )
				Place( inst, map, new Static( parts[i, 0] ), cx + parts[i, 1], cy + parts[i, 2], z, 0 );
		}

		private static void PlaceLight( Instance inst, Map map, BaseLight light, int x, int y, int z )
		{
			light.Movable = false;
			Place( inst, map, light, x, y, z, -1 );
		}

		private static void Place( Instance inst, Map map, Item item, int x, int y, int z, int hue )
		{
			if ( hue >= 0 )
				item.Hue = hue;

			item.Movable = false;
			Point3D p = new Point3D( x, y, z );
			item.MoveToWorld( p, map );
			inst.Items.Add( new InstanceItem( item, p ) );
		}

		// ----- Teardown -----

		protected override void OnFreeInstance( Instance inst, Map liveMap )
		{
			LobbyState state;
			m_States.TryGetValue( inst.OwnerSerial, out state );

			if ( liveMap != null )
				ClearMap( liveMap, state );

			m_States.Remove( inst.OwnerSerial );
		}

		// Sends players, their pets and corpses home, and deletes everything else on a hall's map.
		private void ClearMap( Map map, LobbyState state )
		{
			Map returnMap = state != null ? state.ReturnMap : ExitMap;
			Point3D returnPoint = state != null ? state.ReturnPoint : ExitPoint;

			List<Mobile> mobiles = new List<Mobile>();
			foreach ( Mobile m in World.Mobiles.Values )
			{
				if ( m != null && !m.Deleted && Object.ReferenceEquals( m.Map, map ) )
					mobiles.Add( m );
			}

			List<Item> items = new List<Item>();
			foreach ( Item item in World.Items.Values )
			{
				if ( item != null && !item.Deleted && item.Parent == null && Object.ReferenceEquals( item.Map, map ) )
					items.Add( item );
			}

			for ( int i = 0; i < mobiles.Count; i++ )
			{
				Mobile m = mobiles[i];
				if ( m.Deleted )
					continue;

				if ( m.Player )
				{
					BaseCreature.TeleportPets( m, returnPoint, returnMap, false );
					m.MoveToWorld( returnPoint, returnMap );
					m_ReturnsByPlayer.Remove( m.Serial );
					continue;
				}

				BaseCreature pet = m as BaseCreature;
				if ( pet != null && pet.Controlled && !pet.Summoned && pet.ControlMaster != null )
				{
					pet.ControlTarget = null;
					pet.ControlOrder = OrderType.Stay;
					pet.MoveToWorld( returnPoint, returnMap );
					continue;
				}

				m.Delete();
			}

			for ( int i = 0; i < items.Count; i++ )
			{
				Item item = items[i];
				if ( item.Deleted )
					continue;

				Corpse corpse = item as Corpse;
				if ( corpse != null && corpse.Owner != null && corpse.Owner.Player && corpse.TotalItems > 0 )
				{
					corpse.MoveToWorld( returnPoint, returnMap );
					continue;
				}

				item.Delete();
			}
		}

		// ----- Persistence -----

		private void OnWorldSave( WorldSaveEventArgs e )
		{
			List<Instance> live = new List<Instance>();
			foreach ( Instance inst in LiveInstances )
			{
				if ( m_States.ContainsKey( inst.OwnerSerial ) )
					live.Add( inst );
			}

			Persistence.Serialize(
				LobbySavePath,
				delegate( GenericWriter writer )
				{
					writer.Write( (int) 0 ); // version

					writer.Write( (int) live.Count );
					for ( int i = 0; i < live.Count; i++ )
					{
						Instance inst = live[i];
						LobbyState state = m_States[inst.OwnerSerial];

						writer.Write( (int) inst.OwnerSerial );
						writer.Write( (int) inst.LiveMapIndex );
						writer.Write( (Map) state.ReturnMap );
						writer.Write( (Point3D) state.ReturnPoint );

						writer.Write( (int) inst.Items.Count );
						for ( int j = 0; j < inst.Items.Count; j++ )
						{
							writer.Write( (Item) inst.Items[j].Item );
							writer.Write( (Point3D) inst.Items[j].Loc );
						}
					}

					writer.Write( (int) m_ReturnsByPlayer.Count );
					foreach ( KeyValuePair<Serial, LobbyReturn> kvp in m_ReturnsByPlayer )
					{
						writer.Write( (int) kvp.Key );
						writer.Write( (Map) kvp.Value.Map );
						writer.Write( (Point3D) kvp.Value.Point );
					}
				} );
		}

		private void OnWorldLoad()
		{
			List<SavedLobby> saved = new List<SavedLobby>();
			Dictionary<Serial, LobbyReturn> returns = new Dictionary<Serial, LobbyReturn>();

			try
			{
				Persistence.Deserialize(
					LobbySavePath,
					delegate( GenericReader reader )
					{
						if ( reader.End() )
							return;

						reader.ReadInt(); // version

						int count = reader.ReadInt();
						for ( int i = 0; i < count; i++ )
							saved.Add( SavedLobby.Read( reader ) );

						int players = reader.ReadInt();
						for ( int i = 0; i < players; i++ )
						{
							Serial serial = (Serial)reader.ReadInt();
							Map map = reader.ReadMap();
							Point3D point = reader.ReadPoint3D();
							returns[serial] = new LobbyReturn( map, point );
						}
					} );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "[Instancing] Dungeon halls could not be loaded and will be cleared: {0}", ex.Message );
				saved.Clear();
				returns.Clear();
			}

			foreach ( KeyValuePair<Serial, LobbyReturn> kvp in returns )
				m_ReturnsByPlayer[kvp.Key] = kvp.Value;

			HashSet<int> restored = new HashSet<int>();
			for ( int i = 0; i < saved.Count; i++ )
			{
				if ( RestoreLobby( saved[i] ) )
					restored.Add( saved[i].MapIndex );
			}

			for ( int i = 0; i < PoolSize; i++ )
			{
				int idx = PoolBaseIndex + i;
				if ( idx < Map.Maps.Length && Map.Maps[idx] != null && !restored.Contains( idx ) )
					ClearMap( Map.Maps[idx], null );
			}

			if ( saved.Count > 0 )
				Console.WriteLine( "[Instancing] Restored {0} of {1} saved dungeon hall(s).", restored.Count, saved.Count );
		}

		private bool RestoreLobby( SavedLobby saved )
		{
			Map map = saved.MapIndex >= 0 && saved.MapIndex < Map.Maps.Length ? Map.Maps[saved.MapIndex] : null;
			if ( !IsPoolMap( map ) || IsMapIndexLive( saved.MapIndex ) || GetByKey( saved.Key ) != null )
				return false;

			DungeonMaster master = null;
			foreach ( Mobile m in World.Mobiles.Values )
			{
				if ( m is DungeonMaster && !m.Deleted && Object.ReferenceEquals( m.Map, map ) )
				{
					master = (DungeonMaster)m;
					break;
				}
			}

			if ( master == null )
				return false;

			Instance inst = GetOrCreateByKey( saved.Key, InstanceOwnerKind.Mobile );
			inst.Items.AddRange( saved.Items );
			RestoreLive( inst, saved.MapIndex );

			LobbyState state = new LobbyState( DungeonInstanceType.NormalizeExternalMap( saved.ReturnMap ), saved.ReturnPoint );
			state.Master = master;
			m_States[saved.Key] = state;
			return true;
		}

		private sealed class LobbyState
		{
			public readonly Map ReturnMap;
			public readonly Point3D ReturnPoint;
			public DungeonMaster Master;

			public LobbyState( Map returnMap, Point3D returnPoint )
			{
				ReturnMap = returnMap;
				ReturnPoint = returnPoint;
			}
		}

		private sealed class LobbyReturn
		{
			public readonly Map Map;
			public readonly Point3D Point;

			public LobbyReturn( Map map, Point3D point )
			{
				Map = map;
				Point = point;
			}
		}

		private sealed class SavedLobby
		{
			public Serial Key;
			public int MapIndex;
			public Map ReturnMap;
			public Point3D ReturnPoint;
			public readonly List<InstanceItem> Items = new List<InstanceItem>();

			public static SavedLobby Read( GenericReader reader )
			{
				SavedLobby s = new SavedLobby();
				s.Key = (Serial)reader.ReadInt();
				s.MapIndex = reader.ReadInt();
				s.ReturnMap = reader.ReadMap();
				s.ReturnPoint = reader.ReadPoint3D();

				int items = reader.ReadInt();
				for ( int i = 0; i < items; i++ )
				{
					Item item = reader.ReadItem();
					Point3D loc = reader.ReadPoint3D();
					if ( item != null && !item.Deleted )
						s.Items.Add( new InstanceItem( item, loc ) );
				}

				return s;
			}
		}
	}
}
