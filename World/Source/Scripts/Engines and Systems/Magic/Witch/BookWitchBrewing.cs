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
using Server.Spells.Undead;

namespace Server.Items
{
	public class BookWitchBrewing : Item
	{
		[Constructable]
		public BookWitchBrewing( ) : base( 0x5689 )
		{
			Weight = 1.0;
			Name = "The Witch's Brew";
			Hue = 0x9A2;
		}

		public class BookGump : Gump
		{
			public BookGump( Mobile from, int page ): base( 100, 100 )
			{
				string color = "#d89191";
				from.SendSound( 0x55 );

				this.Closable=true;
				this.Disposable=true;
				this.Dragable=true;
				this.Resizable=false;

				AddPage(0);

				AddImage(0, 0, 7005, 2845);
				AddImage(0, 0, 7006);
				AddImage(0, 0, 7024, 2736);
				AddImage(87, 117, 7053);
				AddImage(382, 117, 7053);

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
					AddHtml( 107, 46, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>THE WITCH'S BREW</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 398, 48, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>THE WITCH'S BREW</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 78, 75, 248, 318, @"<BODY><BASEFONT Color=" + color + ">Witchery brewing is the art of taking morbid reagents and creating concoctions that necromancers can use in their dark magics. You would use your forensics skill to create the potions, and your necromancy skill to use them. This book explains the various mixtures you can make, as well as additional information to manage these potions effectively. Unlike other potions, these require jars as the liquid needs a thicker glass to store as it is acidic enough to dissolve bottle glass and even the wood of a keg.</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 372, 75, 248, 318, @"<BODY><BASEFONT Color=" + color + ">You will need a small cauldron to brew these concoctions. You can also get a belt pouch to store the ingredients, cauldrons, jars, potions, and this book to make them easier to carry. Single click this bag to organize it for easier use of the mixtures.</BASEFONT></BODY>", (bool)false, (bool)false);
				}
				else
				{
					AddHtml( 107, 46, 186, 20, @"<BODY><BASEFONT Color=" + color + "><CENTER>" + potionInfo( potion, 1 ) + "</CENTER></BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 73, 72, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Forensics:</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 267, 72, 47, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 4 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 73, 98, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Necromancy:</BASEFONT></BODY>", (bool)false, (bool)false);
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

					AddHtml( 366, 72, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Forensics:</BASEFONT></BODY>", (bool)false, (bool)false);
					AddHtml( 560, 72, 47, 20, @"<BODY><BASEFONT Color=" + color + ">" + potionInfo( potion, 4 ) + "</BASEFONT></BODY>", (bool)false, (bool)false);

					AddHtml( 366, 98, 187, 20, @"<BODY><BASEFONT Color=" + color + ">Necromancy:</BASEFONT></BODY>", (bool)false, (bool)false);
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

				txt = "This is created from a witch's brew: " + potionInfo( potion, 3 ) + " To use it, one should have a " + potionInfo( potion, 4 ) + " in Forensics and a " + potionInfo( potion, 5 ) + " in Necromancy.";

				return txt;
			}

			public static string potionInfo( int page, int val )
			{
				int spellID = 0;
				string txtSklA = "";
				string txtIngA = "";
				string txtIngB = "";
				string txtIngC = "";

				if ( page == 2 ){ spellID = UndeadEyesSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "10"; txtIngA = "Mummy Wrap"; txtIngB = "Eye of Toad"; txtIngC = ""; }
				else if ( page == 3 ){ spellID = NecroUnlockSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "15"; txtIngA = "Maggot"; txtIngB = "Beetle Shell"; txtIngC = ""; }
				else if ( page == 4 ){ spellID = NecroPoisonSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "20"; txtIngA = "Violet Fungus"; txtIngB = "Nox Crystal"; txtIngC = ""; }
				else if ( page == 5 ){ spellID = PhantasmSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "25"; txtIngA = "Dried Toad"; txtIngB = "Gargoyle Ear"; txtIngC = ""; }
				else if ( page == 6 ){ spellID = RetchedAirSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "30"; txtIngA = "Black Sand"; txtIngB = "Grave Dust"; txtIngC = ""; }
				else if ( page == 7 ){ spellID = ManaLeechSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "35"; txtIngA = "Dried Toad"; txtIngB = "Red Lotus"; txtIngC = ""; }
				else if ( page == 8 ){ spellID = UndeadWallOfSpikesSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "40"; txtIngA = "Bitter Root"; txtIngB = "Pig Iron"; txtIngC = ""; }
				else if ( page == 9 ){ spellID = UndeadCurePoisonSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "45"; txtIngA = "Wolfsbane"; txtIngB = "Swamp Berries"; txtIngC = ""; }
				else if ( page == 10 ){ spellID = UnBloodlessSpecPactSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "50"; txtIngA = "Blood Rose"; txtIngB = "Daemon Blood"; txtIngC = ""; }
				else if ( page == 11 ){ spellID = SpectreShadowSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "55"; txtIngA = "Violet Fungus"; txtIngB = "Silver Widow"; txtIngC = ""; }
				else if ( page == 12 ){ spellID = GhostPhaseSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "60"; txtIngA = "Bitter Root"; txtIngB = "Moon Crystal"; txtIngC = ""; }
				else if ( page == 13 ){ spellID = HellsGateSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "65"; txtIngA = "Maggot"; txtIngB = "Black Pearl"; txtIngC = ""; }
				else if ( page == 14 ){ spellID = GhostlyImagesSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "70"; txtIngA = "Mummy Wrap"; txtIngB = "Bloodmoss"; txtIngC = ""; }
				else if ( page == 15 ){ spellID = HellsBrandSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "75"; txtIngA = "Werewolf Claw"; txtIngB = "Brimstone"; txtIngC = ""; }
				else if ( page == 16 ){ spellID = UndeadGraveyardGatewaySpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "80"; txtIngA = "Black Sand"; txtIngB = "Wolfsbane"; txtIngC = "Pixie Skull"; }
				else if ( page == 17 ){ spellID = VampireGiftSpell.SpellInfo.SpellDefinition.SpellID; txtSklA = "85"; txtIngA = "Werewolf Claw"; txtIngB = "Bat Wing"; txtIngC = "Blood Rose"; }

				SpellDefinition definition = WitchSpellProvider.GetDefinition( spellID );
				if ( definition == null )
					return "";

				if ( val == 1 )
					return definition.Name.String;
				else if ( val == 2 )
					return definition.IconGraphic.ToString();
				else if ( val == 3 )
					return definition.Description.String;
				else if ( val == 4 )
					return txtSklA;
				else if ( val == 5 )
					return definition.MinSkill.ToString();
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

		public BookWitchBrewing(Serial serial) : base(serial)
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