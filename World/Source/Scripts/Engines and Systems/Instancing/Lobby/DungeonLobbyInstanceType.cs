using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Instancing
{
	// A STONE HALL BUILT FROM ITEMS ON A BLACK STRETCH OF SERPENT ISLAND, ONE PER PARTY
	public class DungeonLobbyInstanceType : InstanceType
	{
		public static readonly DungeonLobbyInstanceType Instance = new DungeonLobbyInstanceType();

		public const int CenterX = 2424;
		public const int CenterY = 1800;
		public const int FloorZ = 0;

		private const int HalfSize = 8;
		private const int DaisOffsetY = -5;
		private const int DaisHeight = 5;
		private const int DaisHue = 0x497;

		private const int WallEastWest = 0x241;
		private const int WallNorthSouth = 0x242;
		private const int WallSoutheastPost = 0x243;
		private const int WallNorthwestCorner = 0x244;

		private DungeonLobbyInstanceType()
		{
		}

		public override string Key { get { return "DungeonLobby"; } }
		public override int PoolBaseIndex { get { return 128; } }
		public override int PoolSize { get { return 16; } }
		public override Point3D Landing { get { return new Point3D( CenterX, CenterY + HalfSize - 3, FloorZ ); } }
		public override string RegionName { get { return "the Dungeon Master's Hall"; } }
		public override TimeSpan UnloadAfter { get { return TimeSpan.FromMinutes( 10 ); } }

		public override Rectangle3D RegionFootprint()
		{
			int size = HalfSize + 2;
			return new Rectangle3D( new Point3D( CenterX - size, CenterY - size, sbyte.MinValue ), new Point3D( CenterX + size + 1, CenterY + size + 1, sbyte.MaxValue ) );
		}

		public static Point3D DaisTop { get { return new Point3D( CenterX, CenterY + DaisOffsetY, FloorZ + DaisHeight ); } }

		public override Serial OwnerKey( Mobile m )
		{
			return PartyKey( m );
		}

		public bool IsLive( Serial key )
		{
			Instance inst = GetByKey( key );
			return inst != null && inst.IsLive;
		}

		// A HALL IS KEPT WHILE THE DUNGEON BEHIND ITS PORTAL RUNS
		protected override bool IsInUse( Instance inst )
		{
			DungeonMaster master = inst.Data as DungeonMaster;
			Item portal = master != null ? master.Portal : null;
			return HasPlayersInside( inst ) || ( portal != null && !portal.Deleted && DungeonInstanceType.Instance.HasInstance( portal.Serial ) );
		}

		public bool Enter( Mobile from, Map returnMap, Point3D returnPoint )
		{
			Instance inst = GetByKey( OwnerKey( from ) );
			bool created = inst == null;

			if ( created )
			{
				inst = GetOrCreate( from );
				inst.ReturnMap = returnMap;
				inst.ReturnPoint = returnPoint;
			}

			if ( !SendToInstance( from, inst ) )
			{
				if ( created )
					FreeInstance( inst );

				return false;
			}

			from.SendMessage( 0x22, "The rift pulls you through into a silent hall of black stone." );
			return true;
		}

		protected override void OnFreeInstance( Instance inst, Map liveMap )
		{
			if ( liveMap != null )
				ClearMap( liveMap, inst );
		}

		// HALLS ARE REBUILT FROM THEIR DUNGEON MASTERS; ANY OTHER HALL MAP IS CLEARED
		public override void OnLoad()
		{
			HashSet<int> kept = new HashSet<int>();

			foreach ( Mobile m in World.Mobiles.Values )
			{
				DungeonMaster master = m as DungeonMaster;
				if ( master == null || !IsPoolMap( master.Map ) || GetByKey( master.HallKey ) != null )
					continue;

				Instance inst = GetOrCreateByKey( master.HallKey );
				inst.ReturnMap = master.ReturnMap;
				inst.ReturnPoint = master.ReturnPoint;
				inst.Data = master;

				if ( RestoreLive( inst, master.Map.MapIndex ) )
					kept.Add( master.Map.MapIndex );
				else
					FreeInstance( inst );
			}

			ClearPoolMaps( kept );
		}

		public override void OnSave()
		{
		}

		protected override void BuildContents( Instance inst, Map map )
		{
			ClearMap( map, null );

			int cx = CenterX, cy = CenterY, z = FloorZ;
			int dx = cx, dy = cy + DaisOffsetY;

			for ( int x = cx - HalfSize; x <= cx + HalfSize; x++ )
				for ( int y = cy - HalfSize; y <= cy + HalfSize; y++ )
					Place( map, new Static( 0x500 + Utility.Random( 4 ) ), x, y, z, 0 );

			BuildWalls( map, cx, cy, z );
			BuildDais( map, dx, dy, z );
			BuildPentagram( map, cx, cy, z );

			for ( int i = 0; i < 4; i++ )
				Place( map, new BrazierTall(), cx + ( i % 2 == 0 ? -1 : 1 ) * ( HalfSize - 1 ), cy + ( i < 2 ? -1 : 1 ) * ( HalfSize - 1 ), z, -1 );

			Place( map, new Brazier(), dx - 3, dy, z, -1 );
			Place( map, new Brazier(), dx + 3, dy, z, -1 );
			Place( map, new DungeonLobbyExit(), cx, cy + HalfSize, z, -1 );

			DungeonMaster master = new DungeonMaster();
			master.HallKey = inst.OwnerSerial;
			master.ReturnMap = inst.ReturnMap;
			master.ReturnPoint = inst.ReturnPoint;
			master.Direction = Direction.South;
			master.MoveToWorld( new Point3D( cx, cy, z ), map );
			inst.Data = master;
		}

		private static void BuildWalls( Map map, int cx, int cy, int z )
		{
			int west = cx - HalfSize - 1, east = cx + HalfSize + 1;
			int north = cy - HalfSize - 1, south = cy + HalfSize + 1;

			for ( int x = west + 1; x < east; x++ )
			{
				Place( map, new Static( WallEastWest ), x, north, z, 0 );
				Place( map, new Static( WallEastWest ), x, south, z, 0 );
			}

			for ( int y = north + 1; y < south; y++ )
			{
				Place( map, new Static( WallNorthSouth ), west, y, z, 0 );
				Place( map, new Static( WallNorthSouth ), east, y, z, 0 );
			}

			Place( map, new Static( WallNorthwestCorner ), west, north, z, 0 );
			Place( map, new Static( WallEastWest ), east, north, z, 0 );
			Place( map, new Static( WallNorthSouth ), east, north, z, 0 );
			Place( map, new Static( WallEastWest ), west, south, z, 0 );
			Place( map, new Static( WallNorthSouth ), west, south, z, 0 );
			Place( map, new Static( WallSoutheastPost ), east, south, z, 0 );
		}

		// THE CHAMPION PLATFORM: A 3X3 TOP WITH STAIRS ON EVERY SIDE
		private static void BuildDais( Map map, int dx, int dy, int z )
		{
			for ( int x = -1; x <= 1; x++ )
				for ( int y = -1; y <= 1; y++ )
					Place( map, new Static( 0x750 ), dx + x, dy + y, z, DaisHue );

			for ( int i = -1; i <= 1; i++ )
			{
				Place( map, new Static( 0x751 ), dx + i, dy + 2, z, DaisHue );
				Place( map, new Static( 0x752 ), dx + 2, dy + i, z, DaisHue );
				Place( map, new Static( 0x753 ), dx + i, dy - 2, z, DaisHue );
				Place( map, new Static( 0x754 ), dx - 2, dy + i, z, DaisHue );
			}

			Place( map, new Static( 0x759 ), dx - 2, dy - 2, z, DaisHue );
			Place( map, new Static( 0x75A ), dx + 2, dy + 2, z, DaisHue );
			Place( map, new Static( 0x75B ), dx - 2, dy + 2, z, DaisHue );
			Place( map, new Static( 0x75C ), dx + 2, dy - 2, z, DaisHue );
		}

		private static void BuildPentagram( Map map, int cx, int cy, int z )
		{
			int[,] parts = new int[,]
			{
				{ 0xFE7, -1, -1 }, { 0xFE8, 0, -1 }, { 0xFEB, 1, -1 },
				{ 0xFE6, -1, 0 }, { 0xFEA, 0, 0 }, { 0xFEE, 1, 0 },
				{ 0xFE9, -1, 1 }, { 0xFEC, 0, 1 }, { 0xFED, 1, 1 }
			};

			for ( int i = 0; i < parts.GetLength( 0 ); i++ )
				Place( map, new Static( parts[i, 0] ), cx + parts[i, 1], cy + parts[i, 2], z, 0 );
		}

		private static void Place( Map map, Item item, int x, int y, int z, int hue )
		{
			if ( hue >= 0 )
				item.Hue = hue;

			item.Movable = false;
			item.MoveToWorld( new Point3D( x, y, z ), map );
		}
	}
}
