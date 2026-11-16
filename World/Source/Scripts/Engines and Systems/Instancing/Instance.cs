using System;
using System.Collections.Generic;
using Server;

namespace Server.Engines.Instancing
{
	public class InstanceItem
	{
		public Item Item;
		public Point3D Loc;

		public InstanceItem( Item item, Point3D loc )
		{
			Item = item;
			Loc = loc;
		}
	}

	public class Instance
	{
		public Serial OwnerSerial;
		public DateTime LastTouched = DateTime.Now;
		public int LiveMapIndex = -1;
		public bool Built;
		public bool Purchased;
		public bool Public;
		public Map ReturnMap;
		public Point3D ReturnPoint;
		public object Data;
		public readonly List<Serial> Members = new List<Serial>();
		public readonly List<InstanceItem> Items = new List<InstanceItem>();

		public bool IsLive { get { return LiveMapIndex >= 0; } }
		public Map LiveMap { get { return IsLive ? Map.Maps[LiveMapIndex] : null; } }

		public void Touch()
		{
			LastTouched = DateTime.Now;
		}
	}
}
