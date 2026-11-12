using System;
using Server;
using Server.Items;
using System.Text;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
using Server.Commands;
using Server.Misc;
using Server.Spells;
using Server.Spells.Herbalist;

namespace Server.Items
{
	public class BookDruidBrewing : Item
	{
		[Constructable]
		public BookDruidBrewing( ) : base( 0x5688 )
		{
			Weight = 1.0;
			Name = "Druidic Herbalism";
			Hue = 0x85D;
		}

		public class BookGump : Gump
		{
			public BookGump( Mobile from, int page ): base( 100, 100 )
			{
				string color = "#80d080";
				from.SendSound( 0x55 );

				this.Closable=true;
				this.Disposable=true;
				this.Dragable=true;
				this.Resizable=false;

				AddPage(0);

				AddImage(0, 0, 7005, 2936);
				AddImage(0, 0, 7006);
				AddImage(0, 0, 7024, 2736);
				AddImage(77, 98, 7054);
				AddImage(368, 98, 7054);

				int prev = page - 1;
					if ( prev < 1 ){ prev = 99; }
				int next = page + 1;

				AddButton(72, 45, 4014, 4014, prev, GumpButtonType.Reply, 0);
				AddButton(590, 48, 4005, 4005, next, GumpButtonType.Reply, 0);

				int potion = 0;

				if ( page == 2 ){ potion = 2; }
				else if ( page == 3 ){ potion = 4; }
				else if ( page == 4 ){ potion = 6; }
				else if ( page == 5 ){ potion = 8; }
				else if ( page == 6 ){ potion = 10; }
				else if ( page == 7 ){ potion = 11; }
				else if ( page == 8 ){ potion = 12; }
				else if ( page == 9 ){ potion = 14; }
				else if ( page == 10 ){ potion = 16; }

				// --------------------------------------------------------------------------------

				if ( page == 1 )
				{
					AddHtml( 107, 46, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>DRUIDIC HERBALISM</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 398, 48, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>DRUIDIC HERBALISM</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 78, 75, 248, 318, @"<BODY><BASEFONT Color=" + color + ">Druidic Herbalism is the art of taking natural reagents and creating mixtures that druids can use. You would use your druidism skill to create and use the potions, but some veterinary is needed to help create them and make them more effective in some cases. This book explains the various potions you can make, as well as additional information to manage these mixtures effectively. Unlike other potions, these require jars as the liquid needs a thicker glass to store as it is acidic enough to dissolve bottle glass and even the wood of a keg.</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 372, 75, 248, 318, @"<BODY><BASEFONT Color=" + color + ">You will need a small cauldron to brew these potions. You can also get a belt pouch to store the ingredients, cauldrons, jars, potions, and this book to make them easier to carry. Single click this bag to organize it for easier use of the potions.</BASEFONT></BODY>", (bool)false, (bool)false);
				}
				else
				{
					AddHtml( 107, 46, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>" + potionInfo( potion, 1 ) + "</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 73, 72, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Druidism:</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 267, 72, 47, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 4 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 73, 98, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Veterinary:</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 267, 98, 47, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 5 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddImage(77, 128, Int32.Parse( potionInfo( potion, 2 ) ) );
					AddHtml( 133, 139, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Ingredients</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 73, 180, 246, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 6 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 73, 206, 246, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 7 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 73, 232, 246, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 8 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 73, 258, 245, 133, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 3 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

					potion++;

					AddHtml( 398, 48, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>" + potionInfo( potion, 1 ) + "</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 366, 72, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Druidism:</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 560, 72, 47, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 4 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 366, 98, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Veterinary:</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 560, 98, 47, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 5 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddImage(366, 128, Int32.Parse( potionInfo( potion, 2 ) ) );
					AddHtml( 422, 139, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Ingredients</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 366, 180, 246, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 6 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 366, 206, 246, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 7 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 366, 232, 246, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 8 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 366, 258, 245, 133, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 3 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);
				}
			}

			public override void OnResponse( NetState state, RelayInfo info )
			{
				Mobile from = state.Mobile;
				int page = info.ButtonID;
					if ( page == 99 ){ page = 9; }
					else if ( page > 9 ){ page = 1; }

				if ( info.ButtonID > 0 )
				{
					from.SendGump( new BookGump( from, page ) );
				}
				else
					from.SendSound( 0x55 );
			}

			public static string potionDesc( int potion )
			{
				string txt = "";

				txt = "This is created from druidic herbalism: " + potionInfo( potion, 3 ) + " To use it, one should have a " + potionInfo( potion, 4 ) + " in Druidism and a " + potionInfo( potion, 5 ) + " in Veterinary.";

				return txt;
			}

			public static string potionInfo( int page, int val )
			{
				int spellID = 0;
				string txtSklB = "";
				string txtIngA = "";
				string txtIngB = "";
				string txtIngC = "";

				if ( page == 2 ){ spellID = LureStoneSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "5"; txtIngA = "Moon Crystal"; txtIngB = "Silver Widow"; txtIngC = ""; }
				else if ( page == 3 ){ spellID = NaturesPassageSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "10"; txtIngA = "Sea Salt"; txtIngB = "Fairy Egg"; txtIngC = ""; }
				else if ( page == 4 ){ spellID = ShieldOfEarthSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "15"; txtIngA = "Ginseng"; txtIngB = "Black Pearl"; txtIngC = ""; }
				else if ( page == 5 ){ spellID = WoodlandProtectionSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "20"; txtIngA = "Garlic"; txtIngB = "Swamp Berries"; txtIngC = ""; }
				else if ( page == 6 ){ spellID = StoneCircleSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "25"; txtIngA = "Beetle Shell"; txtIngB = "Sea Salt"; txtIngC = ""; }
				else if ( page == 7 ){ spellID = GraspingRootsSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "30"; txtIngA = "Mandrake Root"; txtIngB = "Ginseng"; txtIngC = ""; }
				else if ( page == 8 ){ spellID = DruidicRuneSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "35"; txtIngA = "Black Pearl"; txtIngB = "Eye of Toad"; txtIngC = ""; }
				else if ( page == 9 ){ spellID = HerbalHealingSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "40"; txtIngA = "Red Lotus"; txtIngB = "Garlic"; txtIngC = ""; }
				else if ( page == 10 ){ spellID = BlendWithForestSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "45"; txtIngA = "Silver Widow"; txtIngB = "Nightshade"; txtIngC = ""; }
				else if ( page == 11 ){ spellID = FireflySpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "50"; txtIngA = "Spider Silk"; txtIngB = "Butterfly Wings"; txtIngC = ""; }
				else if ( page == 12 ){ spellID = MushroomGatewaySpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "55"; txtIngA = "Bloodmoss"; txtIngB = "Eye of Toad"; txtIngC = ""; }
				else if ( page == 13 ){ spellID = SwarmOfInsectsSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "60"; txtIngA = "Butterfly Wings"; txtIngB = "Beetle Shell"; txtIngC = ""; }
				else if ( page == 14 ){ spellID = ProtectiveFairySpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "65"; txtIngA = "Fairy Egg"; txtIngB = "Moon Crystal"; txtIngC = ""; }
				else if ( page == 15 ){ spellID = TreefellowSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "70"; txtIngA = "Swamp Berries"; txtIngB = "Mandrake Root"; txtIngC = ""; }
				else if ( page == 16 ){ spellID = VolcanicEruptionSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "75"; txtIngA = "Brimstone"; txtIngB = "Sulfurous Ash"; txtIngC = ""; }
				else if ( page == 17 ){ spellID = RestorativeSoilSpell.SpellInfo.SpellDefinition.SpellID; txtSklB = "80"; txtIngA = "Nightshade"; txtIngB = "Red Lotus"; txtIngC = ""; }

				SpellDefinition definition = DruidSpellProvider.GetDefinition( spellID );
				if ( definition == null )
					return "";

				if ( val == 1 )
					return definition.Name.String;
				else if ( val == 2 )
					return definition.IconGraphic.ToString();
				else if ( val == 3 )
					return definition.Description.String;
				else if ( val == 4 )
					return definition.MinSkill.ToString();
				else if ( val == 5 )
					return txtSklB;
				else if ( val == 6 )
					return txtIngA;
				else if ( val == 7 )
					return txtIngB;

				return txtIngC;
			}
		}

		public override void OnDoubleClick( Mobile e )
		{
			if ( !IsChildOf( e.Backpack ) ) 
			{
				e.SendMessage( "This must be in your backpack to read." );
			}
			else
			{
				e.CloseGump( typeof( BookGump ) );
				e.SendGump( new BookGump( e, 1 ) );
			}
		}

		public BookDruidBrewing(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();
		}
	}
}