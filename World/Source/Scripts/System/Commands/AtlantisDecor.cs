using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Server.Items;

namespace Server.Commands
{
    // Fixed additions on the logical Atlantis facet. Nothing writes MUL files,
    // regenerates world decoration, or runs automatically when the server starts.
    public static class AtlantisDecor
    {
        internal const string ManifestFile = "Data/Atlantis/Decorations.json";
        internal const int FloorZ = -127;
        internal const int CeilingZ = -87;
        internal static readonly string[] Districts = { "north", "central", "east", "south" };

        public static void Initialize()
        {
            CommandSystem.Register("AtlantisDecor", AccessLevel.Administrator, OnCommand);
            CommandSystem.Register("AtlantisSave", AccessLevel.Administrator, AtlantisSave.OnCommand);
        }

        [Usage("AtlantisDecor <preview|check|clear|status|go> [north|central|east|south|all]")]
        [Description("Previews or removes the underwater-city decoration additions in Atlantis.")]
        public static void OnCommand(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                e.Mobile.SendGump(new Server.Gumps.AtlantisPaletteGump(e.Mobile, 0, ""));
                return;
            }
            string action = e.GetString(0).ToLowerInvariant();
            string district = e.Length > 1 ? e.GetString(1).ToLowerInvariant() : "all";

            if (e.Length > 2 || !IsDistrict(district) || action == "help")
            {
                Help(e.Mobile);
                return;
            }

            try
            {
                if (action == "clear")
                {
                    int removed = ClearPreview(district);
                    e.Mobile.SendMessage("Removed {0} Atlantis preview items ({1}).", removed, district);
                    return;
                }

                if (action == "status")
                {
                    foreach (string name in Districts)
                        if (district == "all" || district == name)
                            e.Mobile.SendMessage("Atlantis {0}: {1} preview items in the world.", name, CountPreview(name));
                    return;
                }

                if (action != "preview" && action != "check" && action != "go")
                {
                    Help(e.Mobile);
                    return;
                }

                string path = Path.Combine(Core.BaseDirectory, ManifestFile);
                List<DecorationGroup> groups = ReadManifest(path);
                Map map = Map.Atlantis;
                if (map == null || !map.Tiles.Exists)
                    throw new InvalidOperationException("Atlantis's map files are not available.");

                if (action == "go")
                {
                    Go(e.Mobile, map, groups, district == "all" ? "north" : district);
                    return;
                }

                PreviewResult result = ApplyPreview(map, groups, district, action == "check");
                e.Mobile.SendMessage("Atlantis {0}: {1} items in {2} groups {3}; {4} groups already present; {5} groups blocked.",
                    district, result.Items, result.Groups, action == "check" ? "available" : "placed",
                    result.Existing, result.Blocked);
                foreach (string detail in result.Details)
                    e.Mobile.SendMessage(detail);
                if (action == "preview" && result.Items > 0)
                    e.Mobile.SendMessage("Use [AtlantisDecor go {0} to visit, or [AtlantisDecor clear {1} to remove these additions.",
                        district == "all" ? "north" : district, district);
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Atlantis decoration: {0}", ex.Message);
                Console.WriteLine("AtlantisDecor: {0}", ex);
            }
        }

        private static void Help(Mobile from)
        {
            from.SendMessage("[AtlantisDecor - open the palette of every art/hue pair used in Atlantis and place one at a target.");
            from.SendMessage("[AtlantisDecor preview [district] - place the additions; check - count available placements.");
            from.SendMessage("[AtlantisDecor go [district] - visit; status - count preview items; clear [district] - remove preview items.");
            from.SendMessage("Districts: north, central, east, south, all. Omitted district means all; go defaults to north.");
        }

        internal static bool IsDistrict(string district)
        {
            return district == "all" || Array.IndexOf(Districts, district) >= 0;
        }

        // These deliberately exclude the furnished western and southern rooms.
        internal static bool InDistrict(string district, int x, int y)
        {
            switch (district)
            {
                case "north": return x >= 4318 && x < 4472 && y >= 2519 && y < 2584;
                case "central": return x >= 4320 && x < 4380 && y >= 2584 && y < 2636;
                case "east": return x >= 4380 && x < 4472 && y >= 2584 && y < 2636;
                case "south": return x >= 4380 && x < 4472 && y >= 2636 && y < 2684;
                default: return false;
            }
        }

        internal sealed class Placement
        {
            internal int Art, Hue, X, Y, Z;
            internal string Mode;
        }

        internal sealed class DecorationGroup
        {
            internal string District, Key;
            internal readonly List<Placement> Items = new List<Placement>();
            internal int MinX = Int32.MaxValue, MinY = Int32.MaxValue;
            internal int MaxX = Int32.MinValue, MaxY = Int32.MinValue;
            internal Rectangle2D Clearance
            {
                get { return new Rectangle2D(MinX - 1, MinY - 1, MaxX - MinX + 3, MaxY - MinY + 3); }
            }
        }

        internal sealed class PreviewResult
        {
            internal int Items, Groups, Existing, Blocked;
            internal readonly List<string> Details = new List<string>();
        }

        internal static int Number(string value)
        {
            return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Int32.Parse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : Int32.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        // Placement rows as the eight manifest fields: district, group, art, hue, x, y, z, mode.
        internal static List<string[]> ReadRows(string path)
        {
            List<string[]> rows = new List<string[]>();
            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, object> root = AtlantisJson.Parse(File.ReadAllText(path)) as Dictionary<string, object>;
                object placements;
                if (root == null || !root.TryGetValue("placements", out placements) || !(placements is List<object>))
                    throw new InvalidDataException("The Atlantis manifest needs a \"placements\" array.");
                string[] keys = { "district", "group", "art", "hue", "x", "y", "z", "mode" };
                foreach (object entry in (List<object>)placements)
                {
                    Dictionary<string, object> obj = entry as Dictionary<string, object>;
                    if (obj == null) throw new InvalidDataException("Each placement must be an object.");
                    string[] row = new string[8];
                    for (int k = 0; k < 8; ++k)
                    {
                        object value;
                        row[k] = obj.TryGetValue(keys[k], out value) ? AtlantisJson.Text(value) : "";
                    }
                    rows.Add(row);
                }
                return rows;
            }
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                rows.Add(line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            }
            return rows;
        }

        internal static bool SavedInGame(string path)
        {
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return false;
            Dictionary<string, object> root = AtlantisJson.Parse(File.ReadAllText(path)) as Dictionary<string, object>;
            object flag;
            return root != null && root.TryGetValue("savedInGame", out flag) && flag is bool && (bool)flag;
        }

        internal static void WriteRows(string path, List<string[]> rows, bool savedInGame, string lastSave)
        {
            List<string> lines = new List<string>();
            lines.Add("{");
            lines.Add(" \"map\": \"Atlantis (logical map 6, file index 1), lower floor Z -127\",");
            lines.Add(" \"format\": 2,");
            lines.Add(" \"savedInGame\": " + (savedInGame ? "true" : "false") + ",");
            lines.Add(" \"lastSave\": " + AtlantisJson.Quote(lastSave == null ? "" : lastSave) + ",");
            lines.Add(" \"placements\": [");
            for (int i = 0; i < rows.Count; ++i)
            {
                string[] r = rows[i];
                lines.Add("  {\"district\":" + AtlantisJson.Quote(r[0]) + ",\"group\":" + AtlantisJson.Quote(r[1]) + ",\"art\":\"0x" + Number(r[2]).ToString("X4") + "\",\"hue\":" + Number(r[3]) +
                    ",\"x\":" + Number(r[4]) + ",\"y\":" + Number(r[5]) + ",\"z\":" + Number(r[6]) + ",\"mode\":" + AtlantisJson.Quote(r[7]) + "}" + (i + 1 < rows.Count ? "," : ""));
            }
            lines.Add(" ]");
            lines.Add("}");
            File.WriteAllLines(path, lines.ToArray());
        }

        internal static List<DecorationGroup> ReadManifest(string path)
        {
            List<DecorationGroup> groups = new List<DecorationGroup>();
            Dictionary<string, DecorationGroup> byKey = new Dictionary<string, DecorationGroup>(StringComparer.Ordinal);
            HashSet<string> positions = new HashSet<string>();
            int lineNumber = 0, count = 0;
            foreach (string[] fields in ReadRows(path))
            {
                ++lineNumber;
                try
                {
                    if (fields.Length != 8 || !IsDistrict(fields[0]) || fields[0] == "all" ||
                        !fields[1].StartsWith(fields[0] + "_v2_", StringComparison.Ordinal) || fields[1].Length > 100)
                        throw new FormatException("Expected district, v2 group, art, hue, x, y, z, placement mode.");
                    Placement p = new Placement();
                    p.Art = Number(fields[2]); p.Hue = Number(fields[3]);
                    p.X = Number(fields[4]); p.Y = Number(fields[5]); p.Z = Number(fields[6]);
                    p.Mode = fields[7];
                    if (p.Mode != "floor" && p.Mode != "overlay" && p.Mode != "surface" && p.Mode != "north" && p.Mode != "west")
                        throw new FormatException("Unknown placement mode.");
                    if (!InDistrict(fields[0], p.X, p.Y) || p.Z < FloorZ || p.Z >= CeilingZ ||
                        p.Art <= 0 || p.Art >= TileData.ItemTable.Length || p.Hue < 0 || p.Hue > 0x3FFF ||
                        p.Z + TileData.ItemTable[p.Art].Height > CeilingZ ||
                        ((p.Mode == "floor" || p.Mode == "overlay") && p.Z != FloorZ) ||
                        (p.Mode == "overlay" && (TileData.ItemTable[p.Art].Height > 1 || TileData.ItemTable[p.Art].Impassable)) ||
                        (p.Mode == "surface" && p.Z == FloorZ))
                        throw new FormatException("Placement is outside the preview districts, floor, or supported artwork range.");
                    if (!positions.Add(p.Art + ":" + p.X + ":" + p.Y + ":" + p.Z))
                        throw new FormatException("Duplicate artwork at the same position.");
                    if (++count > 8000)
                        throw new FormatException("The preview is limited to 8000 items.");
                    DecorationGroup group;
                    if (!byKey.TryGetValue(fields[1], out group))
                    {
                        group = new DecorationGroup();
                        group.District = fields[0]; group.Key = fields[1];
                        byKey.Add(group.Key, group); groups.Add(group);
                    }
                    group.Items.Add(p);
                    group.MinX = Math.Min(group.MinX, p.X); group.MaxX = Math.Max(group.MaxX, p.X);
                    group.MinY = Math.Min(group.MinY, p.Y); group.MaxY = Math.Max(group.MaxY, p.Y);
                    if (group.Items.Count > 300 || group.MaxX - group.MinX > 32 || group.MaxY - group.MinY > 32)
                        throw new FormatException("A decoration group is too large.");
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("Invalid Atlantis manifest at placement " + lineNumber + ": " + ex.Message, ex);
                }
            }
            if (groups.Count == 0)
                throw new InvalidDataException("The Atlantis decoration manifest is empty.");
            foreach (DecorationGroup group in groups)
                foreach (Placement p in group.Items)
                {
                    if (p.Mode == "surface" && !HasPlannedSupport(group, p))
                        throw new InvalidDataException(group.Key + ": a raised item has no supporting furnishing.");
                    foreach (Placement other in group.Items)
                        if (p != other && p.X == other.X && p.Y == other.Y &&
                            p.Mode == "floor" && other.Mode == "floor")
                            throw new InvalidDataException(group.Key + ": overlapping floor furnishings.");
                }
            return groups;
        }

        private static bool HasPlannedSupport(DecorationGroup group, Placement p)
        {
            foreach (Placement support in group.Items)
                if (support.X == p.X && support.Y == p.Y && support.Z < p.Z &&
                    (support.Mode == "floor" || support.Mode == "north" || support.Mode == "west") &&
                    support.Z + TileData.ItemTable[support.Art].Height == p.Z)
                    return true;
            return false;
        }

        private static bool Floor(StaticTile tile)
        {
            return tile.ID >= 0x519 && tile.ID <= 0x524 && tile.Z == FloorZ;
        }

        private static bool Architecture(StaticTile tile)
        {
            return Floor(tile) || (tile.ID >= 0xC7 && tile.ID <= 0xE6);
        }

        internal static bool CanPlaceGroup(Map map, DecorationGroup group, out string reason)
        {
            reason = null;
            // Examine the whole furnished room plus a one-tile margin. This catches pools,
            // low debris, wall ornaments, and raised furnishings, even if CanFit
            // would permit placing an item on or beside them.
            for (int x = group.MinX - 1; x <= group.MaxX + 1; ++x)
                for (int y = group.MinY - 1; y <= group.MaxY + 1; ++y)
                    foreach (StaticTile tile in map.Tiles.GetStaticTiles(x, y, true))
                        if (tile.Z >= FloorZ - 1 && tile.Z < CeilingZ && !Architecture(tile))
                        {
                            reason = "existing static decoration";
                            return false;
                        }

            // Do not weave a partial group around an existing dynamic decoration,
            // teleporter, door, player item, or mobile. Skip the whole arrangement.
            IPooledEnumerable items = map.GetItemsInBounds(group.Clearance);
            try
            {
                foreach (Item item in items)
                    if (!item.Deleted && item.Z < CeilingZ && item.Z + Math.Max(1, item.ItemData.Height) > FloorZ - 1)
                    {
                        reason = "existing world item";
                        return false;
                    }
            }
            finally { items.Free(); }
            IPooledEnumerable mobiles = map.GetMobilesInBounds(group.Clearance);
            try
            {
                foreach (Mobile mobile in mobiles)
                    if (!mobile.Deleted && mobile.Z < CeilingZ && mobile.Z + 16 > FloorZ)
                    {
                        reason = "mobile in the placement area";
                        return false;
                    }
            }
            finally { mobiles.Free(); }

            foreach (Placement p in group.Items)
            {
                if (p.Mode == "north" || p.Mode == "west")
                {
                    bool wall = false;
                    int wx = p.X - (p.Mode == "west" ? 1 : 0);
                    int wy = p.Y - (p.Mode == "north" ? 1 : 0);
                    foreach (StaticTile tile in map.Tiles.GetStaticTiles(wx, wy, true))
                        if (tile.ID >= 0xC7 && tile.ID <= 0xE6 && tile.Z < CeilingZ &&
                            tile.Z + TileData.ItemTable[tile.ID].Height > p.Z)
                            wall = true;
                    if (!wall) { reason = "missing backing wall"; return false; }
                }
                if (p.Mode == "surface" && !HasPlannedSupport(group, p))
                { reason = "missing supporting furnishing"; return false; }
                bool supported = false;
                foreach (StaticTile tile in map.Tiles.GetStaticTiles(p.X, p.Y, true))
                {
                    if (Floor(tile)) supported = true;
                    else if (tile.Z >= FloorZ - 1 && tile.Z < CeilingZ)
                    {
                        reason = "occupied floor tile";
                        return false;
                    }
                }
                // Raised details are supported by the planned table/pedestal, which
                // is created only after the entire room passes validation.
                if (!supported || !map.CanFit(p.X, p.Y, p.Z, Math.Max(1, TileData.ItemTable[p.Art].Height), true, true, p.Z == FloorZ))
                {
                    reason = "missing floor or blocked placement";
                    return false;
                }
            }
            return true;
        }

        internal static PreviewResult ApplyPreview(Map map, List<DecorationGroup> groups, string district, bool dryRun)
        {
            if (map == null || map != Map.Atlantis || !IsDistrict(district))
                throw new InvalidOperationException("The preview can only be placed on Map.Atlantis in a listed district.");
            PreviewResult result = new PreviewResult();
            HashSet<string> existing = new HashSet<string>(StringComparer.Ordinal);
            foreach (Item item in World.Items.Values)
            {
                AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                if (preview != null && !preview.Deleted)
                {
                    if ((district == "all" || preview.PreviewDistrict == district) &&
                        (preview.PreviewGroup == null || !preview.PreviewGroup.StartsWith(preview.PreviewDistrict + "_v2_", StringComparison.Ordinal)))
                        throw new InvalidOperationException("The old preview is still present. Use [AtlantisDecor clear " + district + " before placing these furnished rooms.");
                    existing.Add(preview.PreviewGroup);
                }
            }
            List<AtlantisPreviewItem> created = new List<AtlantisPreviewItem>();
            try
            {
                foreach (DecorationGroup group in groups)
                {
                    if (district != "all" && district != group.District) continue;
                    // A moved or partially removed group remains owned and is not
                    // duplicated. Clear its district before rebuilding it.
                    if (existing.Contains(group.Key)) { ++result.Existing; continue; }
                    string reason;
                    if (!CanPlaceGroup(map, group, out reason))
                    {
                        ++result.Blocked;
                        if (result.Details.Count < 4)
                            result.Details.Add(group.Key + ": " + reason + ".");
                        continue;
                    }
                    if (!dryRun)
                        foreach (Placement p in group.Items)
                        {
                            AtlantisPreviewItem item = new AtlantisPreviewItem(p.Art, p.Hue, group.District, group.Key);
                            created.Add(item);
                            item.MoveToWorld(new Point3D(p.X, p.Y, p.Z), map);
                        }
                    result.Items += group.Items.Count;
                    ++result.Groups;
                }
                return result;
            }
            catch
            {
                // Roll back this invocation only. Prior previews and existing
                // decorations are never part of this list.
                foreach (AtlantisPreviewItem item in created)
                    if (!item.Deleted) item.Delete();
                throw;
            }
        }

        internal static int ClearPreview(string district)
        {
            if (!IsDistrict(district)) throw new ArgumentException("Unknown Atlantis district.");
            List<AtlantisPreviewItem> items = new List<AtlantisPreviewItem>();
            foreach (Item item in World.Items.Values)
            {
                AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                if (preview != null && !preview.Deleted && (district == "all" || preview.PreviewDistrict == district))
                    items.Add(preview);
            }
            foreach (AtlantisPreviewItem item in items) item.Delete();
            return items.Count;
        }

        private static int CountPreview(string district)
        {
            int count = 0;
            foreach (Item item in World.Items.Values)
            {
                AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                if (preview != null && !preview.Deleted && preview.PreviewDistrict == district) ++count;
            }
            return count;
        }

        private static void Go(Mobile from, Map map, List<DecorationGroup> groups, string district)
        {
            foreach (DecorationGroup group in groups)
            {
                if (group.District != district) continue;
                int cx = (group.MinX + group.MaxX) / 2, cy = group.MaxY + 3;
                for (int radius = 0; radius <= 8; ++radius)
                    for (int dx = -radius; dx <= radius; ++dx)
                        for (int dy = -radius; dy <= radius; ++dy)
                        {
                            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                            int x = cx + dx, y = cy + dy;
                            if (!InDistrict(district, x, y)) continue;
                            bool floor = false;
                            foreach (StaticTile tile in map.Tiles.GetStaticTiles(x, y, true))
                                if (Floor(tile)) floor = true;
                            if (floor && map.CanFit(x, y, FloorZ, 16, true, true, true))
                            {
                                from.MoveToWorld(new Point3D(x, y, FloorZ), map);
                                from.SendMessage("Atlantis {0}: {1}, {2}, {3}.", district, x, y, FloorZ);
                                return;
                            }
                        }
            }
            from.SendMessage("No clear viewing location was found in that district.");
        }
    }

    // Snapshots the decoration inside a targeted rectangle back into the manifest, so
    // rooms can be adjusted in game and the manifest kept as the record to freeze from.
    public static class AtlantisSave
    {
        internal sealed class SaveResult
        {
            internal int Items, Groups, Converted, Rekeyed, DroppedGroups;
            internal Rectangle2D Area;
            internal readonly List<string> Notes = new List<string>();
        }

        internal sealed class Piece
        {
            internal int Art, Hue, X, Y, Z;
            internal string Mode, District;
            internal Item Source;
        }

        [Usage("AtlantisSave [name | unfreeze [district] | freeze [district]]")]
        [Description("Target two corners in Atlantis and save the decoration between them into Decorations.json; unfreeze/freeze toggle whether the placed items can be moved.")]
        public static void OnCommand(CommandEventArgs e)
        {
            string first = e.Length > 0 ? e.GetString(0).ToLowerInvariant() : null;
            if (first == "unfreeze" || first == "freeze")
            {
                string district = e.Length > 1 ? e.GetString(1).ToLowerInvariant() : "all";
                if (!AtlantisDecor.IsDistrict(district)) { e.Mobile.SendMessage("Districts: north, central, east, south, all."); return; }
                int changed = SetMovable(district, first == "unfreeze");
                e.Mobile.SendMessage("{0} Atlantis decoration items are now {1} ({2}).", changed, first == "unfreeze" ? "movable; drag them, then [AtlantisSave to record them" : "fixed in place", district);
                return;
            }
            string name = first;
            if (e.Length > 1 || (name != null && !IsName(name)))
            {
                e.Mobile.SendMessage("[AtlantisSave - target two corners and save what is between them. Optional name uses letters, digits and underscores.");
                e.Mobile.SendMessage("[AtlantisSave unfreeze [district] - make the placed items movable; [AtlantisSave freeze [district] - fix them again.");
                return;
            }
            if (e.Mobile.Map != Map.Atlantis)
            {
                e.Mobile.SendMessage("Stand in Atlantis before saving its decoration.");
                return;
            }
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(Picked), name);
        }

        private static bool IsName(string name)
        {
            if (name.Length == 0 || name.Length > 40) return false;
            foreach (char c in name)
                if (!(Char.IsLetterOrDigit(c) || c == '_')) return false;
            return true;
        }

        private static void Picked(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            try
            {
                Rectangle2D rect = new Rectangle2D(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
                    Math.Abs(end.X - start.X) + 1, Math.Abs(end.Y - start.Y) + 1);
                string author = from.Account == null ? from.Name : from.Account.ToString();
                SaveResult result = Save(map, rect, (string)state, Path.Combine(Core.BaseDirectory, AtlantisDecor.ManifestFile), author, true);
                from.SendMessage("Atlantis save: {0} items in {1} group(s) written for {2},{3} to {4},{5}; {6} earlier group(s) replaced; {7} items converted to preview items; {8} re-keyed.",
                    result.Items, result.Groups, result.Area.Start.X, result.Area.Start.Y, result.Area.End.X - 1, result.Area.End.Y - 1,
                    result.DroppedGroups, result.Converted, result.Rekeyed);
                foreach (string note in result.Notes)
                    from.SendMessage(note);
            }
            catch (Exception ex)
            {
                from.SendMessage("Atlantis save failed; the manifest was not changed: {0}", ex.Message);
                Console.WriteLine("AtlantisSave: {0}", ex);
            }
        }

        // Collects the decoration in the rectangle (plus the rest of any preview group it
        // touches), infers placement modes, validates the whole manifest, and only then
        // writes it and takes ownership of the world items.
        internal static SaveResult Save(Map map, Rectangle2D rect, string name, string path, string author, bool adoptItems)
        {
            if (map != Map.Atlantis) throw new InvalidOperationException("Only Map.Atlantis can be saved.");
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The manifest must be a .json file.");
            SaveResult result = new SaveResult();
            List<Item> sources = new List<Item>();
            HashSet<string> touchedKeys = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<Type, int> skippedTypes = new Dictionary<Type, int>();

            IPooledEnumerable inBounds = map.GetItemsInBounds(rect);
            try
            {
                foreach (Item item in inBounds)
                {
                    if (item.Deleted || item.Parent != null || item.Z < AtlantisDecor.FloorZ || item.Z >= AtlantisDecor.CeilingZ) continue;
                    AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                    if (preview != null)
                    {
                        if (preview.PreviewGroup != null) touchedKeys.Add(preview.PreviewGroup);
                        sources.Add(item);
                    }
                    else if (item.GetType() == typeof(Static))
                        sources.Add(item);
                    else
                    {
                        int n; skippedTypes.TryGetValue(item.GetType(), out n); skippedTypes[item.GetType()] = n + 1;
                    }
                }
            }
            finally { inBounds.Free(); }

            // Keep groups whole: pull in the rest of any preview group the rectangle touches.
            int minX = rect.Start.X, minY = rect.Start.Y, maxX = rect.End.X - 1, maxY = rect.End.Y - 1;
            if (touchedKeys.Count > 0)
                foreach (Item item in World.Items.Values)
                {
                    AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                    if (preview == null || preview.Deleted || preview.Map != map || preview.PreviewGroup == null || !touchedKeys.Contains(preview.PreviewGroup) || sources.Contains(preview)) continue;
                    sources.Add(preview);
                    minX = Math.Min(minX, preview.X); maxX = Math.Max(maxX, preview.X); minY = Math.Min(minY, preview.Y); maxY = Math.Max(maxY, preview.Y);
                }
            result.Area = new Rectangle2D(minX, minY, maxX - minX + 1, maxY - minY + 1);
            if (name == null || name.Length == 0) name = "saved_" + minX + "_" + minY;
            foreach (KeyValuePair<Type, int> kv in skippedTypes)
                result.Notes.Add("Ignored " + kv.Value + " x " + kv.Key.Name + " (only Static and preview items are saved).");
            if (sources.Count == 0) throw new InvalidOperationException("No Static or preview items in that rectangle.");

            // Infer placement modes, lowest items first so raised pieces can find their support.
            List<Piece> pieces = new List<Piece>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> floors = new HashSet<string>(StringComparer.Ordinal);
            sources.Sort(delegate(Item a, Item b) { return a.Z.CompareTo(b.Z); });
            foreach (Item item in sources)
            {
                ItemData data = TileData.ItemTable[item.ItemID & TileData.MaxItemValue];
                string where = "0x" + item.ItemID.ToString("X4") + " at " + item.X + "," + item.Y + "," + item.Z;
                string district = null;
                foreach (string d in AtlantisDecor.Districts)
                    if (AtlantisDecor.InDistrict(d, item.X, item.Y)) district = d;
                if (district == null) { result.Notes.Add("Skipped " + where + ": outside the preview districts."); continue; }
                if (item.Z + data.Height > AtlantisDecor.CeilingZ) { result.Notes.Add("Skipped " + where + ": reaches the upper level."); continue; }
                if (!seen.Add(item.ItemID + ":" + item.X + ":" + item.Y + ":" + item.Z)) { result.Notes.Add("Skipped a duplicate " + where + "."); continue; }
                Piece p = new Piece();
                p.Art = item.ItemID; p.Hue = item.Hue; p.X = item.X; p.Y = item.Y; p.Z = item.Z; p.District = district; p.Source = item;
                string cell = item.X + ":" + item.Y;
                if (item.Z == AtlantisDecor.FloorZ)
                {
                    bool overlay = data.Height <= 1 && !data.Impassable;
                    if (!overlay && !floors.Add(cell)) { result.Notes.Add("Skipped " + where + ": a second floor furnishing on that tile."); continue; }
                    p.Mode = overlay ? "overlay" : "floor";
                }
                else
                {
                    foreach (Piece support in pieces)
                        if (support.X == p.X && support.Y == p.Y && support.Z < p.Z && support.Mode != "overlay" && support.Mode != "surface" &&
                            support.Z + TileData.ItemTable[support.Art & TileData.MaxItemValue].Height == p.Z)
                        { p.Mode = "surface"; break; }
                    if (p.Mode == null && HasWall(map, p.X, p.Y - 1, p.Z)) p.Mode = "north";
                    if (p.Mode == null && HasWall(map, p.X - 1, p.Y, p.Z)) p.Mode = "west";
                    if (p.Mode == null) { result.Notes.Add("Skipped " + where + ": raised with nothing under it and no wall behind it."); continue; }
                }
                pieces.Add(p);
            }
            if (pieces.Count == 0) throw new InvalidOperationException("Nothing in that rectangle could be saved.");

            // Split into groups that satisfy the manifest limits, one district at a time.
            List<List<Piece>> groups = new List<List<Piece>>();
            foreach (string d in AtlantisDecor.Districts)
            {
                List<Piece> mine = new List<Piece>();
                foreach (Piece p in pieces) if (p.District == d) mine.Add(p);
                if (mine.Count > 0) Split(mine, groups);
            }
            List<string> keys = new List<string>();
            List<string> newLines = new List<string>();
            for (int i = 0; i < groups.Count; ++i)
            {
                List<Piece> g = groups[i];
                string key = g[0].District + "_v2_" + name + (groups.Count > 1 ? "_" + (i + 1) : "");
                keys.Add(key);
                foreach (Piece p in g)
                    newLines.Add(String.Format(CultureInfo.InvariantCulture, "{0} {1} 0x{2:X4} {3} {4} {5} {6} {7}", p.District, key, p.Art, p.Hue, p.X, p.Y, p.Z, p.Mode));
            }

            // Rebuild the manifest: drop every group with a placement inside the saved area or
            // whose world items were absorbed, then append the snapshot.
            HashSet<string> dropKeys = new HashSet<string>(touchedKeys, StringComparer.Ordinal);
            foreach (string key in keys) dropKeys.Add(key);
            List<string[]> original = File.Exists(path) ? AtlantisDecor.ReadRows(path) : new List<string[]>();
            foreach (string[] f in original)
            {
                if (f.Length != 8) continue;
                int x = AtlantisDecor.Number(f[4]), y = AtlantisDecor.Number(f[5]);
                if (x >= minX && x <= maxX && y >= minY && y <= maxY) dropKeys.Add(f[1]);
            }
            List<string[]> output = new List<string[]>();
            foreach (string[] f in original)
                if (f.Length == 8 && !dropKeys.Contains(f[1])) output.Add(f);
            foreach (string key in dropKeys) if (!keys.Contains(key)) ++result.DroppedGroups;
            foreach (string line in newLines) output.Add(line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            string lastSave = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC by " + author + ": " + name + " (" + newLines.Count + " items, " + groups.Count + " group(s))";

            // Validate the complete manifest before touching the real file or the world.
            string temp = path + ".saving.json";
            AtlantisDecor.WriteRows(temp, output, true, lastSave);
            try { AtlantisDecor.ReadManifest(temp); }
            catch { File.Delete(temp); throw; }
            string backupDir = Path.Combine(Path.GetDirectoryName(path), "Backups");
            Directory.CreateDirectory(backupDir);
            if (File.Exists(path))
                File.Copy(path, Path.Combine(backupDir, Path.GetFileNameWithoutExtension(path) + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json"), true);
            File.Copy(temp, path, true);
            File.Delete(temp);

            result.Items = newLines.Count;
            result.Groups = groups.Count;
            if (adoptItems)
                for (int i = 0; i < groups.Count; ++i)
                    foreach (Piece p in groups[i])
                    {
                        AtlantisPreviewItem preview = p.Source as AtlantisPreviewItem;
                        if (preview != null)
                        {
                            preview.Movable = false;
                            if (preview.PreviewGroup != keys[i]) { preview.PreviewGroup = keys[i]; preview.PreviewDistrict = p.District; ++result.Rekeyed; }
                        }
                        else
                        {
                            AtlantisPreviewItem adopted = new AtlantisPreviewItem(p.Art, p.Hue, p.District, keys[i]);
                            adopted.MoveToWorld(new Point3D(p.X, p.Y, p.Z), map);
                            p.Source.Delete();
                            ++result.Converted;
                        }
                    }
            return result;
        }

        internal static int SetMovable(string district, bool movable)
        {
            int changed = 0;
            foreach (Item item in World.Items.Values)
            {
                AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                if (preview == null || preview.Deleted || (district != "all" && preview.PreviewDistrict != district)) continue;
                if (preview.Movable != movable) { preview.Movable = movable; ++changed; }
            }
            return changed;
        }

        private static bool HasWall(Map map, int x, int y, int z)
        {
            foreach (StaticTile tile in map.Tiles.GetStaticTiles(x, y, true))
                if (tile.ID >= 0xC7 && tile.ID <= 0xE6 && tile.Z < AtlantisDecor.CeilingZ && tile.Z + TileData.ItemTable[tile.ID].Height > z)
                    return true;
            return false;
        }

        private static void Split(List<Piece> pieces, List<List<Piece>> groups)
        {
            int minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = Int32.MinValue, maxY = Int32.MinValue;
            foreach (Piece p in pieces)
            { minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y); }
            if (pieces.Count <= 300 && maxX - minX <= 32 && maxY - minY <= 32) { groups.Add(pieces); return; }
            bool byX = maxX - minX >= maxY - minY;
            pieces.Sort(delegate(Piece a, Piece b) { return byX ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y); });
            int cut = byX ? pieces[pieces.Count / 2].X : pieces[pieces.Count / 2].Y;
            List<Piece> low = new List<Piece>(), high = new List<Piece>();
            foreach (Piece p in pieces) ((byX ? p.X : p.Y) < cut ? low : high).Add(p);
            if (low.Count == 0 || high.Count == 0) { groups.Add(pieces); return; }
            Split(low, groups); Split(high, groups);
        }
    }

    // Minimal JSON support for the decoration manifest: objects, arrays, strings,
    // integers and booleans, with trailing commas tolerated so hand edits survive.
    internal static class AtlantisJson
    {
        public static object Parse(string text)
        {
            int i = 0;
            object value = ParseValue(text, ref i);
            Skip(text, ref i);
            if (i != text.Length) throw new FormatException("Unexpected text after the JSON document at " + i + ".");
            return value;
        }

        private static void Skip(string t, ref int i)
        {
            while (i < t.Length && Char.IsWhiteSpace(t[i])) ++i;
        }

        private static object ParseValue(string t, ref int i)
        {
            Skip(t, ref i);
            if (i >= t.Length) throw new FormatException("Unexpected end of JSON.");
            char c = t[i];
            if (c == '{')
            {
                ++i;
                Dictionary<string, object> obj = new Dictionary<string, object>(StringComparer.Ordinal);
                Skip(t, ref i);
                if (i < t.Length && t[i] == '}') { ++i; return obj; }
                while (true)
                {
                    Skip(t, ref i);
                    string key = ParseString(t, ref i);
                    Skip(t, ref i);
                    if (i >= t.Length || t[i] != ':') throw new FormatException("Expected ':' at " + i + ".");
                    ++i;
                    obj[key] = ParseValue(t, ref i);
                    Skip(t, ref i);
                    if (i < t.Length && t[i] == ',') { ++i; Skip(t, ref i); if (i < t.Length && t[i] == '}') { ++i; return obj; } continue; }
                    if (i < t.Length && t[i] == '}') { ++i; return obj; }
                    throw new FormatException("Expected ',' or '}' at " + i + ".");
                }
            }
            if (c == '[')
            {
                ++i;
                List<object> list = new List<object>();
                Skip(t, ref i);
                if (i < t.Length && t[i] == ']') { ++i; return list; }
                while (true)
                {
                    list.Add(ParseValue(t, ref i));
                    Skip(t, ref i);
                    if (i < t.Length && t[i] == ',') { ++i; Skip(t, ref i); if (i < t.Length && t[i] == ']') { ++i; return list; } continue; }
                    if (i < t.Length && t[i] == ']') { ++i; return list; }
                    throw new FormatException("Expected ',' or ']' at " + i + ".");
                }
            }
            if (c == '"') return ParseString(t, ref i);
            if (String.CompareOrdinal(t, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (String.CompareOrdinal(t, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (String.CompareOrdinal(t, i, "null", 0, 4) == 0) { i += 4; return null; }
            int start = i;
            while (i < t.Length && (Char.IsDigit(t[i]) || t[i] == '-' || t[i] == '+' || t[i] == '.' || t[i] == 'e' || t[i] == 'E')) ++i;
            if (start == i) throw new FormatException("Unexpected character '" + c + "' at " + i + ".");
            string number = t.Substring(start, i - start);
            long whole;
            if (Int64.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole)) return whole;
            return Double.Parse(number, CultureInfo.InvariantCulture);
        }

        private static string ParseString(string t, ref int i)
        {
            if (i >= t.Length || t[i] != '"') throw new FormatException("Expected a string at " + i + ".");
            ++i;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (i < t.Length)
            {
                char c = t[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= t.Length) break;
                char e = t[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > t.Length) throw new FormatException("Bad unicode escape.");
                        sb.Append((char)Int32.Parse(t.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; break;
                    default: throw new FormatException("Bad escape '\\" + e + "'.");
                }
            }
            throw new FormatException("Unterminated string.");
        }

        public static string Quote(string s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        public static string Text(object value)
        {
            if (value == null) return "";
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is long) return ((long)value).ToString(CultureInfo.InvariantCulture);
            if (value is double) return ((double)value).ToString(CultureInfo.InvariantCulture);
            return value.ToString();
        }
    }
}

namespace Server.Items
{
    // Static is a normal saved world Item, not a frozen map static. The subclass
    // gives the preview persistent ownership and remains compatible with Freeze.
    public class AtlantisPreviewItem : Static
    {
        private string m_District, m_Group;

        [CommandProperty(AccessLevel.Administrator)]
        public string PreviewDistrict { get { return m_District; } internal set { m_District = value; } }

        [CommandProperty(AccessLevel.Administrator)]
        public string PreviewGroup { get { return m_Group; } internal set { m_Group = value; } }

        public override bool Decays { get { return false; } }

        public AtlantisPreviewItem(int art, int hue, string district, string group) : base(art)
        {
            Hue = hue;
            Movable = false;
            Weight = 1.0;
            m_District = district;
            m_Group = group;
            if (Array.IndexOf(LitArt, art) >= 0) Light = LightType.Circle150;
            else if (art == 0x19AA || art == 0x53F0) Light = LightType.Circle225;
        }

        // Crystal fixtures, salt crystals, braziers, candelabra, lanterns and glowing gems.
        private static readonly int[] LitArt =
        {
            0x61DA, 0x61DD, 0x61E1, 0x6380, 0x6385, 0x6384, 0x6389, 0x6764, 0xE31,
            0x52D, 0x52E, 0xA27, 0xA15, 0x3A92, 0x1430, 0x1853, 0x176C, 0x178A, 0x4881, 0x484A, 0x4883
        };

        public AtlantisPreviewItem(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_District);
            writer.Write(m_Group);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            m_District = reader.ReadString();
            m_Group = reader.ReadString();
            Movable = false;
        }
    }
}

namespace Server.Gumps
{
    using Server.Commands;
    using Server.Items;
    using Server.Network;
    using Server.Targeting;

    // Every art/hue pair used in the Atlantis decoration, as a paged picture palette.
    // Tapping one asks for a target and drops a movable preview item there.
    public class AtlantisPaletteGump : Gump
    {
        private const int Columns = 6, Rows = 5, CellWidth = 100, CellHeight = 108, PerPage = Columns * Rows;
        private readonly int m_Page;
        private readonly string m_Filter;
        private readonly List<int[]> m_Entries;

        internal sealed class Entry { internal int Art, Hue; internal string Name; }

        private static List<int[]> Entries(string filter)
        {
            Dictionary<string, int[]> unique = new Dictionary<string, int[]>(StringComparer.Ordinal);
            string path = Path.Combine(Core.BaseDirectory, AtlantisDecor.ManifestFile);
            if (File.Exists(path))
                foreach (string[] row in AtlantisDecor.ReadRows(path))
                {
                    int art = AtlantisDecor.Number(row[2]), hue = AtlantisDecor.Number(row[3]);
                    unique[art + ":" + hue] = new int[] { art, hue };
                }
            foreach (Item item in World.Items.Values)
            {
                AtlantisPreviewItem preview = item as AtlantisPreviewItem;
                if (preview != null && !preview.Deleted) unique[preview.ItemID + ":" + preview.Hue] = new int[] { preview.ItemID, preview.Hue };
            }
            List<int[]> list = new List<int[]>();
            string needle = filter == null ? "" : filter.Trim().ToLowerInvariant();
            foreach (int[] e in unique.Values)
            {
                if (needle.Length > 0)
                {
                    string name = Name(e[0]).ToLowerInvariant();
                    string hex = "0x" + e[0].ToString("x4");
                    if (name.IndexOf(needle) < 0 && hex.IndexOf(needle) < 0 && e[0].ToString().IndexOf(needle) < 0) continue;
                }
                list.Add(e);
            }
            list.Sort(delegate(int[] a, int[] b)
            {
                int c = String.CompareOrdinal(Name(a[0]), Name(b[0]));
                if (c != 0) return c;
                c = a[0].CompareTo(b[0]);
                return c != 0 ? c : a[1].CompareTo(b[1]);
            });
            return list;
        }

        private static string Name(int art)
        {
            return art >= 0 && art < TileData.ItemTable.Length ? TileData.ItemTable[art].Name : "?";
        }

        public AtlantisPaletteGump(Mobile from, int page, string filter) : base(40, 40)
        {
            m_Filter = filter == null ? "" : filter;
            m_Entries = Entries(m_Filter);
            int pages = Math.Max(1, (m_Entries.Count + PerPage - 1) / PerPage);
            m_Page = Math.Max(0, Math.Min(page, pages - 1));

            Closable = true; Disposable = true; Dragable = true; Resizable = false;
            int width = Columns * CellWidth + 40, height = Rows * CellHeight + 110;
            AddPage(0);
            AddBackground(0, 0, width, height, 9270);
            AddAlphaRegion(10, 10, width - 20, height - 20);
            AddLabel(20, 16, 1153, "Atlantis decoration palette: " + m_Entries.Count + " art/hue pairs, page " + (m_Page + 1) + " of " + pages + ". Tap an item, then target where it goes.");
            AddLabel(20, 42, 1153, "Filter (name or 0x art):");
            AddBackground(190, 38, 220, 26, 3000);
            AddTextEntry(195, 41, 210, 20, 1153, 1, m_Filter);
            AddButton(420, 42, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddLabel(455, 42, 1153, "Apply");
            if (m_Page > 0) { AddButton(width - 180, 42, 4014, 4016, 1, GumpButtonType.Reply, 0); AddLabel(width - 145, 42, 1153, "Prev"); }
            if (m_Page < pages - 1) { AddButton(width - 90, 42, 4005, 4007, 2, GumpButtonType.Reply, 0); AddLabel(width - 55, 42, 1153, "Next"); }

            int first = m_Page * PerPage;
            for (int k = 0; k < PerPage && first + k < m_Entries.Count; ++k)
            {
                int[] e = m_Entries[first + k];
                int x = 20 + (k % Columns) * CellWidth, y = 75 + (k / Columns) * CellHeight;
                AddImageTiled(x, y, CellWidth - 6, CellHeight - 6, 2624);
                AddAlphaRegion(x, y, CellWidth - 6, CellHeight - 6);
                AddItem(x + 22, y + 6, e[0], e[1]);
                AddButton(x + 2, y + CellHeight - 30, 4005, 4007, 100 + first + k, GumpButtonType.Reply, 0);
                AddLabelCropped(x + 34, y + CellHeight - 30, CellWidth - 40, 20, 1153, Name(e[0]));
                AddLabelCropped(x + 2, y + CellHeight - 48, CellWidth - 8, 18, 88, "0x" + e[0].ToString("X4") + (e[1] != 0 ? " hue " + e[1] : ""));
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || from.AccessLevel < AccessLevel.Administrator) return;
            TextRelay entry = info.GetTextEntry(1);
            string filter = entry == null ? m_Filter : entry.Text;
            switch (info.ButtonID)
            {
                case 0: return;
                case 1: from.SendGump(new AtlantisPaletteGump(from, m_Page - 1, filter)); return;
                case 2: from.SendGump(new AtlantisPaletteGump(from, m_Page + 1, filter)); return;
                case 3: from.SendGump(new AtlantisPaletteGump(from, 0, filter)); return;
            }
            int index = info.ButtonID - 100;
            if (index < 0 || index >= m_Entries.Count) return;
            int[] pick = m_Entries[index];
            from.SendMessage("Target where the {0} (0x{1:X4}) should stand.", Name(pick[0]), pick[0]);
            from.Target = new PlaceTarget(pick[0], pick[1], m_Page, filter);
        }

        private class PlaceTarget : Target
        {
            private readonly int m_Art, m_Hue, m_Page;
            private readonly string m_Filter;

            public PlaceTarget(int art, int hue, int page, string filter) : base(-1, true, TargetFlags.None)
            {
                m_Art = art; m_Hue = hue; m_Page = page; m_Filter = filter;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D p = targeted as IPoint3D;
                if (p == null || from.Map != Map.Atlantis)
                {
                    from.SendMessage("Target a spot in Atlantis.");
                    return;
                }
                int z = p.Z;
                Item under = targeted as Item;
                if (under != null) z = under.Z + under.ItemData.CalcHeight;
                string district = "north";
                foreach (string d in AtlantisDecor.Districts)
                    if (AtlantisDecor.InDistrict(d, p.X, p.Y)) district = d;
                AtlantisPreviewItem item = new AtlantisPreviewItem(m_Art, m_Hue, district, district + "_v2_placed");
                item.Movable = true;
                item.MoveToWorld(new Point3D(p.X, p.Y, z), from.Map);
                from.SendMessage("Placed {0} at {1},{2},{3}. It stays movable until you run [AtlantisSave over it.", Name(m_Art), p.X, p.Y, z);
                from.SendGump(new AtlantisPaletteGump(from, m_Page, m_Filter));
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                from.SendGump(new AtlantisPaletteGump(from, m_Page, m_Filter));
            }
        }
    }
}
