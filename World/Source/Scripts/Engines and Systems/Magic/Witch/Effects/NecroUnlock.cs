using System;
using Server.Targeting;
using Server.Items;
using Server.Utilities;

namespace Server.Spells.Undead
{
	public class NecroUnlockSpell : UndeadSpell
	{
		public static readonly SpellInfo SpellInfo = new SpellInfo(
				new SpellDefinition
				{
					SpellID = 145,
					IconGraphic = 11468,
					Name = "Tomb Raiding Concoction",
					PowerWords = "",
					Description = "Summons the spirits to unlock something for you.",
					ManaCost = 0,
					MinSkill = 15,
					TargetType = TargetFlags.None
				},
				239,
				9021
			);

		public override double RequiredSkill{ get{ return SpellInfo.SpellDefinition.MinSkill; } }
		public override int RequiredMana{ get{ return SpellInfo.SpellDefinition.ManaCost; } }
		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.0 ); } }

		public NecroUnlockSpell( Mobile caster, Item scroll ) : base( caster, scroll, SpellInfo )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private NecroUnlockSpell m_Owner;

			public InternalTarget( NecroUnlockSpell owner ) : base( 12, false, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				IPoint3D loc = o as IPoint3D;

				if ( loc == null )
					return;

				if ( m_Owner.CheckSequence() ) {
					SpellHelper.Turn( from, o );

					Effects.SendLocationParticles( EffectItem.Create( new Point3D( loc ), from.Map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 5024 );

					Effects.PlaySound( loc, from.Map, 0x17E );

					UnlockUtilities.TrySpellUnlock( from, o, UnlockUtilities.NecroUnlockProfile );
				}

				m_Owner.FinishSequence();
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}
