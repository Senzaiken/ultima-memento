using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Utilities;
using System.Linq;

namespace Server.Spells.Elementalism
{
	public abstract class ElementalSpell : Spell
	{
		public override SkillName CastSkill{ get{ return SkillName.Elementalism; } }
		public override SkillName DamageSkill{ get{ return SkillName.Elementalism; } }

		public ElementalSpell( Mobile caster, Item scroll, SpellInfo info ): base( caster, scroll, info )
		{
		}

		public abstract SpellCircle Circle { get; }

		public int OneBasedCircle { get { return (int)Circle + 1; } }

		public override bool ConsumeReagents()
		{
			return true;
		}

		public override bool CheckCast()
		{
			if ( !base.CheckCast() )
				return false;

			if ( Caster.Stam < GetStam() )
			{
				Caster.SendMessage( "Insufficient stamina for this spell." );
				Server.Misc.IntelligentAction.FizzleSpell( Caster );
				return false;
			}
			else if ( ArmorFizzle( Caster ) >= Utility.RandomMinMax(1,100) )
			{
				Caster.SendMessage( "You are wearing too much armor to cast that (" + ArmorFizzle( Caster ) + "%)." );
				Server.Misc.IntelligentAction.FizzleSpell( Caster );
				return false;
			}

			return true;
		}

		private const double ChanceOffset = 20.0, ChanceLength = 100.0 / 7.0;

		public override void GetCastSkills( out double min, out double max )
		{
			int circle = (int)Circle;
			double avg = ChanceLength * circle;

			min = avg - ChanceOffset;
			max = avg + ChanceOffset;
		}

		private static int[] m_ManaTable = new int[] { 5, 7, 10, 14, 19, 24, 40, 50 };

		public override int GetMana()
		{
			var manaCost = m_ManaTable[(int)Circle];
			return IsScrollCast ? manaCost / 2 : manaCost;
		}

		public static string GetElement( Mobile m )
		{
			int element = ((PlayerMobile)m).CharacterElement;

			string elm = "air";

			if ( element == 0 )
				elm = "air";

			else if ( element == 1 )
				elm = "earth";

			else if ( element == 2 )
				elm = "fire";

			else if ( element == 3 )
				elm = "water";

			return elm;
		}

		public int GetStam()
		{
			int stam = GetMana();

			double reduce = AosAttributes.GetValue( Caster, AosAttribute.LowerRegCost ) * 0.01;

			stam = stam - ( (int)(stam * reduce) );

			if ( stam < 0 )
				stam = 0;

			return stam;
		}

		public virtual bool CheckResisted( Mobile target )
		{
			double n = GetResistPercent( target );

			n /= 100.0;

			if( n <= 0.0 )
				return false;

			if( n >= 1.0 )
				return true;

			int maxSkill = (1 + (int)Circle) * 10;
			maxSkill += (1 + ((int)Circle / 6)) * 25;

			if( target.Skills[SkillName.MagicResist].Value < maxSkill )
				target.CheckSkill( SkillName.MagicResist, 0.0, target.Skills[SkillName.MagicResist].Cap );

			return (n >= Utility.RandomDouble());
		}

		public virtual double GetResistPercentForCircle( Mobile target, SpellCircle circle )
		{
			double firstPercent = target.Skills[SkillName.MagicResist].Value / 5.0;
			double secondPercent = target.Skills[SkillName.MagicResist].Value - (((Caster.Skills[CastSkill].Value - 20.0) / 5.0) + (1 + (int)circle) * 5.0);

			return (firstPercent > secondPercent ? firstPercent : secondPercent) / 2.0; // Seems should be about half of what stratics says.
		}

		public virtual double GetResistPercent( Mobile target )
		{
			return GetResistPercentForCircle( target, Circle );
		}

		public override TimeSpan CastDelayBase
		{
			get
			{
				return TimeSpan.FromSeconds( (3 + (int)Circle) * CastDelaySecondsPerTick );
			}
		}

		public static void AddWater( Mobile m )
		{
			Item water = new ElementalEffect( Utility.RandomMinMax( 0x5691, 0x569A ), 5.0, null );
			water.Name = "water";
			water.Hue = 0xB3F;
			water.MoveToWorld( m.Location, m.Map );

			int extraWater = Utility.RandomMinMax( 2, 4 );

			for( int i = 0; i < extraWater; i++ )
			{
				Item wet = new ElementalEffect( Utility.RandomMinMax( 0x5691, 0x569A ), 5.0, null );
				wet.Name = "water";
				wet.Hue = 0xB3F;
				wet.MoveToWorld( new Point3D(
				m.X + Utility.RandomMinMax( -1, 1 ),
				m.Y + Utility.RandomMinMax( -1, 1 ),
				m.Z ), m.Map );
			}
		}

		public static int ElementalHue( string element )
		{
			int hue = 0;

			int val = Utility.RandomMinMax( 0, 3 );
				if ( element == "earth" ){ val = 0; }
				else if ( element == "water" ){ val = 3; }
				else if ( element == "air" ){ val = 2; }
				else if ( element == "fire" ){ val = 1; }

			switch ( val )
			{
				case 0:	hue = Utility.RandomList( 0xB79, 0xB51, 0x85D, 0x82E, 0xB61, 0xABE, 0xABF, 0xAC0 ); break; // EARTH
				case 1:	hue = Utility.RandomList( 0xB17, 0x981, 0x86C, 0x775 ); break; // FIRE
				case 2:	hue = Utility.RandomList( 0x8C1, 0xB2B, 0x613, 0xB4D, 0xAFE, 0xAF8, 0x8E4 ); break; // AIR
				case 3:	hue = Utility.RandomList( 0x97F, 0xB3D, 0xB0A, 0x5CE ); break; // WATER
			}

			return hue;
		}

		private static readonly string[] m_SphereNames = new string[] { "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth" };

		public static string CommonInfo( int id, int cat )
		{
			string info = "";

			string shortName = "";
			string longName = "";
			string mantra = "";
			string sphere = m_SphereNames[0];
			string skill = "0";

			SpellDefinition definition = ElementalSpellProvider.GetDefinition( id );
			if ( definition != null )
			{
				shortName = ElementalSpellProvider.GetShortName( definition );
				longName = definition.Name.String;
				mantra = definition.PowerWords.String;
				sphere = m_SphereNames[ElementalSpellProvider.GetCircle( definition )];
				skill = definition.MinSkill.ToString();
			}

			if ( cat == 1 ){ info = shortName; }
			else if ( cat == 2 ){ info = longName; }
			else if ( cat == 3 ){ info = mantra; }
			else if ( cat == 4 ){ }
			else if ( cat == 5 ){ info = longName.ToLower(); }
			else if ( cat == 6 ){ info = sphere; }
			else if ( cat == 7 ){ info = skill; }

			return info;
		}

		public static string SpellDescription( int spell )
		{
			string txt = "This scroll contains an elemental spell, where an example of what it does is provided within the Sphere of Air Elementalism: " + DescriptionInfo( spell, 0x6717 ) + "";

			return txt + " It requires at least a " + CommonInfo( spell, 7 ) + " in Elementalism.";
		}

		// Spell definitions describe the air version of each spell. These are the earth, fire, and water versions of spells that differ by element.
		private static readonly Dictionary<int, string[]> m_ElementDescriptions = new Dictionary<int, string[]>
		{
			{
				Elemental_Armor_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Increases your physical resistance while reducing your other resistances. Active until spell is deactivated by re-casting it.", // Earth
					"Increases your fire resistance while reducing your other resistances. Active until spell is deactivated by re-casting it.", // Fire
					"Increases your cold resistance while reducing your other resistances. Active until spell is deactivated by re-casting it." // Water
				}
			},
			{
				Elemental_Bolt_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Shoots a magical bolt at a target, which deals poison and physical damage.", // Earth
					"Shoots a magical bolt at a target, which deals fire and physical damage.", // Fire
					"Shoots a magical bolt at a target, which deals cold and physical damage." // Water
				}
			},
			{
				Elemental_Pain_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Affects the target with falling rocks, dealing physical damage. The closer the target is to the caster, the more damage is dealt.", // Earth
					"Affects the target with flames, dealing fire damage. The closer the target is to the caster, the more damage is dealt.", // Fire
					"Affects the target with freezing water, dealing cold damage. The closer the target is to the caster, the more damage is dealt." // Water
				}
			},
			{
				Elemental_Purge_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Attempts to cleanse poisons affecting the target.", // Earth
					"Attempts to burn away poisons affecting the target.", // Fire
					"Attempts to wash away poisons affecting the target." // Water
				}
			},
			{
				Elemental_Steed_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Summons a great bear that does not fight but you can ride throughout the land. The creature disappears after a set amount of time and requires a control slot.", // Earth
					"Summons a fiery phoenix that does not fight but you can ride throughout the land. The creature disappears after a set amount of time and requires a control slot.", // Fire
					"Summons a water beetle that does not fight but you can ride throughout the land. The creature disappears after a set amount of time and requires a control slot." // Water
				}
			},
			{
				Elemental_Call_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"A lesser earth elemental is summoned to serve the caster. The elemental disappears after a set amount of time and requires a control slot.", // Earth
					"A lesser fire elemental is summoned to serve the caster. The elemental disappears after a set amount of time and requires a control slot.", // Fire
					"A lesser water elemental is summoned to serve the caster. The elemental disappears after a set amount of time and requires a control slot." // Water
				}
			},
			{
				Elemental_Force_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Hurls a magical rock at a target, dealing physical damage.", // Earth
					"Shoots a ball of fire at a target, dealing some physical damage but mostly fire damage.", // Fire
					"Forces some painful water at a target, dealing some physical damage but mostly cold damage." // Water
				}
			},
			{
				Elemental_Wall_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Creates a temporary wall of mud that blocks movement.", // Earth
					"Creates a temporary wall of flame that blocks movement.", // Fire
					"Creates a temporary wall of seaweed that blocks movement." // Water
				}
			},
			{
				Elemental_Field_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Creates a wall of vines that deals physical and poison damage to all who walk through it.", // Earth
					"Creates a wall of flame that deals fire damage to all who walk through it.", // Fire
					"Creates a wall of water that deals cold damage to all who walk through it." // Water
				}
			},
			{
				Elemental_Strike_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Strikes the target with falling rocks, which deals physical damage.", // Earth
					"Strikes the target with falling lava, which deals physical and fire damage.", // Fire
					"Strikes the target with shards of ice from above, which deals physical and cold damage." // Water
				}
			},
			{
				Elemental_Blast_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Makes a blast of stone hit your target with physical damage, dependent on your elementalism and intelligence. Has a short delay.", // Earth
					"Makes a flaming blast hit your target with fire damage, dependent on your elementalism and intelligence. Has a short delay.", // Fire
					"Makes a watery blast hit your target with cold damage, dependent on your elementalism and intelligence. Has a short delay." // Water
				}
			},
			{
				Elemental_Echo_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Harmful wizard spells cast at you will be reflected back toward the caster based on your elementalism. You will need an emerald to make this spell work.", // Earth
					"Harmful wizard spells cast at you will be reflected back toward the caster based on your elementalism. You will need a ruby to make this spell work.", // Fire
					"Harmful wizard spells cast at you will be reflected back toward the caster based on your elementalism. You will need a sapphire to make this spell work." // Water
				}
			},
			{
				Elemental_Fiend_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Conjures a plant creature that attacks a target based off its combat strength and proximity. It disappears after a set amount of time and requires 2 control slots.", // Earth
					"Conjures a lava ooze creature that attacks a target based off its combat strength and proximity. It disappears after a set amount of time and requires 2 control slots.", // Fire
					"Conjures a watery ooze creature that attacks a target based off its combat strength and proximity. It disappears after a set amount of time and requires 2 control slots." // Water
				}
			},
			{
				Elemental_Hold_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Vines emerge to immobilize the target for a brief amount of time. The target's magic resistance skill affects the duration of the immobilization.", // Earth
					"Flame strands emerge to immobilize the target for a brief amount of time. The target's magic resistance skill affects the duration of the immobilization.", // Fire
					"Squid tentacles emerge to immobilize the target for a brief amount of time. The target's magic resistance skill affects the duration of the immobilization." // Water
				}
			},
			{
				Elemental_Barrage_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Launches an orb of vile swamp gas at the target, dealing significant poison damage.", // Earth
					"Launches a glob of searing plasma at the target, dealing significant fire damage.", // Fire
					"Launches a sphere of mystical water at the target, dealing significant cold damage." // Water
				}
			},
			{
				Elemental_Storm_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Causes a swirling storm of poison ivy around the target, dealing poison and physical damage.", // Earth
					"Creates a volcanic storm of molten magma, causing physical and fire damage.", // Fire
					"Calls forth a typhoon of swirling wind and water, causing physical and cold damage." // Water
				}
			},
			{
				Elemental_Summon_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"An ent is summoned to serve the caster.", // Earth
					"A magma elemental is summoned to serve the caster.", // Fire
					"An ice elemental is summoned to serve the caster." // Water
				}
			},
			{
				Elemental_Devastation_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Summons piles of dirt and mud to fall onto nearby enemies, dealing physical damage.", // Earth
					"Calls down a firey malestrom, damaging nearby enemies with fire damage.", // Fire
					"Conjures a storm of ice shards crashing down on nearby enemies, dealing cold damage." // Water
				}
			},
			{
				Elemental_Fall_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Brings down a ball of swamp gas that affects all targets within a radius around the target location. Physical and poison damage dealt is split amongst all targets.", // Earth
					"Brings down a fire storm that affects all targets within a radius around the target location. The total physical and fire damage dealt is split amongst all targets.", // Fire
					"Brings down a hurricane that affects all targets within a radius around the target location. The total physical and cold damage dealt is split amongst all targets." // Water
				}
			},
			{
				Elemental_Havoc_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Envelopes the target in a swarm of deadly bees, causing a massive amount of physical and poison damage.", // Earth
					"Envelopes the target in a flames, causing a massive amount of physical and fire damage.", // Fire
					"Creates a wave of water to burst from the target, causing a massive amount of physical and cold damage." // Water
				}
			},
			{
				Elemental_Apocalypse_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Erupts a massive amount of mud and dirt from the ground, causing horrific physical damage to nearby foes.", // Earth
					"Calls down a fire storm onto foes near the caster, causing some physical but mostly fire damage.", // Fire
					"Brings down a devastating shower of watery ice onto nearby enemies, causing some physical but mostly cold damage." // Water
				}
			},
			{
				Elemental_Lord_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"A Lord of the Earth is called upon to assist the caster.", // Earth
					"A Lord of the Flame is called upon to assist the caster.", // Fire
					"A Lord of the Sea is called upon to assist the caster." // Water
				}
			},
			{
				Elemental_Spirit_Spell.SpellInfo.SpellDefinition.SpellID, new string[]
				{
					"Summons a earth spirit that attacks a target based off its intelligence and proximity. It disappears after a set amount of time and requires 2 control slots.", // Earth
					"Summons a lava spirit that attacks a target based off its intelligence and proximity. It disappears after a set amount of time and requires 2 control slots.", // Fire
					"Summons a water spirit that attacks a target based off its intelligence and proximity. It disappears after a set amount of time and requires 2 control slots." // Water
				}
			},
		};

		public static string DescriptionInfo( int id, int item )
		{
			SpellDefinition definition = ElementalSpellProvider.GetDefinition( id );
			if ( definition == null )
				return "";

			int element = -1; // AIR

			if ( item == 0x6713 ){ element = 0; }		// EARTH
			else if ( item == 0x6719 ){ element = 1; }	// FIRE
			else if ( item == 0x6715 ){ element = 2; }	// WATER

			string[] descriptions;
			if ( element >= 0 && m_ElementDescriptions.TryGetValue( id, out descriptions ) )
				return descriptions[element];

			return definition.Description.String;
		}

		public static int ArmorCheck( Item item, int val )
		{
			int num = 0;

			if ( item is BaseArmor && ((BaseArmor)item).ArmorAttributes.MageArmor == 0 && ((BaseArmor)item).Attributes.SpellChanneling == 0 )
			{
				if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Leather ){ num = 2 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Skin ){ num = 2 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Wood ){ num = 4 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Scales ){ num = 4 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Skeletal ){ num = 4 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Metal ){ num = 6 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Block ){ num = 6 * val; }
				else if ( CraftResources.GetType( item.Resource ) == CraftResourceType.Fabric ){ /* ADD NOTHING */ }
			}
			return num;
		}

		public static int ArmorFizzle( Mobile m )
		{
			int penalty = 0;

			if ( m.FindItemOnLayer( Layer.OuterTorso ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.OuterTorso ), 5 ); }
			if ( m.FindItemOnLayer( Layer.TwoHanded ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.TwoHanded ), 3 ); }
			if ( m.FindItemOnLayer( Layer.Helm ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Helm ), 2 ); }
			if ( m.FindItemOnLayer( Layer.Arms ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Arms ), 3 ); }
			if ( m.FindItemOnLayer( Layer.OuterLegs ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.OuterLegs ), 4 ); }
			if ( m.FindItemOnLayer( Layer.Neck ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Neck ), 1 ); }
			if ( m.FindItemOnLayer( Layer.Gloves ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Gloves ), 1 ); }
			if ( m.FindItemOnLayer( Layer.Shoes ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Shoes ), 1 ); }
			if ( m.FindItemOnLayer( Layer.Cloak ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Cloak ), 3 ); }
			if ( m.FindItemOnLayer( Layer.Waist ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Waist ), 1 ); }
			if ( m.FindItemOnLayer( Layer.InnerLegs ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.InnerLegs ), 4 ); }
			if ( m.FindItemOnLayer( Layer.InnerTorso ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.InnerTorso ), 4 ); }
			if ( m.FindItemOnLayer( Layer.Pants ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Pants ), 4 ); }
			if ( m.FindItemOnLayer( Layer.Shirt ) != null ){ penalty = penalty + ArmorCheck( m.FindItemOnLayer( Layer.Shirt ), 4 ); }

			return penalty;
		}

		public static int ScrollLook( int id, int cat )
		{
			int info = 0;
			int item = 0;
			int hue = 0;

			if ( id == 300 ){ item = 25676; hue = 0xB94; }
			else if ( id == 301 ){ item = 25677; hue = 0xB64; }
			else if ( id == 302 ){ item = 25678; hue = 0xB40; }
			else if ( id == 303 ){ item = 25679; hue = 0xAE2; }
			else if ( id == 304 ){ item = 25680; hue = 0x983; }
			else if ( id == 305 ){ item = 25677; hue = 0xB94; }
			else if ( id == 306 ){ item = 25678; hue = 0xB64; }
			else if ( id == 307 ){ item = 25679; hue = 0xB40; }
			else if ( id == 308 ){ item = 25680; hue = 0xAE2; }
			else if ( id == 309 ){ item = 25681; hue = 0x983; }
			else if ( id == 310 ){ item = 25676; hue = 0xB72; }
			else if ( id == 311 ){ item = 25678; hue = 0xB94; }
			else if ( id == 312 ){ item = 25679; hue = 0xB64; }
			else if ( id == 313 ){ item = 25680; hue = 0xB40; }
			else if ( id == 314 ){ item = 25681; hue = 0xAE2; }
			else if ( id == 315 ){ item = 25679; hue = 0xB94; }
			else if ( id == 316 ){ item = 25680; hue = 0xB64; }
			else if ( id == 317 ){ item = 25681; hue = 0xB40; }
			else if ( id == 318 ){ item = 25676; hue = 0x983; }
			else if ( id == 319 ){ item = 25677; hue = 0xB72; }
			else if ( id == 320 ){ item = 25680; hue = 0xB94; }
			else if ( id == 321 ){ item = 25681; hue = 0xB64; }
			else if ( id == 322 ){ item = 25676; hue = 0xAE2; }
			else if ( id == 323 ){ item = 25677; hue = 0x983; }
			else if ( id == 324 ){ item = 25681; hue = 0xB94; }
			else if ( id == 325 ){ item = 25676; hue = 0xB40; }
			else if ( id == 326 ){ item = 25677; hue = 0xAE2; }
			else if ( id == 327 ){ item = 25678; hue = 0x983; }
			else if ( id == 328 ){ item = 25676; hue = 0xB64; }
			else if ( id == 329 ){ item = 25677; hue = 0xB40; }
			else if ( id == 330 ){ item = 25678; hue = 0xAE2; }
			else if ( id == 331 ){ item = 25679; hue = 0x983; }

			if ( cat == 1 ){ info = item; }
			else if ( cat == 2 ){ info = hue; }

			return info;
		}

		// Distance between the air, earth, fire, and water icon sets
		private const int ElementIconOffset = 32;

		public static int SpellIcon( int item, int spell )
		{
			SpellDefinition definition = ElementalSpellProvider.GetDefinition( spell );
			if ( definition == null )
				return spell;

			int icon = definition.IconGraphic; // AIR

			if ( item == 0x6713 ){ icon += ElementIconOffset; }			// EARTH
			else if ( item == 0x6719 ){ icon += 2 * ElementIconOffset; }	// FIRE
			else if ( item == 0x6715 ){ icon += 3 * ElementIconOffset; }	// WATER

			return icon;
		}

		public static string FontColor( int item )
		{
			string color = "";

			if ( item == 0x6717 ){ color = "#9484DE"; }			// AIR
			else if ( item == 0x6713 ){ color = "#ADE76b"; }	// EARTH
			else if ( item == 0x6719 ){ color = "#FFAD52"; }	// FIRE
			else if ( item == 0x6715 ){ color = "#189CE7"; }	// WATER

			return color;
		}

		public static void BookCover( Item item, int element )
		{
			if ( item.ArtifactLevel == ArtifactLevel.None )
			{
				if ( element == 0 ){ item.ItemID = 0x6717; }		// AIR
				else if ( element == 1 ){ item.ItemID = 0x6713; }	// EARTH
				else if ( element == 2 ){ item.ItemID = 0x6719; }	// FIRE
				else if ( element == 3 ){ item.ItemID = 0x6715; }	// WATER
			}
		}

		public static bool CanUseBook( Item item, Mobile from, bool msg )
		{
			if ( item != null && from != null )
			{
				if ( item is ElementalSpellbook && from is PlayerMobile )
				{
					int element = ((PlayerMobile)from).CharacterElement;

					if ( element != 0 && item.ItemID == 0x6717 )
					{
						if ( msg ){ from.SendMessage("Your need to focus on air elemental magic to use that!"); }
						return false;
					}
					if ( element != 1 && item.ItemID == 0x6713 )
					{
						if ( msg ){ from.SendMessage("Your need to focus on earth elemental magic to use that!"); }
						return false;
					}
					if ( element != 2 && item.ItemID == 0x6719 )
					{
						if ( msg ){ from.SendMessage("Your need to focus on fire elemental magic to use that!"); }
						return false;
					}
					if ( element != 3 && item.ItemID == 0x6715 )
					{
						if ( msg ){ from.SendMessage("Your need to focus on water elemental magic to use that!"); }
						return false;
					}
				}
			}
			return true;
		}

		public static void UnequipBook( Mobile from )
		{
			if ( from.FindItemOnLayer( Layer.Trinket ) != null )
			{
				if ( from.FindItemOnLayer( Layer.Trinket ) is ElementalSpellbook )
				{
					if ( !CanUseBook( from.FindItemOnLayer( Layer.Trinket ), from, false ) )
					{
						Container pack = from.Backpack;
						from.AddToBackpack( from.FindItemOnLayer( Layer.Trinket ) );
					}
				}
			}
		}

		public static void ChangeBooks( Mobile m, int element )
		{
			WorldUtilities
				.ForEachItem<ElementalSpellbook>( item => item.ArtifactLevel == ArtifactLevel.None && item.EllyOwner == m )
				.ToList()
				.ForEach( item => BookCover( item, element ) );
		}
	}
}
