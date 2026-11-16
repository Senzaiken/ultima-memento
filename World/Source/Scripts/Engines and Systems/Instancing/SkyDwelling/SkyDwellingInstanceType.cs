using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Multis;
using Knives.TownHouses;

namespace Server.Engines.Instancing
{
	// THE DWELLING SHELL IS BAKED INTO THE STATICS; ONLY ITS DOORS AND SIGN ARE ITEMS
	public class SkyDwellingInstanceType : InstanceType
	{
		public static readonly SkyDwellingInstanceType Instance = new SkyDwellingInstanceType();

		private static readonly Rectangle2D FirstDwellingBounds = new Rectangle2D( 1950, 1910, 72, 92 );

		public int DwellingPrice = 100000;
		private TimeSpan m_UnloadAfter = TimeSpan.FromMinutes( 15 );

		private SkyDwellingInstanceType()
		{
		}

		public override string Key { get { return "SkyDwelling"; } }
		public override int PoolBaseIndex { get { return 40; } }
		public override int PoolSize { get { return 32; } }
		public override Point3D Landing { get { return new Point3D( 1974, 1977, 0 ); } }
		public override string RegionName { get { return "Sky Dwelling"; } }
		public override bool Persistent { get { return true; } }
		public override string SavePath { get { return "Saves/Instancing/SkyDwellings.bin"; } }
		public override TimeSpan UnloadAfter { get { return m_UnloadAfter; } }

		public void SetUnloadAfter( TimeSpan ts )
		{
			m_UnloadAfter = ts;
		}

		// AN UNPURCHASED DWELLING IS ONLY A LOOK AROUND, FREED WHEN IDLE
		protected override bool ShouldPersistOnIdle( Instance inst )
		{
			return inst.Purchased;
		}

		public bool OwnsDwelling( Mobile m )
		{
			Instance inst = GetByOwner( m );
			return inst != null && inst.Purchased;
		}

		public bool Purchase( Mobile owner )
		{
			Instance inst = GetOrCreate( owner );
			if ( inst.Purchased )
				return false;

			inst.Purchased = true;
			inst.Touch();

			for ( int i = 0; i < inst.Items.Count; i++ )
			{
				SkyDwellingSign sign = inst.Items[i].Item as SkyDwellingSign;
				if ( sign != null )
					sign.SetOwned( inst.OwnerSerial );
			}

			return true;
		}

		public bool SetPublic( Mobile owner, bool pub )
		{
			Instance inst = GetByOwner( owner );
			if ( inst == null || !inst.Purchased )
				return false;

			inst.Public = pub;
			return true;
		}

		public bool IsPublic( Mobile owner )
		{
			Instance inst = GetByOwner( owner );
			return inst != null && inst.Public;
		}

		public int ParkEmpty()
		{
			List<Instance> empty = new List<Instance>();
			foreach ( Instance inst in LiveInstances )
			{
				if ( !HasPlayersInside( inst ) )
					empty.Add( inst );
			}

			for ( int i = 0; i < empty.Count; i++ )
				Park( empty[i] );

			return empty.Count;
		}

		public override void OnLogin( Mobile m )
		{
			Instance inst = GetByOwner( m );
			if ( inst != null )
				SendToInstance( m, inst );
			else
				LeaveInstance( m );
		}

		public List<Instance> InvitedInstances( Mobile m )
		{
			List<Instance> list = new List<Instance>();

			foreach ( Instance inst in AllInstances )
			{
				if ( inst.Purchased && inst.OwnerSerial != m.Serial && inst.Members.Contains( m.Serial ) )
					list.Add( inst );
			}

			return list;
		}

		public List<Instance> PublicInstances( Mobile m )
		{
			List<Instance> list = new List<Instance>();

			foreach ( Instance inst in AllInstances )
			{
				if ( inst.Purchased && inst.Public && inst.OwnerSerial != m.Serial && !inst.Members.Contains( m.Serial ) )
					list.Add( inst );
			}

			return list;
		}

		public bool VisitFriendDwelling( Mobile from, Mobile owner )
		{
			Instance inst = GetByOwner( owner );
			if ( inst == null || !inst.Purchased )
			{
				from.SendMessage( "{0} has no sky dwelling.", owner.Name );
				return false;
			}

			if ( !IsMember( inst, from ) && from.AccessLevel < AccessLevel.GameMaster )
			{
				from.SendMessage( "{0} has not invited you to their sky dwelling.", owner.Name );
				return false;
			}

			return EnterChosen( from, inst.OwnerSerial );
		}

		public bool EnterChosen( Mobile from, Serial owner )
		{
			Instance inst = GetByKey( owner );
			if ( inst == null || !inst.Purchased )
			{
				from.SendMessage( "That sky dwelling is no longer there." );
				return false;
			}

			if ( !IsMember( inst, from ) && !inst.Public && from.AccessLevel < AccessLevel.GameMaster )
			{
				from.SendMessage( "You are not allowed to enter that sky dwelling." );
				return false;
			}

			if ( !SendToInstance( from, inst ) )
				return false;

			Mobile m = World.FindMobile( owner );
			if ( m != null && m != from )
				from.SendMessage( "You arrive in {0}'s sky dwelling.", m.Name );

			return true;
		}

		protected override void BuildContents( Instance inst, Map map )
		{
			Point3D signLoc = new Point3D( Landing.X, Landing.Y - 1, Landing.Z );

			IPooledEnumerable eable = Map.SerpentIsland.GetItemsInBounds( FirstDwellingBounds );
			foreach ( Item item in eable )
			{
				if ( item is BaseDoor )
					Place( inst, map, CloneDoor( (BaseDoor)item ), item.Location );
				else if ( item is HouseSign || item is TownHouseSign )
					signLoc = item.Location;
			}
			eable.Free();

			SkyDwellingSign sign = new SkyDwellingSign();
			if ( inst.Purchased )
				sign.SetOwned( inst.OwnerSerial );
			else
				sign.Price = DwellingPrice;

			Place( inst, map, sign, signLoc );
		}

		private static void Place( Instance inst, Map map, Item item, Point3D p )
		{
			item.MoveToWorld( p, map );
			inst.Items.Add( new InstanceItem( item, p ) );
		}

		private static BaseDoor CloneDoor( BaseDoor src )
		{
			BaseDoor door;
			if ( src is StrongWoodDoor )
				door = new StrongWoodDoor( DoorFacing.WestCW );
			else if ( src is MetalDoor )
				door = new MetalDoor( DoorFacing.WestCW );
			else
				door = new DarkWoodDoor( DoorFacing.WestCW );

			door.ItemID = src.ItemID;
			door.ClosedID = src.ClosedID;
			door.OpenedID = src.OpenedID;
			door.OpenedSound = src.OpenedSound;
			door.ClosedSound = src.ClosedSound;
			door.Offset = src.Offset;
			door.Locked = false;
			return door;
		}
	}
}
