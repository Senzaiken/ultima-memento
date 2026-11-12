using System;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Elementalism
{
	public class Elemental_Spirit_Spell : ElementalSpell
	{
		public static readonly SpellInfo SpellInfo = new SpellInfo(
				new SpellDefinition
				{
					SpellID = 331,
					IconGraphic = 0x2CF4,
					Name = "Elemental Spirit",
					PowerWords = "Fantoma",
					Description = "Summons a cloud spirit that attacks a target based off its intelligence and proximity. It disappears after a set amount of time and requires 2 control slots.",
					ManaCost = 50,
					MinSkill = 80,
					TargetType = TargetFlags.None
				},
				260,
				9032,
				false
			);

		public override SpellCircle Circle { get { return SpellCircle.Eighth; } }

		public Elemental_Spirit_Spell( Mobile caster, Item scroll ) : base( caster, scroll, SpellInfo )
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
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D p )
		{
			Map map = Caster.Map;

			SpellHelper.GetSurfaceTop( ref p );

			if ( map == null || !map.CanSpawnMobile( p.X, p.Y, p.Z ) )
			{
				Caster.SendLocalizedMessage( 501942 ); // That location is blocked.
			}
			else if ( SpellHelper.CheckTown( p, Caster ) && CheckSequence() )
			{
				int nBenefit = (int)(Caster.Skills[CastSkill].Value / 2);

				TimeSpan duration = TimeSpan.FromSeconds( 90.0 + nBenefit );

				string elm = ElementalSpell.GetElement( Caster );

				if ( elm == "air" )
				{
					BaseCreature.Summon( new ElementalSpiritAir(), false, Caster, new Point3D( p ), 0x57C, duration );
				}
				else if ( elm == "earth" )
				{
					BaseCreature.Summon( new ElementalSpiritEarth(), false, Caster, new Point3D( p ), 0x308, duration );
				}
				else if ( elm == "fire" )
				{
					BaseCreature.Summon( new ElementalSpiritFire(), false, Caster, new Point3D( p ), 0x5CA, duration );
				}
				else if ( elm == "water" )
				{
					BaseCreature.Summon( new ElementalSpiritWater(), false, Caster, new Point3D( p ), 0x026, duration );
				}

				Caster.SendMessage( "You can double click the summoned to dispel them." );
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private Elemental_Spirit_Spell m_Owner;

			public InternalTarget( Elemental_Spirit_Spell owner ) : base( Core.ML ? 10 : 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is IPoint3D )
					m_Owner.Target( (IPoint3D)o );
			}

			protected override void OnTargetOutOfLOS( Mobile from, object o )
			{
				from.SendLocalizedMessage( 501943 ); // Target cannot be seen. Try again.
				from.Target = new InternalTarget( m_Owner );
				from.Target.BeginTimeout( from, TimeoutTime - DateTime.Now );
				m_Owner = null;
			}

			protected override void OnTargetFinish( Mobile from )
			{
				if ( m_Owner != null )
					m_Owner.FinishSequence();
			}
		}
	}
}