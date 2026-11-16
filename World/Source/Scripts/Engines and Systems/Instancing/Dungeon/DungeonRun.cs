using System;
using System.Collections.Generic;
using Server;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Mobiles;
using Server.Misc;
using Server.Network;
using Server.Regions;
using Server.Timers;

namespace Server.Engines.Instancing
{
	public partial class DungeonInstanceType
	{
		private sealed class DungeonRun
		{
			public readonly DungeonInstanceDefinition Definition;
			public readonly Difficulty Difficulty;
			public readonly TimeSpan TimeLimit;
			public RuntimeDungeonState State;

			public DungeonRun( DungeonInstanceDefinition definition, Difficulty difficulty, TimeSpan timeLimit )
			{
				Definition = definition;
				Difficulty = ClampDifficulty( difficulty );
				TimeLimit = timeLimit > TimeSpan.Zero ? timeLimit : TimeSpan.Zero;
			}

			public bool Timed { get { return TimeLimit > TimeSpan.Zero; } }

			public bool Matches( DungeonRun other )
			{
				return other != null && Object.ReferenceEquals( Definition, other.Definition ) && Difficulty == other.Difficulty && TimeLimit == other.TimeLimit;
			}
		}

		// MONSTERS ARE WORTH THEIR LEVEL IN POINTS; 70% OF THE POINTS WAKES THE BOSS, WHOSE DEATH WINS THE RUN
		private sealed class RuntimeDungeonState
		{
			private const int BossThresholdPercent = 70;
			private const int MinMonsters = 25;

			private static readonly int[] OmenPercents = new int[] { 20, 40, 60, 80 };

			private static readonly string[] Omens = new string[]
			{
				"A low groan rolls through the stone. Something deep below has noticed you.",
				"The torches gutter and the air turns cold. The master of this place stirs in its sleep.",
				"Dust sifts down from above as heavy footsteps pace somewhere in the dark.",
				"The walls seem to breathe around you. Its patience is nearly at an end."
			};

			// WHAT THE SIGIL SAYS OF THE MASTER BEFORE IT WAKES, ONE FOR EACH OMEN REACHED
			private static readonly string[] MasterStates = new string[] { "Sleeping", "Dreaming", "Stirring", "Restless", "Awakening" };

			private const string WakeOmen = "The ground heaves and a terrible voice fills the halls. The master of this place has awoken!";

			private static readonly TimeSpan[] ClockWarnings = new TimeSpan[] { TimeSpan.FromMinutes( 10 ), TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 1 ) };
			private static readonly TimeSpan MinimumRestoredTime = TimeSpan.FromMinutes( 2 );

			private enum BossPhase { Dormant, Awake, Slain }

			private readonly DungeonRun m_Run;
			private readonly Map m_Map;
			private readonly HashSet<Serial> m_Scaled = new HashSet<Serial>();
			private readonly Dictionary<Mobile, int> m_Monsters = new Dictionary<Mobile, int>();
			private List<BaseRegion> m_Regions = new List<BaseRegion>();
			private int m_KilledPoints;
			private int m_Omens;
			private Timer m_Timer;
			private bool m_Deleted;

			private BossPhase m_BossPhase;
			private BaseCreature m_Boss;
			private Type m_BossType;
			private bool m_BossPending;

			private DateTime m_Deadline;
			private TimeSpan m_StoppedAt;
			private int m_NextWarning;
			private bool m_Expired;

			public RuntimeDungeonState( DungeonRun run, Map map )
			{
				m_Run = run;
				m_Map = map;
			}

			private bool Won { get { return m_BossPhase == BossPhase.Slain; } }
			private int Level { get { return (int)m_Run.Difficulty; } }

			public int KilledPoints { get { return m_KilledPoints; } }

			public int TotalPoints
			{
				get
				{
					int total = m_KilledPoints;
					foreach ( int points in m_Monsters.Values )
						total += points;

					return total;
				}
			}

			// HOW CLOSE THE MASTER IS TO WAKING: 100% IS THE KILLS THAT WAKE IT
			public int ProgressPercent
			{
				get
				{
					long needed = (long)TotalPoints * BossThresholdPercent;
					return needed > 0 ? (int)Math.Min( 100, m_KilledPoints * 10000L / needed ) : 0;
				}
			}

			public string BossLabel
			{
				get
				{
					if ( m_BossPhase == BossPhase.Slain )
						return "boss slain";

					if ( m_BossPhase == BossPhase.Awake )
						return m_Boss != null && !m_Boss.Deleted ? String.Format( "boss {0} up ({1}/{2} hits)", m_Boss.Name, m_Boss.Hits, m_Boss.HitsMax ) : "boss waking";

					return String.Format( "boss at {0}% ({1})", BossThresholdPercent, m_BossType != null ? m_BossType.Name : "none" );
				}
			}

			public DungeonProgress GetProgress()
			{
				DungeonProgress p = new DungeonProgress();
				p.Dungeon = m_Run.Definition.Name;
				p.Difficulty = m_Run.Difficulty;
				p.Percent = ProgressPercent;
				p.Risen = m_BossPhase != BossPhase.Dormant;
				p.Clock = m_Run.Timed ? ClockLabel : "Untimed";

				if ( m_BossPhase == BossPhase.Slain )
				{
					p.Master = "Slain";
					p.Omen = "The master of this place is dead. The sigil has gone still.";
				}
				else if ( m_BossPhase == BossPhase.Awake )
				{
					p.MasterAwake = m_Boss != null && !m_Boss.Deleted;
					p.Master = p.MasterAwake ? String.Format( "{0} {1}", m_Boss.Name, m_Boss.Title ) : MasterStates[Omens.Length];
					p.Omen = WakeOmen;
				}
				else
				{
					p.Master = MasterStates[m_Omens];
					p.Omen = m_Omens > 0 ? Omens[m_Omens - 1] : "The sigil is quiet. Nothing below has noticed you yet.";
				}

				return p;
			}

			public void Start( List<BaseRegion> regions )
			{
				m_Regions = regions;
				m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromSeconds( 5.0 ), new TimerCallback( OnTick ) );
			}

			private void OnTick()
			{
				ApplyDifficulty();
				PruneMonsters();
				CheckClock();
				CheckOmens();
				CheckBoss( Point3D.Zero, false );
			}

			public void Delete()
			{
				m_Deleted = true;

				if ( m_Timer != null )
					m_Timer.Stop();

				// UNREGISTERING A REGION DELETES ITS SPAWN ENTRIES
				foreach ( BaseRegion region in m_Regions )
				{
					if ( region.Registered )
						region.Unregister();
				}
			}

			private void Broadcast( int hue, string text )
			{
				foreach ( Mobile m in PlayersOn( m_Map ) )
					m.SendMessage( hue, text );
			}

			private static bool IsScalable( BaseCreature bc )
			{
				return bc != null && !bc.Deleted && !bc.Controlled && !bc.Summoned && bc.ControlMaster == null && bc.SummonMaster == null;
			}

			private static bool IsCountedMonster( BaseCreature bc )
			{
				return IsScalable( bc ) && !bc.Blessed && !( bc is BaseVendor ) && bc.AI != AIType.AI_Citizen;
			}

			public void ApplyDifficulty()
			{
				foreach ( Rectangle3D rect in m_Run.Definition.Area )
				{
					IPooledEnumerable eable = m_Map.GetMobilesInBounds( new Rectangle2D( rect.Start.X, rect.Start.Y, rect.Width, rect.Height ) );
					foreach ( Mobile m in eable )
					{
						BaseCreature bc = m as BaseCreature;
						if ( IsScalable( bc ) && m_Scaled.Add( bc.Serial ) )
							BaseCreature.BeefUp( bc, m_Run.Difficulty, false );
					}
					eable.Free();
				}
			}

			// CREATURES RESTORED FROM A SAVE WERE SCALED BEFORE IT
			public void MarkAllScaled()
			{
				foreach ( Mobile m in World.Mobiles.Values )
				{
					if ( Object.ReferenceEquals( m.Map, m_Map ) && IsScalable( m as BaseCreature ) )
						m_Scaled.Add( m.Serial );
				}
			}

			public void TrackMonsters()
			{
				foreach ( Mobile m in World.Mobiles.Values )
				{
					BaseCreature bc = m as BaseCreature;
					if ( bc == null || !Object.ReferenceEquals( bc.Map, m_Map ) )
						continue;

					// A PREMIUM SPAWNER RESTARTS ITSELF WHEN ITS CREATURE DIES
					bc.SpawnerID = 0;

					if ( IsCountedMonster( bc ) )
						m_Monsters[bc] = Math.Max( 1, IntelligentAction.GetCreatureLevel( bc ) );
				}

				TopUp();
				ChooseBossType();
			}

			// A DUNGEON THAT IS ALMOST EMPTY GETS MORE OF WHAT ALREADY LIVES THERE
			private void TopUp()
			{
				List<Type> kinds = new List<Type>();
				foreach ( Mobile m in m_Monsters.Keys )
				{
					if ( m.GetType().GetConstructor( Type.EmptyTypes ) != null )
						kinds.Add( m.GetType() );
				}

				for ( int count = m_Monsters.Count; kinds.Count > 0 && count < MinMonsters; count++ )
				{
					Point3D p;
					if ( !m_Run.Definition.TryGetRandomStandable( m_Map, out p ) )
						return;

					BaseCreature bc = (BaseCreature)Activator.CreateInstance( kinds[Utility.Random( kinds.Count )] );
					bc.Home = p;
					bc.RangeHome = 10;
					bc.MoveToWorld( p, m_Map );

					m_Scaled.Add( bc.Serial );
					BaseCreature.BeefUp( bc, m_Run.Difficulty, false );

					m_Monsters[bc] = Math.Max( 1, IntelligentAction.GetCreatureLevel( bc ) );
				}
			}

			public bool CreditKill( BaseCreature bc )
			{
				if ( Object.ReferenceEquals( bc, m_Boss ) )
				{
					OnBossSlain( bc );
					return true;
				}

				int points;
				if ( !m_Monsters.TryGetValue( bc, out points ) )
					return false;

				m_Monsters.Remove( bc );
				m_KilledPoints += points;

				CheckOmens();
				CheckBoss( bc.Location, true );
				return true;
			}

			// ONE OMEN PER THRESHOLD CROSSED, ONLY THE LATEST IF A KILL CROSSES SEVERAL
			private void CheckOmens()
			{
				if ( m_BossPhase != BossPhase.Dormant || m_Expired )
					return;

				int reached = OmensReached();
				if ( reached > m_Omens )
				{
					m_Omens = reached;
					Broadcast( 0x22, Omens[m_Omens - 1] );
				}
			}

			private int OmensReached()
			{
				int percent = ProgressPercent;
				int reached = 0;

				while ( reached < OmenPercents.Length && percent >= OmenPercents[reached] )
					reached++;

				return reached;
			}

			// TAMED, DELETED OR LOST MONSTERS LEAVE THE TOTAL
			private void PruneMonsters()
			{
				List<Mobile> gone = new List<Mobile>();
				foreach ( Mobile m in m_Monsters.Keys )
				{
					if ( !IsCountedMonster( m as BaseCreature ) || !Object.ReferenceEquals( m.Map, m_Map ) )
						gone.Add( m );
				}

				foreach ( Mobile m in gone )
					m_Monsters.Remove( m );
			}

			private void ChooseBossType()
			{
				int best = -1;

				foreach ( KeyValuePair<Mobile, int> kvp in m_Monsters )
				{
					BaseCreature bc = (BaseCreature)kvp.Key;
					if ( kvp.Value > best && !bc.NoWalker && bc.GetType().GetConstructor( Type.EmptyTypes ) != null )
					{
						best = kvp.Value;
						m_BossType = bc.GetType();
					}
				}
			}

			private void CheckBoss( Point3D near, bool hasNear )
			{
				if ( m_Deleted || m_Expired || m_BossPending )
					return;

				int total = TotalPoints;

				if ( m_BossPhase == BossPhase.Dormant && total > 0 && m_KilledPoints * 100L >= (long)total * BossThresholdPercent )
					WakeBoss( near, hasNear );
				else if ( m_BossPhase == BossPhase.Awake && ( m_Boss == null || m_Boss.Deleted ) )
					ScheduleBoss( near, hasNear, TimeSpan.Zero );
			}

			public bool ForceBoss( Point3D near )
			{
				if ( m_Deleted || m_Expired || m_BossPending || m_BossPhase != BossPhase.Dormant )
					return false;

				WakeBoss( near, true );
				return true;
			}

			private void WakeBoss( Point3D near, bool hasNear )
			{
				m_BossPhase = BossPhase.Awake;
				Broadcast( 0x22, WakeOmen );

				if ( hasNear )
					Effects.PlaySound( near, m_Map, 0x2F3 );

				ScheduleBoss( near, hasNear, TimeSpan.FromSeconds( 3.0 ) );
			}

			private void ScheduleBoss( Point3D near, bool hasNear, TimeSpan delay )
			{
				m_BossPending = true;

				Timer.DelayCall( delay, delegate
				{
					m_BossPending = false;

					if ( !m_Deleted && !m_Expired && m_BossPhase == BossPhase.Awake && ( m_Boss == null || m_Boss.Deleted ) )
						SpawnBoss( hasNear ? near : PickBossOrigin() );
				} );
			}

			private Point3D PickBossOrigin()
			{
				foreach ( Mobile m in PlayersOn( m_Map ) )
				{
					if ( m.Alive && m.AccessLevel == AccessLevel.Player )
						return m.Location;
				}

				Point3D p;
				return m_Run.Definition.TryGetRandomStandable( m_Map, out p ) ? p : m_Run.Definition.Landing;
			}

			// A FREE SPOT A FEW STEPS AWAY, IN SIGHT OF THE KILL THAT WOKE IT
			private Point3D FindBossSpot( Point3D near )
			{
				for ( int i = 0; i < 30; i++ )
				{
					int x = near.X + Utility.RandomMinMax( -4, 4 );
					int y = near.Y + Utility.RandomMinMax( -4, 4 );
					int z = near.Z;

					if ( ( x == near.X && y == near.Y ) || !m_Run.Definition.Contains( new Point3D( x, y, z ) ) )
						continue;

					if ( !m_Map.CanFit( x, y, z, 16, false, false ) )
					{
						z = m_Map.GetAverageZ( x, y );
						if ( Math.Abs( z - near.Z ) > 8 || !m_Map.CanFit( x, y, z, 16, false, false ) )
							continue;
					}

					if ( m_Map.LineOfSight( new Point3D( near.X, near.Y, near.Z + 10 ), new Point3D( x, y, z + 10 ) ) )
						return new Point3D( x, y, z );
				}

				return near;
			}

			private BaseCreature CreateBoss()
			{
				BaseCreature bc = null;

				try
				{
					if ( m_BossType != null )
						bc = Activator.CreateInstance( m_BossType ) as BaseCreature;
				}
				catch
				{
				}

				return bc ?? new Daemon();
			}

			private void Empower( BaseCreature boss )
			{
				int d = Level;

				string name = NameList.RandomName( "demonic" );
				if ( !String.IsNullOrEmpty( name ) )
					boss.Name = name;

				boss.Title = RandomThings.RandomEvilTitle();
				boss.NameHue = 0x22;
				boss.Tamable = false;

				int statFloor = 150 + 50 * d;
				boss.RawStr = Math.Max( (int)( boss.RawStr * 1.5 ), statFloor );
				boss.RawDex = Math.Max( (int)( boss.RawDex * 1.5 ), statFloor );
				boss.RawInt = Math.Max( (int)( boss.RawInt * 1.5 ), statFloor );
				boss.HitsMaxSeed = Math.Min( 15000, Math.Max( boss.HitsMax * 3, 300 + 150 * d ) );

				double skillFloor = Math.Min( 120.0, 75.0 + 9.0 * d );
				for ( int i = 0; i < boss.Skills.Length; i++ )
				{
					if ( boss.Skills[i].Base > 0.0 && boss.Skills[i].Base < skillFloor )
						boss.Skills[i].Base = skillFloor;
				}

				boss.DamageMin = Math.Max( boss.DamageMin, 8 + 3 * d );
				boss.DamageMax = Math.Max( boss.DamageMax, boss.DamageMin + 8 );
				boss.VirtualArmor = Math.Max( boss.VirtualArmor, 30 + 5 * d );

				boss.Hits = boss.HitsMax;
				boss.Stam = boss.StamMax;
				boss.Mana = boss.ManaMax;
			}

			private void SpawnBoss( Point3D near )
			{
				Point3D p = FindBossSpot( near );
				BaseCreature boss = CreateBoss();

				BaseCreature.BeefUp( boss, m_Run.Difficulty, false );
				Empower( boss );
				boss.Home = p;
				boss.RangeHome = 10;
				boss.MoveToWorld( p, m_Map );
				m_Scaled.Add( boss.Serial );
				m_Boss = boss;

				Effects.SendLocationParticles( EffectItem.Create( p, m_Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 2023 );
				Effects.SendLocationParticles( EffectItem.Create( p, m_Map, EffectItem.DefaultDuration ), 0x3709, 10, 30, 5052 );
				Effects.PlaySound( p, m_Map, 0x1FE );

				Mobile target = null;
				IPooledEnumerable eable = m_Map.GetMobilesInRange( p, 12 );
				foreach ( Mobile m in eable )
				{
					if ( m.Player && m.Alive && m.AccessLevel == AccessLevel.Player && ( target == null || m.GetDistanceToSqrt( p ) < target.GetDistanceToSqrt( p ) ) )
						target = m;
				}
				eable.Free();

				if ( target != null )
					boss.Combatant = target;

				Broadcast( 0x22, String.Format( "{0} {1} has risen to defend this place!", boss.Name, boss.Title ) );
			}

			private void OnBossSlain( BaseCreature boss )
			{
				m_StoppedAt = TimeRemaining();
				m_Boss = null;
				m_BossPhase = BossPhase.Slain;

				Difficulty difficulty = m_Run.Difficulty;
				TimeSpan limit = m_Run.TimeLimit;
				DungeonBossChest chest = new DungeonBossChest( Math.Min( 10, 4 + Level ) );
				chest.SetOwner( boss );
				ChampionRewards.AddBossRewards( chest, boss, DungeonMaster.TreasureChestChance( difficulty, limit ),
					DungeonMaster.PowerfulItemChance( difficulty, limit ), DungeonMaster.ArtifactChance( difficulty, limit ) );
				chest.MoveToWorld( boss.Location, m_Map );

				Effects.SendLocationParticles( EffectItem.Create( boss.Location, m_Map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 5008 );
				Effects.PlaySound( boss.Location, m_Map, 0x1F7 );

				// THE QUEST VILLAIN'S SHOWER OF COINS AND GEMS, KEPT INSIDE THE DUNGEON AND THINNED BY THE GOLD PERCENT
				int goldPercent = DungeonMaster.GoldPercent( difficulty, limit );
				int radius = 7 + Level;
				for ( int x = -radius; x <= radius; x++ )
				{
					for ( int y = -radius; y <= radius; y++ )
					{
						Point3D p = new Point3D( boss.X + x, boss.Y + y, boss.Z );
						if ( x * x + y * y <= radius * radius && m_Run.Definition.Contains( p ) && Utility.Random( 100 ) < goldPercent )
							new GoodiesTimer( m_Map, p.X, p.Y ).Start();
					}
				}

				if ( m_Run.Timed )
					Broadcast( 0x3B2, String.Format( "{0} {1} has fallen with {2} to spare! Its treasure is yours.", boss.Name, boss.Title, FormatTime( m_StoppedAt ) ) );
				else
					Broadcast( 0x3B2, String.Format( "{0} {1} has fallen! Its treasure is yours.", boss.Name, boss.Title ) );
			}

			public void StartClock()
			{
				if ( m_Run.Timed )
					SetDeadline( m_Run.TimeLimit );
			}

			private void SetDeadline( TimeSpan remaining )
			{
				m_Deadline = DateTime.Now + remaining;
				m_NextWarning = 0;

				while ( m_NextWarning < ClockWarnings.Length && ClockWarnings[m_NextWarning] >= remaining )
					m_NextWarning++;
			}

			private TimeSpan TimeRemaining()
			{
				if ( !m_Run.Timed )
					return TimeSpan.Zero;

				if ( Won )
					return m_StoppedAt;

				TimeSpan left = m_Deadline - DateTime.Now;
				return left > TimeSpan.Zero ? left : TimeSpan.Zero;
			}

			public string ClockLabel
			{
				get
				{
					if ( !m_Run.Timed )
						return "untimed";

					return String.Format( Won ? "won with {0} left" : "{0} left", FormatTime( TimeRemaining() ) );
				}
			}

			public void SendTimeRemaining( Mobile m )
			{
				if ( m_Run.Timed && !Won && !m_Expired )
					m.SendMessage( 0x22, "You have {0} to defeat the master of this place.", FormatTime( TimeRemaining() ) );
			}

			private static string FormatTime( TimeSpan time )
			{
				int minutes = (int)time.TotalMinutes;
				int seconds = time.Seconds;

				if ( minutes > 0 && seconds == 0 )
					return String.Format( "{0} minute{1}", minutes, minutes == 1 ? "" : "s" );

				if ( minutes > 0 )
					return String.Format( "{0}:{1:00}", minutes, seconds );

				return String.Format( "{0} second{1}", seconds, seconds == 1 ? "" : "s" );
			}

			private void CheckClock()
			{
				if ( !m_Run.Timed || Won || m_Expired )
					return;

				TimeSpan left = TimeRemaining();

				if ( left <= TimeSpan.Zero )
				{
					m_Expired = true;
					Broadcast( 0x22, "Time has run out! The dungeon crumbles away around you." );

					Map map = m_Map;
					Timer.DelayCall( TimeSpan.Zero, delegate { DungeonInstanceType.Instance.ExpireInstance( map ); } );
				}
				else if ( m_NextWarning < ClockWarnings.Length && left <= ClockWarnings[m_NextWarning] )
				{
					Broadcast( 0x22, String.Format( "{0} remain before this place crumbles away.", FormatTime( ClockWarnings[m_NextWarning] ) ) );

					while ( m_NextWarning < ClockWarnings.Length && left <= ClockWarnings[m_NextWarning] )
						m_NextWarning++;
				}
			}

			public void Serialize( GenericWriter writer )
			{
				writer.Write( (int) m_KilledPoints );

				writer.Write( (int) m_Monsters.Count );
				foreach ( KeyValuePair<Mobile, int> kvp in m_Monsters )
				{
					writer.Write( (Mobile) kvp.Key );
					writer.Write( (int) kvp.Value );
				}

				writer.Write( (TimeSpan) TimeRemaining() );
				writer.Write( (string) ( m_BossType != null ? m_BossType.Name : "" ) );
				writer.Write( (int) m_BossPhase );
				writer.Write( (Mobile) m_Boss );
			}

			public void Deserialize( GenericReader reader )
			{
				m_KilledPoints = reader.ReadInt();

				int count = reader.ReadInt();
				for ( int i = 0; i < count; i++ )
				{
					BaseCreature bc = reader.ReadMobile() as BaseCreature;
					int points = reader.ReadInt();

					if ( IsCountedMonster( bc ) && Object.ReferenceEquals( bc.Map, m_Map ) )
						m_Monsters[bc] = points;
				}

				TimeSpan remaining = reader.ReadTimeSpan();
				string bossType = reader.ReadString();
				m_BossType = String.IsNullOrEmpty( bossType ) ? null : ScriptCompiler.FindTypeByName( bossType );
				m_BossPhase = (BossPhase)reader.ReadInt();
				m_Boss = reader.ReadMobile() as BaseCreature;

				if ( m_BossType == null )
					ChooseBossType();

				m_Omens = OmensReached();

				if ( Won )
					m_StoppedAt = remaining;
				else if ( m_Run.Timed )
					SetDeadline( remaining > MinimumRestoredTime ? remaining : MinimumRestoredTime ); // THE CLOCK STOOD STILL WHILE THE SERVER WAS DOWN
			}
		}
	}
}
