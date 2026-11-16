using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Instancing
{
	public static class SkyInstanceCommands
	{
		private static SkyDwellingInstanceType Sky { get { return SkyDwellingInstanceType.Instance; } }

		public static void Configure()
		{
			CommandSystem.Register( "skydwelling", AccessLevel.Player, new CommandEventHandler( OnSkyDwellingCommand ) );
			CommandSystem.Register( "leavedwelling", AccessLevel.Player, new CommandEventHandler( OnLeaveDwellingCommand ) );
			CommandSystem.Register( "skyfriend", AccessLevel.Player, new CommandEventHandler( OnSkyFriendCommand ) );
			CommandSystem.Register( "skyvisit", AccessLevel.Player, new CommandEventHandler( OnSkyVisitCommand ) );
			CommandSystem.Register( "skyinstance", AccessLevel.GameMaster, new CommandEventHandler( OnAdminCommand ) );
		}

		private static void OnSkyDwellingCommand( CommandEventArgs e )
		{
			if ( Server.Spells.SpellHelper.CheckTravel( e.Mobile, Server.Spells.TravelCheckType.RecallFrom ) )
				Sky.SendOwnerToTheirInstance( e.Mobile );
		}

		private static void OnLeaveDwellingCommand( CommandEventArgs e )
		{
			if ( !Sky.LeaveInstance( e.Mobile ) )
				e.Mobile.SendMessage( "You are not currently in a sky dwelling." );
		}

		private static void OnSkyVisitCommand( CommandEventArgs e )
		{
			if ( !Server.Spells.SpellHelper.CheckTravel( e.Mobile, Server.Spells.TravelCheckType.RecallFrom ) )
				return;

			e.Mobile.SendMessage( "Target the player whose sky dwelling you want to visit." );
			e.Mobile.BeginTarget( 12, false, TargetFlags.None, new TargetCallback( OnVisitTarget ) );
		}

		private static void OnVisitTarget( Mobile from, object targeted )
		{
			if ( targeted is PlayerMobile )
				Sky.VisitFriendDwelling( from, (Mobile)targeted );
			else
				from.SendMessage( "That is not a player." );
		}

		private static void OnSkyFriendCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			string sub = e.Arguments.Length > 0 ? e.Arguments[0].ToLower() : "";

			if ( sub == "list" )
			{
				Instance inst = Sky.GetByOwner( from );
				if ( inst == null || inst.Members.Count == 0 )
				{
					from.SendMessage( "You have not invited anyone to your sky dwelling." );
					return;
				}

				from.SendMessage( "Friends invited to your sky dwelling:" );
				for ( int i = 0; i < inst.Members.Count; i++ )
				{
					Mobile f = World.FindMobile( inst.Members[i] );
					from.SendMessage( " - {0}", f != null ? f.Name : String.Format( "(deleted, serial 0x{0:X})", (int)inst.Members[i] ) );
				}
			}
			else if ( sub == "add" && !Sky.OwnsDwelling( from ) )
			{
				from.SendMessage( "You do not own a sky dwelling. Purchase one from a sky dwelling sign." );
			}
			else if ( sub == "add" || sub == "remove" )
			{
				from.SendMessage( sub == "add" ? "Target the player to invite to your sky dwelling." : "Target the player to un-invite from your sky dwelling." );
				from.BeginTarget( 12, false, TargetFlags.None, new TargetStateCallback( OnFriendTarget ), sub == "add" );
			}
			else
			{
				from.SendMessage( "Usage: [skyfriend add | remove | list" );
			}
		}

		private static void OnFriendTarget( Mobile from, object targeted, object state )
		{
			bool add = (bool)state;
			PlayerMobile target = targeted as PlayerMobile;

			if ( target == null )
				from.SendMessage( "That is not a player." );
			else if ( target == from )
				from.SendMessage( "You cannot {0} yourself.", add ? "invite" : "un-invite" );
			else if ( add )
				from.SendMessage( Sky.AddMember( from, target ) ? "{0} can now visit your sky dwelling." : "{0} is already invited.", target.Name );
			else
				from.SendMessage( Sky.RemoveMember( from, target ) ? "{0} can no longer visit your sky dwelling." : "{0} was not on your invite list.", target.Name );
		}

		private static void OnAdminCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			string sub = e.Arguments.Length > 0 ? e.Arguments[0].ToLower() : "";
			int value;

			switch ( sub )
			{
				case "status":
					from.SendMessage( "Sky Dwelling status: {0} record(s), {1} live / {2} pool maps (price {3}g, unload after {4} min).",
						Sky.OwnerCount, Sky.LiveCount, Sky.PoolSize, Sky.DwellingPrice, (int)Sky.UnloadAfter.TotalMinutes );
					break;
				case "price":
					if ( e.Arguments.Length < 2 || !Int32.TryParse( e.Arguments[1], out value ) || value < 0 )
					{
						from.SendMessage( "Usage: [skyinstance price <gold>" );
						break;
					}
					Sky.DwellingPrice = value;
					from.SendMessage( "Sky dwellings now cost {0} gold.", value );
					break;
				case "unload":
					if ( e.Arguments.Length < 2 || !Int32.TryParse( e.Arguments[1], out value ) || value < 1 )
					{
						from.SendMessage( "Usage: [skyinstance unload <minutes>" );
						break;
					}
					Sky.SetUnloadAfter( TimeSpan.FromMinutes( value ) );
					from.SendMessage( "Sky dwellings will now unload after {0} minutes of inactivity.", value );
					break;
				case "park":
					from.SendMessage( "Parked {0} empty dwelling(s).", Sky.ParkEmpty() );
					break;
				case "grant":
					from.SendMessage( "Target the player to grant a sky dwelling (bypasses purchase)." );
					from.BeginTarget( 12, false, TargetFlags.None, new TargetCallback( OnGrantTarget ) );
					break;
				case "gotoplayer":
					from.SendMessage( "Target the player whose dwelling you want to visit." );
					from.BeginTarget( 12, false, TargetFlags.None, new TargetCallback( OnVisitTarget ) );
					break;
				case "placeportal":
					new SkyDwellingPortal().MoveToWorld( from.Location, from.Map );
					from.SendMessage( "Placed a sky dwelling portal here." );
					break;
				case "freedead":
					from.SendMessage( "Freed {0} orphaned dwelling(s).", Sky.ReleaseDeadOwners() );
					break;
				case "installrope":
					from.SendMessage( "Replaced {0} climb teleporter(s) with the sky dwelling chooser.", InstallChooserRopes() );
					break;
				default:
					from.SendMessage( "[skyinstance status | price <gold> | unload <minutes> | park | grant | gotoplayer | placeportal | freedead | installrope" );
					break;
			}
		}

		private static void OnGrantTarget( Mobile from, object targeted )
		{
			PlayerMobile target = targeted as PlayerMobile;

			if ( target == null )
				from.SendMessage( "That is not a player." );
			else if ( Sky.Purchase( target ) )
				from.SendMessage( "Granted a sky dwelling to {0}.", target.Name );
			else
				from.SendMessage( "{0} already owns a sky dwelling.", target.Name );
		}

		// SWAPS THE PLAIN CLIMB TELEPORTERS IN AN ALREADY DECORATED WORLD FOR THE CHOOSER ONES
		private static int InstallChooserRopes()
		{
			List<KeywordTeleporter> ropes = new List<KeywordTeleporter>();
			foreach ( Item item in World.Items.Values )
			{
				KeywordTeleporter kt = item as KeywordTeleporter;
				if ( kt != null && kt.GetType() == typeof( KeywordTeleporter ) && kt.MapDest == Map.SerpentIsland && kt.PointDest == Sky.Landing )
					ropes.Add( kt );
			}

			foreach ( KeywordTeleporter kt in ropes )
			{
				SkyDwellingTeleporter rope = new SkyDwellingTeleporter();
				rope.Substring = kt.Substring;
				rope.Keyword = kt.Keyword;
				rope.Range = kt.Range;
				rope.PointDest = kt.PointDest;
				rope.MapDest = kt.MapDest;
				rope.Active = kt.Active;
				rope.ItemID = kt.ItemID;
				rope.Hue = kt.Hue;
				rope.MoveToWorld( kt.Location, kt.Map );
				kt.Delete();
			}

			return ropes.Count;
		}
	}
}
