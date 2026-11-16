using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Instancing
{
	public abstract class InstanceType
	{
		public abstract string Key { get; }
		public abstract int PoolBaseIndex { get; }
		public abstract int PoolSize { get; }

		public virtual Map BaseMap { get { return Map.SerpentIsland; } }
		public virtual Point3D Landing { get { return Point3D.Zero; } }
		public virtual Map ExitMap { get { return Map.Sosaria; } }
		public virtual Point3D ExitPoint { get { return new Point3D( 3884, 2879, 0 ); } }

		public virtual string RegionName { get { return Key; } }
		public virtual TimeSpan UnloadAfter { get { return TimeSpan.FromMinutes( 15 ); } }

		// PERSISTENT TYPES PARK IDLE INSTANCES AND SAVE THEM; THE REST ARE FREED
		public virtual bool Persistent { get { return false; } }
		public virtual string SavePath { get { return String.Format( "Saves/Instancing/{0}.bin", Key ); } }

		// A SMALL BOX, NOT THE WHOLE MAP: REGIONS ALLOCATE EVERY SECTOR THEY COVER
		public virtual bool UsesFootprintRegion { get { return true; } }

		public virtual Rectangle3D RegionFootprint()
		{
			return new Rectangle3D( new Point3D( Landing.X - 64, Landing.Y - 64, sbyte.MinValue ), new Point3D( Landing.X + 64, Landing.Y + 64, sbyte.MaxValue ) );
		}

		private readonly Dictionary<Serial, Instance> m_ByOwner = new Dictionary<Serial, Instance>();
		private readonly Dictionary<int, Instance> m_Live = new Dictionary<int, Instance>();
		private bool m_RegionsBuilt;

		public IEnumerable<Instance> AllInstances { get { return m_ByOwner.Values; } }
		public IEnumerable<Instance> LiveInstances { get { return m_Live.Values; } }
		public int OwnerCount { get { return m_ByOwner.Count; } }
		public int LiveCount { get { return m_Live.Count; } }

		protected virtual Map PoolBaseMapFor( int slot )
		{
			return BaseMap;
		}

		public void RegisterMaps()
		{
			for ( int i = 0; i < PoolSize; i++ )
			{
				int idx = PoolBaseIndex + i;
				Map baseMap = PoolBaseMapFor( i );
				if ( baseMap == null || idx >= Map.Maps.Length || Map.Maps[idx] != null )
					continue;

				Map map = new Map( baseMap.MapID, idx, baseMap.MapID, baseMap.Width, baseMap.Height, 1, String.Format( "{0}{1}", Key, i ), MapRules.LodorRules | MapRules.FreeMovement );
				map.BaseMap = baseMap;

				Map.Maps[idx] = map;
				Map.AllMaps.Add( map );
			}
		}

		public void EnsureRegions()
		{
			if ( m_RegionsBuilt )
				return;

			m_RegionsBuilt = true;

			for ( int i = 0; i < PoolSize; i++ )
			{
				Map map = Map.Maps[PoolBaseIndex + i];
				if ( map == null )
					continue;

				map.DefaultRegion = new InstanceVoidRegion( this, map );

				if ( UsesFootprintRegion )
					new InstanceRegion( RegionName, map, RegionFootprint() ).Register();
			}
		}

		public bool IsPoolMap( Map map )
		{
			return map != null && map.MapIndex >= PoolBaseIndex && map.MapIndex < PoolBaseIndex + PoolSize;
		}

		protected virtual Point3D GetLandingPoint( Instance inst )
		{
			return Landing;
		}

		public virtual Serial OwnerKey( Mobile m )
		{
			return m.Serial;
		}

		// PARTY MEMBERS SHARE THE LEADER'S INSTANCE
		protected static Serial PartyKey( Mobile m )
		{
			Server.Engines.PartySystem.Party p = Server.Engines.PartySystem.Party.Get( m );
			return p != null && p.Active && p.Leader != null ? p.Leader.Serial : m.Serial;
		}

		public Instance GetByKey( Serial key )
		{
			Instance inst;
			m_ByOwner.TryGetValue( key, out inst );
			return inst;
		}

		public Instance GetByOwner( Mobile owner )
		{
			return owner != null ? GetByKey( OwnerKey( owner ) ) : null;
		}

		protected Instance GetOrCreateByKey( Serial key )
		{
			Instance inst = GetByKey( key );
			if ( inst == null )
			{
				inst = new Instance();
				inst.OwnerSerial = key;
				m_ByOwner[key] = inst;
			}

			return inst;
		}

		public Instance GetOrCreate( Mobile owner )
		{
			return GetOrCreateByKey( OwnerKey( owner ) );
		}

		protected Instance GetLiveInstance( Map map )
		{
			Instance inst = null;
			if ( map != null )
				m_Live.TryGetValue( map.MapIndex, out inst );

			return inst;
		}

		public bool IsMember( Instance inst, Mobile m )
		{
			return inst != null && m != null && ( inst.OwnerSerial == OwnerKey( m ) || inst.Members.Contains( m.Serial ) );
		}

		public bool AddMember( Mobile owner, Mobile member )
		{
			Instance inst = GetByOwner( owner );
			if ( inst == null || member == null || owner == member || inst.Members.Contains( member.Serial ) )
				return false;

			inst.Members.Add( member.Serial );
			return true;
		}

		public bool RemoveMember( Mobile owner, Mobile member )
		{
			Instance inst = GetByOwner( owner );
			return inst != null && member != null && inst.Members.Remove( member.Serial );
		}

		public bool EnsureLive( Instance inst )
		{
			if ( inst.IsLive )
				return true;

			int idx = AcquireMapIndex( inst );
			if ( idx < 0 )
				return false;

			inst.LiveMapIndex = idx;
			m_Live[idx] = inst;

			if ( inst.Built )
			{
				RestoreContents( inst, inst.LiveMap );
			}
			else
			{
				BuildContents( inst, inst.LiveMap );
				inst.Built = true;
			}

			return true;
		}

		protected virtual bool CanHost( Instance inst, Map map )
		{
			return true;
		}

		private int AcquireMapIndex( Instance inst )
		{
			for ( int i = 0; i < PoolSize; i++ )
			{
				int idx = PoolBaseIndex + i;
				if ( !m_Live.ContainsKey( idx ) && CanHost( inst, Map.Maps[idx] ) )
					return idx;
			}

			Instance victim = null;
			foreach ( Instance live in m_Live.Values )
			{
				if ( !IsInUse( live ) && CanHost( inst, live.LiveMap ) && ( victim == null || live.LastTouched < victim.LastTouched ) )
					victim = live;
			}

			if ( victim == null )
				return -1;

			int freed = victim.LiveMapIndex;
			ReleaseIdle( victim );
			return freed;
		}

		protected bool RestoreLive( Instance inst, int mapIndex )
		{
			if ( inst.IsLive || !IsPoolMap( Map.Maps[mapIndex] ) || m_Live.ContainsKey( mapIndex ) )
				return false;

			inst.LiveMapIndex = mapIndex;
			inst.Built = true;
			m_Live[mapIndex] = inst;
			return true;
		}

		protected abstract void BuildContents( Instance inst, Map map );

		protected virtual void RestoreContents( Instance inst, Map map )
		{
			for ( int i = 0; i < inst.Items.Count; i++ )
			{
				InstanceItem di = inst.Items[i];
				if ( di.Item != null && !di.Item.Deleted )
					di.Item.MoveToWorld( di.Loc, map );
			}
		}

		public void Park( Instance inst )
		{
			if ( !inst.IsLive )
				return;

			CaptureItems( inst );
			InternalizeItems( inst );

			m_Live.Remove( inst.LiveMapIndex );
			inst.LiveMapIndex = -1;
		}

		private static void CaptureItems( Instance inst )
		{
			Map map = inst.LiveMap;
			inst.Items.Clear();

			foreach ( Item item in World.Items.Values )
			{
				if ( !item.Deleted && item.Parent == null && Object.ReferenceEquals( item.Map, map ) )
					inst.Items.Add( new InstanceItem( item, item.Location ) );
			}
		}

		private static void InternalizeItems( Instance inst )
		{
			for ( int i = 0; i < inst.Items.Count; i++ )
			{
				Item item = inst.Items[i].Item;
				if ( item != null && !item.Deleted )
					item.Internalize();
			}
		}

		public void FreeInstance( Instance inst )
		{
			Map liveMap = inst.LiveMap;

			if ( inst.IsLive )
			{
				m_Live.Remove( inst.LiveMapIndex );
				inst.LiveMapIndex = -1;
			}

			OnFreeInstance( inst, liveMap );

			for ( int i = 0; i < inst.Items.Count; i++ )
			{
				Item item = inst.Items[i].Item;
				if ( item != null && !item.Deleted )
					item.Delete();
			}

			inst.Items.Clear();
			m_ByOwner.Remove( inst.OwnerSerial );
		}

		protected virtual void OnFreeInstance( Instance inst, Map liveMap )
		{
		}

		protected virtual bool ShouldPersistOnIdle( Instance inst )
		{
			return Persistent;
		}

		private void ReleaseIdle( Instance inst )
		{
			if ( ShouldPersistOnIdle( inst ) )
				Park( inst );
			else
				FreeInstance( inst );
		}

		public void SweepUnload()
		{
			List<Instance> idle = new List<Instance>();

			foreach ( Instance inst in m_Live.Values )
			{
				if ( IsInUse( inst ) )
					inst.Touch();
				else if ( DateTime.Now - inst.LastTouched >= UnloadAfter )
					idle.Add( inst );
			}

			for ( int i = 0; i < idle.Count; i++ )
				ReleaseIdle( idle[i] );

			ReleaseDeadOwners();
		}

		protected virtual bool IsInUse( Instance inst )
		{
			return HasPlayersInside( inst );
		}

		protected bool HasPlayersInside( Instance inst )
		{
			return inst.IsLive && PlayersOn( inst.LiveMap ).Count > 0;
		}

		public static List<Mobile> PlayersOn( Map map )
		{
			List<Mobile> list = new List<Mobile>();

			foreach ( NetState ns in NetState.Instances )
			{
				if ( ns.Mobile != null && Object.ReferenceEquals( ns.Mobile.Map, map ) )
					list.Add( ns.Mobile );
			}

			return list;
		}

		public int ReleaseDeadOwners()
		{
			List<Instance> dead = new List<Instance>();

			foreach ( Instance inst in m_ByOwner.Values )
			{
				bool exists = inst.OwnerSerial.IsItem ? World.FindItem( inst.OwnerSerial ) != null : World.FindMobile( inst.OwnerSerial ) != null;
				if ( !exists )
					dead.Add( inst );
			}

			for ( int i = 0; i < dead.Count; i++ )
				FreeInstance( dead[i] );

			return dead.Count;
		}

		public bool SendToInstance( Mobile from, Instance inst )
		{
			if ( !EnsureLive( inst ) )
			{
				from.SendMessage( "All instances are currently in use; please try again shortly." );
				return false;
			}

			inst.Touch();
			Teleport( from, GetLandingPoint( inst ), inst.LiveMap );
			return true;
		}

		public bool SendOwnerToTheirInstance( Mobile owner )
		{
			return SendToInstance( owner, GetOrCreate( owner ) );
		}

		protected static void Teleport( Mobile m, Point3D p, Map map )
		{
			BaseCreature.TeleportPets( m, p, map, false );
			m.MoveToWorld( p, map );
		}

		protected void GetReturn( Instance inst, out Map map, out Point3D p )
		{
			if ( inst != null && inst.ReturnMap != null )
			{
				map = inst.ReturnMap;
				p = inst.ReturnPoint;
			}
			else
			{
				map = ExitMap;
				p = ExitPoint;
			}
		}

		public bool LeaveInstance( Mobile m )
		{
			if ( m == null || !IsPoolMap( m.Map ) )
				return false;

			Map map;
			Point3D p;
			GetReturn( GetLiveInstance( m.Map ), out map, out p );
			Teleport( m, p, map );
			return true;
		}

		public virtual void OnLogin( Mobile m )
		{
			Instance inst = GetLiveInstance( m.Map );
			if ( inst != null )
				inst.Touch();
			else
				LeaveInstance( m );
		}

		public virtual void OnWalkIntoVoid( Mobile m, Map map )
		{
		}

		public virtual void OnCreatureDeath( BaseCreature bc, Container corpse )
		{
		}

		public virtual bool TryGetRandomPlayablePoint( Map map, out Point3D p )
		{
			p = Point3D.Zero;
			return false;
		}

		public void ReturnFromVoid( Mobile m, Map map )
		{
			if ( m.Deleted || !Object.ReferenceEquals( m.Map, map ) || !( m.Region is InstanceVoidRegion ) )
				return;

			Instance inst = GetLiveInstance( map );

			if ( m.Player )
			{
				if ( inst != null )
					Teleport( m, GetLandingPoint( inst ), map );
				else
					LeaveInstance( m );

				m.SendMessage( "You cannot go there." );
				return;
			}

			BaseCreature bc = m as BaseCreature;
			Mobile master = bc != null ? ( bc.ControlMaster != null ? bc.ControlMaster : bc.SummonMaster ) : null;

			if ( master != null && !master.Deleted && master.Map != null && master.Map != Map.Internal && !( master.Region is InstanceVoidRegion ) )
				m.MoveToWorld( master.Location, master.Map );
			else if ( bc != null && bc.Controlled && inst != null )
				m.MoveToWorld( GetLandingPoint( inst ), map );
			else if ( bc == null || !bc.Controlled )
				m.Delete();
		}

		// PLAYERS, PETS AND PLAYER CORPSES GO HOME; EVERYTHING ELSE IS DELETED
		protected void ClearMap( Map map, Instance inst )
		{
			ClearMaps( delegate( Map m ) { return Object.ReferenceEquals( m, map ); }, inst );
		}

		protected void ClearPoolMaps( HashSet<int> keep )
		{
			ClearMaps( delegate( Map m ) { return IsPoolMap( m ) && !keep.Contains( m.MapIndex ); }, null );
		}

		private void ClearMaps( Predicate<Map> match, Instance inst )
		{
			List<Mobile> mobiles = new List<Mobile>();
			foreach ( Mobile m in World.Mobiles.Values )
			{
				Map map = m.Map == Map.Internal ? m.LogoutMap : m.Map;
				if ( map != null && match( map ) && ( m.Map != Map.Internal || m.Player ) )
					mobiles.Add( m );
			}

			List<Item> items = new List<Item>();
			foreach ( Item item in World.Items.Values )
			{
				if ( item.Parent == null && item.Map != null && match( item.Map ) )
					items.Add( item );
			}

			Map returnMap;
			Point3D returnPoint;
			GetReturn( inst, out returnMap, out returnPoint );

			for ( int i = 0; i < mobiles.Count; i++ )
			{
				Mobile m = mobiles[i];
				BaseCreature pet = m as BaseCreature;

				if ( m.Deleted )
				{
				}
				else if ( m.Player && m.Map == Map.Internal )
				{
					m.LogoutMap = returnMap;
					m.LogoutLocation = returnPoint;
				}
				else if ( m.Player )
				{
					Teleport( m, returnPoint, returnMap );
				}
				else if ( pet != null && pet.Controlled && !pet.Summoned && pet.ControlMaster != null )
				{
					pet.ControlTarget = null;
					pet.ControlOrder = OrderType.Stay;
					pet.MoveToWorld( returnPoint, returnMap );
				}
				else
				{
					m.Delete();
				}
			}

			for ( int i = 0; i < items.Count; i++ )
			{
				Item item = items[i];
				Corpse corpse = item as Corpse;

				if ( item.Deleted )
					continue;

				if ( corpse != null && corpse.Owner != null && corpse.Owner.Player && corpse.TotalItems > 0 )
					corpse.MoveToWorld( returnPoint, returnMap );
				else
					item.Delete();
			}
		}

		public virtual void OnLoad()
		{
			if ( !Persistent )
				return;

			Load();

			List<Instance> drop = new List<Instance>();
			foreach ( Instance inst in m_ByOwner.Values )
			{
				inst.Items.RemoveAll( delegate( InstanceItem di ) { return di.Item == null || di.Item.Deleted; } );

				if ( !ShouldPersistOnIdle( inst ) )
				{
					drop.Add( inst );
				}
				else if ( inst.IsLive )
				{
					InternalizeItems( inst );
					inst.LiveMapIndex = -1;
				}
			}

			for ( int i = 0; i < drop.Count; i++ )
				FreeInstance( drop[i] );
		}

		public virtual void OnSave()
		{
			if ( !Persistent )
				return;

			foreach ( Instance inst in m_Live.Values )
				CaptureItems( inst );

			Persistence.Serialize( SavePath, delegate( GenericWriter writer )
			{
				writer.Write( (int) 4 ); // version

				writer.Write( (int) m_ByOwner.Count );
				foreach ( Instance inst in m_ByOwner.Values )
				{
					writer.Write( (int) inst.OwnerSerial );
					writer.Write( (DateTime) inst.LastTouched );
					writer.Write( (bool) inst.Built );
					writer.Write( (bool) inst.Purchased );
					writer.Write( (bool) inst.Public );
					writer.Write( (int) inst.LiveMapIndex );

					writer.Write( (int) inst.Members.Count );
					for ( int i = 0; i < inst.Members.Count; i++ )
						writer.Write( (int) inst.Members[i] );

					writer.Write( (int) inst.Items.Count );
					for ( int i = 0; i < inst.Items.Count; i++ )
					{
						writer.Write( (Item) inst.Items[i].Item );
						writer.Write( (Point3D) inst.Items[i].Loc );
					}
				}
			} );
		}

		private void Load()
		{
			Persistence.Deserialize( SavePath, delegate( GenericReader reader )
			{
				if ( reader.End() )
					return;

				int version = reader.ReadInt();
				int count = reader.ReadInt();

				for ( int i = 0; i < count; i++ )
				{
					Instance inst = new Instance();
					inst.OwnerSerial = (Serial)reader.ReadInt();
					if ( version == 3 )
						reader.ReadInt();
					inst.LastTouched = reader.ReadDateTime();
					inst.Built = reader.ReadBool();
					if ( version >= 1 )
						inst.Purchased = reader.ReadBool();
					if ( version >= 2 )
						inst.Public = reader.ReadBool();
					inst.LiveMapIndex = reader.ReadInt();

					int members = reader.ReadInt();
					for ( int j = 0; j < members; j++ )
						inst.Members.Add( (Serial)reader.ReadInt() );

					int items = reader.ReadInt();
					for ( int j = 0; j < items; j++ )
					{
						Item item = reader.ReadItem();
						Point3D loc = reader.ReadPoint3D();
						if ( item != null )
							inst.Items.Add( new InstanceItem( item, loc ) );
					}

					m_ByOwner[inst.OwnerSerial] = inst;
				}
			} );
		}
	}
}
