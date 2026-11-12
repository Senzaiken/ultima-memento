using System;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Elementalism
{
	public class Elemental_Summon_Spell : ElementalSpell
	{
		public static readonly SpellInfo SpellInfo = new SpellInfo(
				new SpellDefinition
				{
					SpellID = 323,
					IconGraphic = 0x2CEC,
					Name = "Elemental Summon",
					PowerWords = "Convoca",
					Description = "A lightning elemental is summoned to serve the caster.",
					ManaCost = 24,
					MinSkill = 52,
					TargetType = TargetFlags.None
				},
				269,
				9020,
				false
			);

		public override SpellCircle Circle { get { return SpellCircle.Sixth; } }
		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 3.25 ); } }

		public Elemental_Summon_Spell( Mobile caster, Item scroll ) : base( caster, scroll, SpellInfo )
		{
		}

		public override bool CheckCast()
		{
			if ( !base.CheckCast() )
				return false;

			if ( (Caster.Followers + 2) > Caster.FollowersMax )
			{
				Caster.SendLocalizedMessage( 1049645 ); // You have too many followers to summon that creature.
				return false;
			}

			return true;
		}

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				TimeSpan duration = TimeSpan.FromMinutes( (Caster.Skills[CastSkill].Value*18)/60 );

				BaseCreature m_Creature = new ElementalSteed();
					m_Creature.Delete();

				string elm = ElementalSpell.GetElement( Caster );

				if ( elm == "air" )
					m_Creature = new ElementalSummonLightning();

				else if ( elm == "earth" )
					m_Creature = new ElementalSummonEnt();

				else if ( elm == "fire" )
					m_Creature = new ElementalSummonMagma();

				else if ( elm == "water" )
					m_Creature = new ElementalSummonIce();

				SpellHelper.Summon( m_Creature, Caster, 0x216, duration, false, false );
				m_Creature.FixedParticles(0x3728, 8, 20, 5042, 0, 0, EffectLayer.Head );
			}

			FinishSequence();
		}
	}
}