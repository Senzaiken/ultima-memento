using System;
using Server;
using Server.Items;
using Server.Spells.Elementalism;

namespace Server.Items
{
	public class Elemental_Armor_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Armor_Scroll() : this( (int)Elemental_Armor_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Armor_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Armor_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Bolt_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Bolt_Scroll() : this( (int)Elemental_Bolt_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Bolt_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Bolt_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Mend_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Mend_Scroll() : this( (int)Elemental_Mend_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Mend_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Mend_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Sanctuary_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Sanctuary_Scroll() : this( (int)Elemental_Sanctuary_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Sanctuary_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Sanctuary_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Pain_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Pain_Scroll() : this( (int)Elemental_Pain_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Pain_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Pain_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Protection_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Protection_Scroll() : this( (int)Elemental_Protection_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Protection_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Protection_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Purge_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Purge_Scroll() : this( (int)Elemental_Purge_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Purge_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Purge_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Steed_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Steed_Scroll() : this( (int)Elemental_Steed_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Steed_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Steed_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Call_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Call_Scroll() : this( (int)Elemental_Call_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Call_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Call_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Force_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Force_Scroll() : this( (int)Elemental_Force_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Force_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Force_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Wall_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Wall_Scroll() : this( (int)Elemental_Wall_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Wall_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Wall_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Warp_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Warp_Scroll() : this( (int)Elemental_Warp_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Warp_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Warp_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Field_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Field_Scroll() : this( (int)Elemental_Field_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Field_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Field_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Restoration_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Restoration_Scroll() : this( (int)Elemental_Restoration_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Restoration_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Restoration_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Strike_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Strike_Scroll() : this( (int)Elemental_Strike_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Strike_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Strike_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Void_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Void_Scroll() : this( (int)Elemental_Void_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Void_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Void_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Blast_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Blast_Scroll() : this( (int)Elemental_Blast_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Blast_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Blast_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Echo_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Echo_Scroll() : this( (int)Elemental_Echo_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Echo_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Echo_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Fiend_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Fiend_Scroll() : this( (int)Elemental_Fiend_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Fiend_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Fiend_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Hold_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Hold_Scroll() : this( (int)Elemental_Hold_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Hold_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Hold_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Barrage_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Barrage_Scroll() : this( (int)Elemental_Barrage_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Barrage_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Barrage_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Rune_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Rune_Scroll() : this( (int)Elemental_Rune_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Rune_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Rune_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Storm_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Storm_Scroll() : this( (int)Elemental_Storm_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Storm_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Storm_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Summon_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Summon_Scroll() : this( (int)Elemental_Summon_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Summon_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Summon_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Devastation_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Devastation_Scroll() : this( (int)Elemental_Devastation_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Devastation_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Devastation_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Fall_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Fall_Scroll() : this( (int)Elemental_Fall_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Fall_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Fall_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Gate_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Gate_Scroll() : this( (int)Elemental_Gate_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Gate_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Gate_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Havoc_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Havoc_Scroll() : this( (int)Elemental_Havoc_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Havoc_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Havoc_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Apocalypse_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Apocalypse_Scroll() : this( (int)Elemental_Apocalypse_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Apocalypse_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Apocalypse_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Lord_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Lord_Scroll() : this( (int)Elemental_Lord_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Lord_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Lord_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Soul_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Soul_Scroll() : this( (int)Elemental_Soul_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Soul_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Soul_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
	public class Elemental_Spirit_Scroll : SpellScroll
	{
		public override string DefaultDescription{ get{ return ElementalSpell.SpellDescription( SpellID ); } }

		[Constructable]
		public Elemental_Spirit_Scroll() : this( (int)Elemental_Spirit_Spell.SpellInfo.SpellDefinition.SpellID )
		{
		}

		[Constructable]
		public Elemental_Spirit_Scroll( int id ) : base( id, ElementalSpell.ScrollLook( id, 1 ) )
		{
			Hue = ElementalSpell.ScrollLook( id, 2 );
			Name = ElementalSpell.CommonInfo( id, 2 ) + " Scroll";
		}

		public Elemental_Spirit_Scroll( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}