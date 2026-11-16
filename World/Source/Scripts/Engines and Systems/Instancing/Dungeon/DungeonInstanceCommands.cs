using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Targeting;

namespace Server.Engines.Instancing
{
	public static class DungeonInstanceCommands
	{
		private const int SpawnAllColumns = 20;

		private static DungeonInstanceType Dungeon { get { return DungeonInstanceType.Instance; } }

		public static void Configure()
		{
			CommandSystem.Register( "dungeon", AccessLevel.GameMaster, new CommandEventHandler( OnEnterCommand ) );
			CommandSystem.Register( "leavedungeon", AccessLevel.Player, new CommandEventHandler( OnLeaveCommand ) );
			CommandSystem.Register( "dungeoninstance", AccessLevel.GameMaster, new CommandEventHandler( OnAdminCommand ) );
		}

		// [dungeon [name or index] ENTERS THAT DUNGEON, OR A RANDOM ONE
		private static void OnEnterCommand( CommandEventArgs e )
		{
			DungeonInstanceType.DungeonInstanceDefinition def = e.Length > 0 ? Dungeon.FindDefinition( e.ArgString ) : Dungeon.GetRandomDefinition();
			Dungeon.SendToConfiguredInstance( e.Mobile, Dungeon.OwnerKey( e.Mobile ), def, Difficulty.Normal, e.Mobile.Map, e.Mobile.Location, TimeSpan.Zero );
		}

		private static void OnLeaveCommand( CommandEventArgs e )
		{
			if ( !Dungeon.LeaveInstance( e.Mobile ) )
				e.Mobile.SendMessage( "You are not currently in a dungeon instance." );
		}

		private static void OnAdminCommand( CommandEventArgs e )
		{
			Mobile from = e.Mobile;
			string sub = e.Length > 0 ? e.Arguments[0].ToLower() : "";
			string rest = e.Length > 1 ? String.Join( " ", e.Arguments, 1, e.Length - 1 ) : "";

			switch ( sub )
			{
				case "status":
					from.SendMessage( "Dungeon instances: {0} record(s), {1} live / {2} pool maps, unload after {3} min.", Dungeon.OwnerCount, Dungeon.LiveCount, Dungeon.PoolSize, (int)Dungeon.UnloadAfter.TotalMinutes );
					break;
				case "list":
					ListDefinitions( from, rest );
					break;
				case "placeentrance":
					new DungeonInstanceGate().MoveToWorld( from.Location, from.Map );
					from.SendMessage( "Placed a dungeon instance gate here." );
					break;
				case "spawnall":
					from.SendMessage( "Target the northwest corner tile for a grid of gates, one per dungeon." );
					from.BeginTarget( -1, true, TargetFlags.None, new TargetCallback( OnSpawnAllTarget ) ).CheckLOS = false;
					break;
				case "audit":
					DungeonInstanceAudit.Run( from, rest.Length > 0 ? rest : "all" );
					break;
				case "clearall":
					from.SendMessage( "Cleared {0} dungeon instance(s).", Dungeon.ClearAllInstances() );
					break;
				case "boss":
					if ( !Dungeon.SummonBoss( from ) )
						from.SendMessage( "Stand in a dungeon instance whose boss has not woken yet." );
					break;
				default:
					from.SendMessage( "[dungeoninstance status | list [filter] | audit [index|filter] | placeentrance | spawnall | clearall | boss" );
					break;
			}
		}

		private static void ListDefinitions( Mobile from, string filter )
		{
			int shown = 0, matched = 0;

			foreach ( DungeonInstanceType.DungeonInstanceDefinition def in Dungeon.Definitions )
			{
				if ( filter.Length > 0 && def.DisplayName.IndexOf( filter, StringComparison.OrdinalIgnoreCase ) < 0 )
					continue;

				if ( ++matched <= 40 )
				{
					from.SendMessage( "{0}: {1} ({2} spawn entries)", def.Index, def.DisplayName, def.SpawnCount );
					shown++;
				}
			}

			if ( matched > shown )
				from.SendMessage( "Showing {0} of {1} matches. Use a filter to narrow the list.", shown, matched );
			else
				from.SendMessage( "Matched {0} dungeon(s).", matched );
		}

		private static void OnSpawnAllTarget( Mobile from, object targeted )
		{
			IPoint3D p = targeted as IPoint3D;
			if ( p == null || from.Map == null || from.Map == Map.Internal )
			{
				from.SendMessage( "That is not a valid location." );
				return;
			}

			List<DungeonInstanceType.DungeonInstanceDefinition> defs = Dungeon.Definitions;
			for ( int i = 0; i < defs.Count; i++ )
			{
				DungeonInstanceGate gate = new DungeonInstanceGate();
				gate.DungeonName = defs[i].DisplayName;
				gate.MoveToWorld( new Point3D( p.X + i % SpawnAllColumns, p.Y + i / SpawnAllColumns, p.Z ), from.Map );
			}

			from.SendMessage( "Placed {0} dungeon instance gate(s).", defs.Count );
		}
	}
}
