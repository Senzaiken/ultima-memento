using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Dynamic
{
	/// <summary>
	/// Describes how one server spellbook type is shown as a native spellbook on clients that
	/// support server-driven spellbooks (TazUO). The spells themselves come from the school's
	/// spell provider; this class only adds presentation (art, layout, labels, pages).
	/// </summary>
	public abstract class DynamicSpellbook
	{
		// Book type the server uses for this school (Spellbook.SpellbookType).
		public abstract SpellbookType SpellbookType { get; }

		// Book type id the client uses for this school. Must be 20 or higher; lower ids are stock client books.
		public abstract byte ClientBookType { get; }

		// Spell id the client uses for the first spell of this book. Clients route ids of 1999 and up to
		// server-defined books, so every book gets its own range starting at or above 2000. These ids are
		// what clients save in macros and spell icons, so they must never change once released.
		public abstract int ClientSpellBase { get; }

		// The school's spells in book order (ascending spell id), normally a SpellProvider.SpellDefinitions list.
		public abstract IList<SpellDefinition> SpellDefinitions { get; }

		// Casts a spell chosen in the native book (or a macro/icon made from it), with the school's own rules.
		// Normally the school's SpellProvider.Cast, so native and legacy casting behave the same.
		public abstract void Cast( Mobile from, int spellId );

		public abstract ushort BookGraphic { get; }
		public abstract ushort MinimizedGraphic { get; }

		public virtual byte SpellsPerPageSide { get { return 8; } }
		public virtual byte MaxDictionaryPages { get { return 2; } }

		public virtual bool DisplayManaCost { get { return true; } }
		public virtual bool DisplayMinSkill { get { return true; } }
		public virtual bool DisplayPowerWords { get { return false; } }
		public virtual string ManaCostLabel { get { return null; } }
		public virtual string MinSkillLabel { get { return null; } }

		public virtual string CustomPropertyTitle { get { return null; } }
		public virtual string CustomPropertyLabel { get { return null; } }
		public virtual string CustomPropertyName { get { return null; } }

		// Colors and offsets; 0 means "use the client default".
		public virtual ushort BookHue { get { return 0; } }
		public virtual ushort TextColor { get { return 0; } }
		public virtual ushort SpellNameColor { get { return 0; } }
		public virtual ushort TitleColor { get { return 0; } }
		public virtual short ContentOffsetX { get { return 0; } }
		public virtual short ContentOffsetY { get { return 0; } }
		public virtual ushort PageTurnLeftGraphic { get { return 0; } }
		public virtual ushort PageTurnRightGraphic { get { return 0; } }
		public virtual short PageTurnLeftX { get { return 0; } }
		public virtual short PageTurnLeftY { get { return 0; } }
		public virtual short PageTurnRightX { get { return 0; } }
		public virtual short PageTurnRightY { get { return 0; } }
		public virtual ushort[] OverlayGraphics { get { return null; } }

		// Section titles; a spell's page (GetPage) is a 1-based index into this list, 0 = no section.
		public virtual string[] PageNames { get { return null; } }
		public virtual List<DynamicInfoPage> InfoPages { get { return null; } }
		public virtual DynamicSpellbookBookmark Bookmark { get { return null; } }

		public virtual byte GetPage( SpellDefinition definition )
		{
			return 0;
		}

		// Order of a spell within its page (lower first); spells with equal keys keep book order.
		public virtual int GetSortKey( SpellDefinition definition )
		{
			return 0;
		}

		public virtual ushort GetIcon( SpellDefinition definition )
		{
			return definition.IconGraphic;
		}

		// Reagent names listed under the spell; null = none.
		public virtual string[] GetReagents( SpellDefinition definition )
		{
			return null;
		}

		public virtual string GetDescription( SpellDefinition definition )
		{
			return definition.Description != null ? definition.Description.String : null;
		}

		// Bit i set = the book holds SpellDefinitions[i].
		public virtual ulong GetSpellBitmask( Mobile from, Spellbook book )
		{
			return book.Content;
		}

		// Return false to fall back to the server gump for this book (e.g. a state the native book can't show).
		public virtual bool CanDisplay( Mobile from, Spellbook book )
		{
			return true;
		}

		// Called when the client presses a ServerCallback bookmark on this book.
		public virtual void OnBookmarkPressed( Mobile from, Spellbook book, uint token )
		{
		}
	}

	public class DynamicInfoPage
	{
		public string Title { get; set; }
		public string Body { get; set; }

		public DynamicInfoPage( string title, string body )
		{
			Title = title;
			Body = body;
		}
	}

	public static class DynamicBookmarkAction
	{
		public const byte None = 0;
		public const byte JumpToPage = 1;		// Action = 1-based page inside the book
		public const byte ServerCallback = 2;	// Action = opaque token sent back with 0xBF/0x3E
	}

	public class DynamicSpellbookBookmark
	{
		public ushort Graphic { get; set; }
		public ushort PressedGraphic { get; set; }	// 0 = reuse Graphic
		public short X { get; set; }
		public short Y { get; set; }
		public ushort Hue { get; set; }
		public byte Page { get; set; }			// 0 = every page
		public byte ActionType { get; set; }		// DynamicBookmarkAction
		public uint Action { get; set; }
		public string Tooltip { get; set; }
	}

	// Wire format of one spell as the client receives it.
	public struct DynamicSpellDefinition
	{
		public ushort SpellID;
		public ushort IconGraphic;
		public int NameCliloc;
		public string Name;
		public string PowerWords;
		public string Description;
		public byte ManaCost;
		public byte MinSkill;
		public byte TargetType;		// 0 = neutral, 1 = harmful, 2 = beneficial
		public ushort Reagents;
		public string[] CustomReagents;
		public ushort Cooldown;
		public byte Page;
	}
}
