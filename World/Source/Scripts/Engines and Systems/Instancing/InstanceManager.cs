using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Instancing
{
	public static class InstanceManager
	{
		private static readonly InstanceType[] m_Types = new InstanceType[]
		{
			SkyDwellingInstanceType.Instance,
			DungeonInstanceType.Instance,
			DungeonLobbyInstanceType.Instance
		};

		public static void Configure()
		{
			EventSink.WorldLoad += new WorldLoadEventHandler( OnWorldLoad );
			EventSink.WorldSave += new WorldSaveEventHandler( OnWorldSave );
			EventSink.Login += new LoginEventHandler( OnLogin );

			Timer.DelayCall( TimeSpan.FromMinutes( 1.0 ), TimeSpan.FromMinutes( 1.0 ), new TimerCallback( Sweep ) );
		}

		// CALLED FROM MAPDEFINITIONS SO POOL MAPS EXIST BEFORE ITEMS DESERIALIZE
		public static void RegisterAllPoolMaps()
		{
			foreach ( InstanceType type in m_Types )
				type.RegisterMaps();
		}

		public static InstanceType Find( Map map )
		{
			foreach ( InstanceType type in m_Types )
			{
				if ( type.IsPoolMap( map ) )
					return type;
			}

			return null;
		}

		public static bool TryGetRunDifficulty( Map map, out Difficulty difficulty )
		{
			return DungeonInstanceType.Instance.TryGetRunDifficulty( map, out difficulty );
		}

		public static bool IsInstanceMap( Map map )
		{
			return Find( map ) != null;
		}

		public static bool IsInstanceVoid( Map map, Point3D p )
		{
			return IsInstanceMap( map ) && Region.Find( p, map ) is InstanceVoidRegion;
		}

		public static bool TryGetRandomPlayablePoint( Map map, out Point3D p )
		{
			InstanceType type = Find( map );
			if ( type != null )
				return type.TryGetRandomPlayablePoint( map, out p );

			p = Point3D.Zero;
			return false;
		}

		public static void OnCreatureDeath( BaseCreature bc, Container corpse )
		{
			DemonRiftPortal.OnCreatureDeath( bc );

			InstanceType type = Find( bc.Map );
			if ( type != null )
				type.OnCreatureDeath( bc, corpse );
		}

		private static void OnWorldLoad()
		{
			foreach ( InstanceType type in m_Types )
			{
				type.OnLoad();
				type.EnsureRegions();
				type.ReleaseDeadOwners();

				Console.WriteLine( "[Instancing] {0}: {1} instance(s), {2} live on pool maps {3}-{4}.",
					type.Key, type.OwnerCount, type.LiveCount, type.PoolBaseIndex, type.PoolBaseIndex + type.PoolSize - 1 );
			}
		}

		private static void OnWorldSave( WorldSaveEventArgs e )
		{
			foreach ( InstanceType type in m_Types )
				type.OnSave();
		}

		private static void OnLogin( LoginEventArgs e )
		{
			InstanceType type = Find( e.Mobile.Map );
			if ( type != null )
				type.OnLogin( e.Mobile );
		}

		private static void Sweep()
		{
			foreach ( InstanceType type in m_Types )
				type.SweepUnload();
		}
	}
}
