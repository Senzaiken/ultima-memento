using System;
using Server.Targeting;
using Server.Network;

namespace Server.Spells.Undead
{
	public class UndeadCurePoisonSpell : UndeadSpell
	{
		public static readonly SpellInfo SpellInfo = new SpellInfo(
				new SpellDefinition
				{
					SpellID = 133,
					IconGraphic = 11459,
					Name = "Disease Curing Concoction",
					PowerWords = "",
					Description = "Cures one of poisonous diseases.",
					ManaCost = 0,
					MinSkill = 45,
					TargetType = TargetFlags.Beneficial
				},
				239,
				9021
			);

		public override double RequiredSkill{ get{ return SpellInfo.SpellDefinition.MinSkill; } }
		public override int RequiredMana{ get{ return SpellInfo.SpellDefinition.ManaCost; } }
		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.0 ); } }

		public UndeadCurePoisonSpell( Mobile caster, Item scroll ) : base( caster, scroll, SpellInfo )
		{
		}
		
		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}
		
		public void Target( Mobile m )
		{
			if ( !Caster.CanSee( m ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if ( CheckBSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );
				
				Poison p = m.Poison;
				
				if ( p != null )
				{
					int chanceToCure = 10000 + Server.Items.BasePotion.EnhancePotions( Caster ) + (int)(Caster.Skills[DamageSkill].Value * 75) - ((p.Level + 1) * 1750);
					chanceToCure /= 100;
					
					if ( chanceToCure > Utility.Random( 100 ) )
					{
						if ( m.CurePoison( Caster ) )
						{
							if ( Caster != m )
								Caster.SendLocalizedMessage( 1010058 ); // You have cured the target of all poisons!
							
							m.SendLocalizedMessage( 1010059 ); // You have been cured of all poisons.
						}
					}
					else
					{
						m.SendLocalizedMessage( 1010060 ); // You have failed to cure your target!
					}
				}
				
				m.FixedParticles( 0x373A, 10, 15, 5012, EffectLayer.Waist );
				m.PlaySound( 0x19C );
			}
			
			FinishSequence();
		}
		
		public class InternalTarget : Target
		{
			private UndeadCurePoisonSpell m_Owner;
			
			public InternalTarget( UndeadCurePoisonSpell owner ) : base( 12, false, TargetFlags.Beneficial )
			{
				m_Owner = owner;
			}
			
			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
				{
					m_Owner.Target( (Mobile)o );
				}
			}
			
			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}
