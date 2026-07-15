using System.Collections.Generic;
using System.Linq;
using Server.Accounting;

namespace Server.Collectibles
{
	public static class CollectionLogStorage
	{
		private const string TagName = "CollectionLog";

		public static HashSet<int> GetCollected( Account account )
		{
			var set = new HashSet<int>();

			if ( account == null )
				return set;

			string raw = account.GetTag( TagName );

			if ( string.IsNullOrEmpty( raw ) )
				return set;

			foreach ( string piece in raw.Split( ',' ) )
			{
				int id;
				if ( int.TryParse( piece, out id ) )
					set.Add( id );
			}

			return set;
		}

		public static HashSet<int> GetCollected( Mobile mob )
		{
			if ( mob == null )
				return new HashSet<int>();

			return GetCollected( mob.Account as Account );
		}

		public static bool MarkCollected( Account account, int itemID )
		{
			if ( account == null )
				return false;

			var set = GetCollected( account );

			if ( !set.Add( itemID ) )
				return false;

			Save( account, set );
			return true;
		}

		public static bool MarkCollected( Mobile mob, int itemID )
		{
			if ( mob == null )
				return false;

			return MarkCollected( mob.Account as Account, itemID );
		}

		public static bool IsCollected( Account account, int itemID )
		{
			return GetCollected( account ).Contains( itemID );
		}

		private static void Save( Account account, HashSet<int> set )
		{
			account.SetTag( TagName, string.Join( ",", set.Select( i => i.ToString() ).ToArray() ) );
		}
	}
}
