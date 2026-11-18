using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.Instancing
{
	// PARTY-SHARED COPIES OF REAL DUNGEONS, SEVEN CLONES OF EACH SURFACE MAP
	public partial class DungeonInstanceType : InstanceType
	{
		public static readonly DungeonInstanceType Instance = new DungeonInstanceType();

		private const int ClonesPerBaseMap = 7;
		private const string InstanceSavePath = "Saves/Instancing/DungeonInstances.bin";

		private static readonly TimeSpan RestartGrace = TimeSpan.FromMinutes( 15 );

		private DungeonInstanceType()
		{
		}

		public static void Configure()
		{
			EventSink.PlayerDeath += delegate( PlayerDeathEventArgs e ) { QueueFate( e.Mobile ); };
			EventSink.Login += delegate( LoginEventArgs e ) { QueueFate( e.Mobile ); };
		}

		public override string Key { get { return "Dungeon"; } }
		public override int PoolBaseIndex { get { return 72; } }
		public override int PoolSize { get { return CloneBases.Length * ClonesPerBaseMap; } }
		public override TimeSpan UnloadAfter { get { return TimeSpan.FromMinutes( 5 ); } }
		public override bool UsesFootprintRegion { get { return false; } }

		private static Map[] CloneBases
		{
			get { return new Map[] { Map.Lodor, Map.Sosaria, Map.Underworld, Map.SerpentIsland, Map.IslesDread, Map.SavagedEmpire, Map.Atlantis }; }
		}

		protected override Map PoolBaseMapFor( int slot )
		{
			return CloneBases[slot / ClonesPerBaseMap];
		}

		public override Serial OwnerKey( Mobile m )
		{
			return PartyKey( m );
		}

		public static Difficulty ClampDifficulty( Difficulty difficulty )
		{
			if ( difficulty < Difficulty.Easy )
				return Difficulty.Easy;

			return difficulty > Difficulty.Deadly ? Difficulty.Deadly : difficulty;
		}

		private static DungeonRun GetRun( Instance inst )
		{
			return inst != null ? inst.Data as DungeonRun : null;
		}

		private RuntimeDungeonState GetState( Map map )
		{
			DungeonRun run = GetRun( GetLiveInstance( map ) );
			return run != null ? run.State : null;
		}

		public bool TryGetRunDifficulty( Map map, out Difficulty difficulty )
		{
			DungeonRun run = GetRun( GetLiveInstance( map ) );
			difficulty = run != null ? run.Difficulty : Difficulty.Normal;
			return run != null;
		}

		// THE DUNGEON MASTER WHOSE PORTAL OPENED THIS RUN, WHILE HIS HALL STANDS
		private static DungeonMaster MasterOf( Instance inst )
		{
			DungeonLobbyPortal portal = inst != null ? World.FindItem( inst.OwnerSerial ) as DungeonLobbyPortal : null;
			DungeonMaster master = portal != null ? portal.Master : null;
			return master != null && !master.Deleted && InstanceManager.IsInstanceMap( master.Map ) ? master : null;
		}

		private DungeonMaster MasterOf( Map map )
		{
			return MasterOf( GetLiveInstance( map ) );
		}

		protected override void GetReturn( Instance inst, out Map map, out Point3D p )
		{
			DungeonMaster master = MasterOf( inst );
			if ( master == null )
			{
				base.GetReturn( inst, out map, out p );
				return;
			}

			map = master.Map;
			p = DungeonLobbyInstanceType.Instance.Landing;
		}

		public bool IsRunMap( Serial key, Map map )
		{
			Instance inst = GetByKey( key );
			return inst != null && inst.IsLive && Object.ReferenceEquals( inst.LiveMap, map );
		}

		// A DEATH THAT LEAVES NOBODY STANDING ALSO ENDS THE WAIT OF THOSE WHO LINGERED TO BE RAISED
		private static void QueueFate( Mobile m )
		{
			Map map = m.Map;
			if ( Instance.MasterOf( map ) == null )
				return;

			Timer.DelayCall( TimeSpan.Zero, delegate
			{
				if ( !IsFallen( m, map ) )
					return;

				OfferFate( m, map );

				if ( HasCompanions( m, map ) )
					return;

				foreach ( Mobile other in PlayersOn( map ) )
				{
					if ( other != m && IsFallen( other, map ) )
						OfferFate( other, map );
				}
			} );
		}

		private static bool IsFallen( Mobile m, Map map )
		{
			return m.Player && !m.Deleted && !m.Alive && Object.ReferenceEquals( m.Map, map ) && Instance.MasterOf( map ) != null;
		}

		// IN THE HALL AND ITS RUNS THE ANKH IS NEVER FAR, SO NOBODY IS ASKED TO BEG THE GODS FOR THEIR LIFE
		public static bool AnkhAwaits( Mobile m )
		{
			return DungeonLobbyInstanceType.Instance.IsPoolMap( m.Map ) || Instance.MasterOf( m.Map ) != null;
		}

		private static bool DiesForGood( Mobile m )
		{
			PlayerMobile pm = m as PlayerMobile;
			return pm != null && ( pm.Avatar.Active || pm.Temptations.HasPermanentDeath );
		}

		// THE FALLEN CHOOSE BETWEEN WHATEVER CAN STILL RAISE THEM BELOW AND THE HALL'S ANKH; WITH NOTHING BELOW, THE HALL IT IS
		private static void OfferFate( Mobile m, Map map )
		{
			m.CloseGump( typeof( SpiritChoiceGump ) );

			if ( DiesForGood( m ) )
			{
				CastOut( m, map );
				return;
			}

			SoulOrb orb = SoulOrb.FindActive( m );
			bool companions = HasCompanions( m, map );

			if ( orb == null && !companions )
			{
				PullBack( m, map );
				return;
			}

			m.SendGump( FateGump( m, map, Instance.MasterOf( map ).Name, orb, companions ) );
		}

		private static SpiritChoiceGump FateGump( Mobile m, Map map, string master, SoulOrb orb, bool companions )
		{
			List<SpiritChoice> choices = new List<SpiritChoice>();
			string text = String.Format( "{0} calls to your spirit.", master );

			if ( orb != null )
			{
				text += String.Format( " The {0} you carry can raise you where you fell.", orb.Name );
				choices.Add( new SpiritChoice( String.Format( "Use the {0}", orb.Name ), delegate
				{
					if ( IsFallen( m, map ) )
						orb.Release( m );
				} ) );
			}

			if ( companions )
			{
				text += " Your companions still stand, and may yet raise you if you linger.";
				choices.Add( new SpiritChoice( "Wait for my companions", delegate
				{
					if ( IsFallen( m, map ) )
						m.SendMessage( 0x22, "Your spirit lingers below. Your companions may yet raise you." );
				} ) );
			}

			text += " Or he can draw you back to his hall, body and all, to rise at his ankh.";
			choices.Add( new SpiritChoice( "Return to the hall", delegate { PullBack( m, map ); } ) );

			return new SpiritChoiceGump( m, "YOU HAVE FALLEN", text, companions, choices.ToArray() );
		}

		// A STEALTHED COMPANION COUNTS; HIDDEN STAFF DO NOT
		private static bool HasCompanions( Mobile m, Map map )
		{
			foreach ( Mobile other in PlayersOn( map ) )
			{
				if ( other != m && other.Alive && ( !other.Hidden || other.AccessLevel == AccessLevel.Player ) )
					return true;
			}

			return false;
		}

		public static void PullBack( Mobile m, Map map )
		{
			if ( !IsFallen( m, map ) )
				return;

			DungeonMaster master = Instance.MasterOf( map );
			MoveWithCorpse( m, map, DungeonLobbyInstanceType.Instance.Landing, master.Map );

			m.SendMessage( 0x22, "Your spirit and your body are drawn back to the Dungeon Master's hall." );
			master.Speak( DungeonMaster.Remark.PlayerDied, m );
		}

		// NO ANKH RAISES THOSE WHO DIE FOR GOOD: THEY ARE LEFT, BODY AND ALL, WHERE THEY FIRST STEPPED INTO THE HALL
		private static void CastOut( Mobile m, Map map )
		{
			DungeonMaster master = Instance.MasterOf( map );
			bool known = master.ReturnMap != null && master.ReturnMap != Map.Internal;

			MoveWithCorpse( m, map, known ? master.ReturnPoint : Instance.ExitPoint, known ? master.ReturnMap : Instance.ExitMap );
			m.SendMessage( 0x22, "The Dungeon Master has no use for the truly dead. Your spirit and your body are cast out of his hall, back to where you entered." );
		}

		private static void MoveWithCorpse( Mobile m, Map from, Point3D p, Map to )
		{
			Container corpse = m.Corpse;

			Teleport( m, p, to );
			if ( corpse != null && !corpse.Deleted && Object.ReferenceEquals( corpse.Map, from ) )
				corpse.MoveToWorld( p, to );
		}

		protected override bool CanHost( Instance inst, Map map )
		{
			return map != null && Object.ReferenceEquals( map.BaseMap, GetRun( inst ).Definition.Map );
		}

		protected override Point3D GetLandingPoint( Instance inst )
		{
			return GetRun( inst ).Definition.Landing;
		}

		public bool SendToConfiguredInstance( Mobile from, Serial instanceKey, DungeonInstanceDefinition definition, Difficulty difficulty, Map returnMap, Point3D returnPoint, TimeSpan timeLimit )
		{
			if ( definition == null )
			{
				from.SendMessage( "This dungeon instance gate is not configured with a valid dungeon." );
				return false;
			}

			DungeonRun run = new DungeonRun( definition, difficulty, timeLimit );
			Instance inst = GetByKey( instanceKey );

			if ( inst != null && !run.Matches( GetRun( inst ) ) )
			{
				if ( HasPlayersInside( inst ) )
				{
					from.SendMessage( "This gate already has an active {0} instance; sending you there.", GetRun( inst ).Definition.Name );
					return Enter( from, inst );
				}

				FreeInstance( inst );
				inst = null;
			}

			if ( inst == null )
			{
				inst = GetOrCreateByKey( instanceKey );
				inst.Data = run;
				inst.ReturnMap = returnMap;
				inst.ReturnPoint = returnPoint;
			}

			if ( Enter( from, inst ) )
				return true;

			FreeInstance( inst );
			return false;
		}

		private bool Enter( Mobile from, Instance inst )
		{
			if ( !SendToInstance( from, inst ) )
				return false;

			GiveSigil( from, inst );
			from.SendMessage( 0x22, "The sigil in your pack, like the stone by the entrance, marks how far you have come." );
			GetRun( inst ).State.SendTimeRemaining( from );
			return true;
		}

		private static void GiveSigil( Mobile m, Instance inst )
		{
			Container pack = m.Backpack;
			if ( !m.Player || pack == null )
				return;

			foreach ( Item item in pack.Items )
			{
				DungeonRunSigil sigil = item as DungeonRunSigil;
				if ( sigil != null && sigil.Run == inst.OwnerSerial )
					return;
			}

			pack.DropItem( new DungeonRunSigil( inst.OwnerSerial, GetRun( inst ).Definition.Name ) );
		}

		public bool SendProgress( Mobile from )
		{
			RuntimeDungeonState state = GetState( from.Map );
			if ( state == null )
				return false;

			from.CloseGump( typeof( DungeonSigilGump ) );
			from.SendGump( new DungeonSigilGump( state.GetProgress() ) );
			return true;
		}

		public bool HasInstance( Serial instanceKey )
		{
			return GetByKey( instanceKey ) != null;
		}

		public DungeonInstanceDefinition GetRandomDefinition()
		{
			return Definitions.Count > 0 ? Definitions[Utility.Random( Definitions.Count )] : null;
		}

		public bool SummonBoss( Mobile near )
		{
			RuntimeDungeonState state = GetState( near.Map );
			return state != null && state.ForceBoss( near.Location );
		}

		private void ExpireInstance( Map map )
		{
			Instance inst = GetLiveInstance( map );
			if ( inst != null )
				FreeInstance( inst );
		}

		public Instance BuildAuditInstance( DungeonInstanceDefinition definition, Serial key )
		{
			CleanupInstance( key );

			Instance inst = GetOrCreateByKey( key );
			inst.Data = new DungeonRun( definition, Difficulty.Normal, TimeSpan.Zero );

			if ( EnsureLive( inst ) )
				return inst;

			FreeInstance( inst );
			return null;
		}

		public bool CleanupInstance( Serial instanceKey )
		{
			Instance inst = GetByKey( instanceKey );
			if ( inst == null )
				return false;

			FreeInstance( inst );
			return true;
		}

		public int ClearAllInstances()
		{
			List<Instance> all = new List<Instance>( AllInstances );
			foreach ( Instance inst in all )
				FreeInstance( inst );

			return all.Count;
		}

		public string GetInstanceStatus( Serial instanceKey )
		{
			Instance inst = GetByKey( instanceKey );
			DungeonRun run = GetRun( inst );

			if ( run == null )
				return "None";

			if ( !inst.IsLive || run.State == null )
				return String.Format( "Waiting: {0}", run.Definition.Name );

			return String.Format( "Live: {0} on map {1}, {2}/{3} points, {4}, {5}", run.Definition.Name, inst.LiveMapIndex,
				run.State.KilledPoints, run.State.TotalPoints, run.State.BossLabel, run.State.ClockLabel );
		}

		// WALKING OFF THE EDGE LEAVES ONLY DUNGEONS WITH NO EXIT TELEPORTER; ELSEWHERE IT IS A WALL
		public override void OnWalkIntoVoid( Mobile m, Map map )
		{
			DungeonRun run = GetRun( GetLiveInstance( map ) );
			if ( !m.Player || run == null || !run.Definition.WalkOutExits )
				return;

			Timer.DelayCall( TimeSpan.Zero, delegate
			{
				if ( !m.Deleted && Object.ReferenceEquals( m.Map, map ) )
					LeaveInstance( m );
			} );
		}

		// MONSTERS LEAVE NO CORPSE OR LOOT HERE; PETS KEEP THEIRS
		public override void OnCreatureDeath( BaseCreature bc, Container corpse )
		{
			if ( bc.Controlled || bc.ControlMaster != null || bc.IsBonded )
				return;

			RuntimeDungeonState state = GetState( bc.Map );
			if ( state != null )
				state.CreditKill( bc );

			// DEFERRED: OVERRIDES STILL ADD LOOT TO THE CORPSE AFTER THIS
			if ( corpse != null )
				Timer.DelayCall( TimeSpan.Zero, delegate { corpse.Delete(); } );
		}

		public override bool TryGetRandomPlayablePoint( Map map, out Point3D p )
		{
			DungeonRun run = GetRun( GetLiveInstance( map ) );
			p = Point3D.Zero;

			if ( run == null )
				return false;

			if ( !run.Definition.TryGetRandomStandable( map, out p ) )
				p = run.Definition.Landing;

			return true;
		}

		protected override void BuildContents( Instance inst, Map map )
		{
			ClearMap( map, inst );

			DungeonRun run = GetRun( inst );
			List<SpawnEntry> entries = new List<SpawnEntry>();
			List<BaseRegion> regions = CopyRegions( run.Definition, map, entries );

			// EVERYTHING SPAWNS ONCE, UP FRONT, AND NOTHING RESPAWNS
			foreach ( SpawnEntry entry in entries )
				entry.Respawn();

			foreach ( BaseRegion region in regions )
			{
				if ( region.Spawns == null )
					continue;

				foreach ( SpawnEntry entry in region.Spawns )
				{
					foreach ( ISpawnable spawned in entry.SpawnedObjects )
						spawned.Spawner = null;

					entry.SpawnedObjects.Clear();
				}

				region.Spawns = null;
			}

			CloneWorldContent( map, run.Definition );
			DungeonSigil.Place( map, run.Definition );
			EnsureExitAtLanding( map, run.Definition.Landing );

			run.State = new RuntimeDungeonState( run, map );
			run.State.ApplyDifficulty();
			run.State.TrackMonsters();
			run.State.StartClock();
			run.State.Start( regions );
		}

		// WHERE A DUNGEON'S OWN WAY OUT IS NOT RIGHT BY THE LANDING, A GATE THERE ALWAYS LEADS BACK
		private static void EnsureExitAtLanding( Map map, Point3D landing )
		{
			IPooledEnumerable eable = map.GetItemsInRange( landing, 3 );
			bool near = false;
			foreach ( Item item in eable )
			{
				if ( item is DungeonInstanceExitTeleporter )
				{
					near = true;
					break;
				}
			}
			eable.Free();

			if ( near )
				return;

			DungeonInstanceExitTeleporter gate = new DungeonInstanceExitTeleporter( 0xF6C );
			gate.Name = "the way out";
			gate.Hue = 0;
			gate.Light = LightType.Circle300;
			gate.MoveToWorld( landing, map );
		}

		protected override void OnFreeInstance( Instance inst, Map liveMap )
		{
			DungeonRun run = GetRun( inst );
			if ( run != null && run.State != null )
				run.State.Delete();

			if ( liveMap != null )
				ClearMap( liveMap, inst );
		}

		public override void OnSave()
		{
			List<Instance> live = new List<Instance>();
			foreach ( Instance inst in LiveInstances )
			{
				if ( GetRun( inst ) != null && GetRun( inst ).State != null )
					live.Add( inst );
			}

			Persistence.Serialize( InstanceSavePath, delegate( GenericWriter writer )
			{
				writer.Write( (int) 0 ); // version

				writer.Write( (int) live.Count );
				foreach ( Instance inst in live )
				{
					DungeonRun run = GetRun( inst );

					writer.Write( (int) inst.OwnerSerial );
					writer.Write( (int) inst.LiveMapIndex );
					writer.Write( (string) run.Definition.DisplayName );
					writer.Write( (int) run.Difficulty );
					writer.Write( (TimeSpan) run.TimeLimit );
					writer.Write( (Map) inst.ReturnMap );
					writer.Write( (Point3D) inst.ReturnPoint );
					run.State.Serialize( writer );
				}
			} );
		}

		// SAVED RUNS COME BACK ON THE MAPS THEY WERE SAVED ON; ANYTHING ELSE ON A POOL MAP IS CLEARED
		public override void OnLoad()
		{
			HashSet<int> restored = new HashSet<int>();

			try
			{
				Persistence.Deserialize( InstanceSavePath, delegate( GenericReader reader )
				{
					if ( reader.End() )
						return;

					reader.ReadInt(); // version
					int count = reader.ReadInt();

					for ( int i = 0; i < count; i++ )
					{
						Serial key = (Serial)reader.ReadInt();
						Map map = Map.Maps[reader.ReadInt()];
						DungeonRun run = new DungeonRun( FindDefinition( reader.ReadString() ), (Difficulty)reader.ReadInt(), reader.ReadTimeSpan() );
						Map returnMap = reader.ReadMap();
						Point3D returnPoint = reader.ReadPoint3D();

						run.State = new RuntimeDungeonState( run, map );
						run.State.Deserialize( reader );

						if ( Restore( key, run, map, returnMap, returnPoint ) )
							restored.Add( map.MapIndex );
					}
				} );
			}
			catch ( Exception e )
			{
				Console.WriteLine( "[Instancing] Dungeon instances could not be loaded and will be cleared: {0}", e.Message );
			}

			ClearPoolMaps( restored );
		}

		private bool Restore( Serial key, DungeonRun run, Map map, Map returnMap, Point3D returnPoint )
		{
			if ( run.Definition == null || !IsPoolMap( map ) || !Object.ReferenceEquals( map.BaseMap, run.Definition.Map ) || GetByKey( key ) != null )
				return false;

			Instance inst = GetOrCreateByKey( key );
			inst.Data = run;
			inst.ReturnMap = returnMap;
			inst.ReturnPoint = returnPoint;
			inst.LastTouched = DateTime.Now + RestartGrace - UnloadAfter;

			if ( !RestoreLive( inst, map.MapIndex ) )
			{
				FreeInstance( inst );
				return false;
			}

			run.State.MarkAllScaled();
			run.State.Start( CopyRegions( run.Definition, map, null ) );
			return true;
		}
	}
}
