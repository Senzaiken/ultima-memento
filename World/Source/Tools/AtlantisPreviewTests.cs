// Standalone regression checks against an isolated in-memory world and the real
// server map reader. Never call World.Load, World.Save, Core.Main, or Freeze here.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server;
using Server.Commands;
using Server.Items;

public static class AtlantisPreviewTests
{
    private static string WorldPath;
    private static int Checks;

    public static int Main(string[] args)
    {
        WorldPath = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e)
        {
            string name = new AssemblyName(e.Name).Name;
            if (name == "World") return Assembly.LoadFrom(Path.Combine(WorldPath, "World.exe"));
            return null;
        };
        try { Run(); return 0; }
        catch (Exception ex) { Console.WriteLine(ex); return 1; }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        ++Checks;
        Console.WriteLine("PASS: " + message);
    }

    private static object Field(object obj, string name)
    {
        return obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    }

    private static int Count(object result, string name) { return (int)Field(result, name); }

    private static object Call(string name, params object[] args)
    {
        return typeof(AtlantisDecor).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    }

    private static List<AtlantisPreviewItem> PreviewItems()
    {
        List<AtlantisPreviewItem> result = new List<AtlantisPreviewItem>();
        foreach (Item item in World.Items.Values)
            if (item is AtlantisPreviewItem && !item.Deleted) result.Add((AtlantisPreviewItem)item);
        return result;
    }

    private static void Run()
    {
        Core.Assembly = typeof(Item).Assembly;
        Core.DataDirectories.Add(Path.Combine(WorldPath, "Data/Files"));
        // Explicit empty dictionaries, rather than loading anyone's save files.
        typeof(World).GetField("m_Items", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new Dictionary<Serial, Item>());
        typeof(World).GetField("m_Mobiles", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new Dictionary<Serial, Mobile>());
        Server.Misc.MapDefinitions.Configure();
        object groups = Call("ReadManifest", Path.Combine(WorldPath, "Data/Atlantis/Decorations.json"));
        IList groupList = (IList)groups;
        int groupCount = groupList.Count, total = 0, north = 0, lamps = 0;
        foreach (object group in groupList)
        {
            IList pieces = (IList)Field(group, "Items");
            total += pieces.Count;
            if ((string)Field(group, "District") == "north") north += pieces.Count;
            foreach (object piece in pieces)
            {
                int art = Count(piece, "Art");
                if (art == 0x6380 || art == 0x6385) ++lamps;
            }
        }
        Check(groupCount >= 13 && total > 1356, "all " + groupCount + " authored groups load (" + total + " items)");
        object result = Call("ApplyPreview", Map.Atlantis, groups, "all", true);
        Check(Count(result, "Items") == total && Count(result, "Blocked") == 0, "all " + total + " placements fit the real MUL map");
        Check(World.Items.Count == 0, "check creates no world items");

        bool rejected = false;
        try { Call("ApplyPreview", Map.Sosaria, groups, "all", false); }
        catch (TargetInvocationException) { rejected = true; }
        Check(rejected && World.Items.Count == 0, "shared-file Sosaria facet is rejected");

        AtlantisPreviewItem legacy = new AtlantisPreviewItem(0x77B5, 0, "north", "north_001_shell_nursery");
        legacy.MoveToWorld(new Point3D(4330, 2525, -127), Map.Atlantis);
        rejected = false;
        try { Call("ApplyPreview", Map.Atlantis, groups, "all", false); }
        catch (TargetInvocationException) { rejected = true; }
        Check(rejected && PreviewItems().Count == 1 && !legacy.Deleted, "legacy preview requires explicit clear before the new rooms");
        legacy.Delete();

        object firstGroup = groupList[0];
        object firstPiece = ((IList)Field(firstGroup, "Items"))[0];
        int x = Count(firstPiece, "X"), y = Count(firstPiece, "Y");
        int firstSize = ((IList)Field(firstGroup, "Items")).Count;
        Static blocker = new Static(0xC30);
        blocker.MoveToWorld(new Point3D(x, y, -127), Map.Atlantis);
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", true);
        Check(Count(result, "Items") == total - firstSize && Count(result, "Blocked") == 1, "passable world decoration protects the whole group");
        blocker.Delete();

        // Inject a passable static in memory only, then restore the original cell.
        StaticTile[][][] block = Map.Atlantis.Tiles.GetStaticBlock(x >> 3, y >> 3);
        StaticTile[] original = block[x & 7][y & 7];
        StaticTile[] changed = new StaticTile[original.Length + 1];
        Array.Copy(original, changed, original.Length);
        changed[original.Length] = new StaticTile(0xC30, -127);
        block[x & 7][y & 7] = changed;
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", true);
        Check(Count(result, "Blocked") == 1, "new map decoration protects the whole group");
        block[x & 7][y & 7] = original;

        // Remove a backing wall from the in-memory map: wall displays cannot float.
        StaticTile[][][] wallBlock = Map.Atlantis.Tiles.GetStaticBlock(x >> 3, (y - 1) >> 3);
        StaticTile[] wallOriginal = wallBlock[x & 7][(y - 1) & 7];
        wallBlock[x & 7][(y - 1) & 7] = new StaticTile[0];
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", true);
        Check(Count(result, "Blocked") == 1, "wall furnishings require a real backing wall");
        wallBlock[x & 7][(y - 1) & 7] = wallOriginal;

        Mobile observer = new Mobile();
        observer.MoveToWorld(new Point3D(x, y, -127), Map.Atlantis);
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", true);
        Check(Count(result, "Blocked") == 1, "a mobile in the group is protected");
        observer.Delete();

        Static existingAtlantis = new Static(0x77B5);
        existingAtlantis.MoveToWorld(new Point3D(4280, 2610, -127), Map.Atlantis);
        Static existingSosaria = new Static(0x77B5);
        existingSosaria.MoveToWorld(new Point3D(x, y, -127), Map.Sosaria);
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", false);
        Check(Count(result, "Items") == total && PreviewItems().Count == total, "preview places every group on Atlantis");
        foreach (AtlantisPreviewItem item in PreviewItems())
            if (item.Movable || item.Decays || item.Map != Map.Atlantis) throw new Exception("Invalid preview item state");
        Check(true, "preview items are immovable, nondecaying, and on Atlantis");
        int raised = 0, rugs = 0, lit = 0, mosaics = 0;
        foreach (AtlantisPreviewItem item in PreviewItems())
        {
            if (item.Z > -127) ++raised;
            if (item.ItemID == 0xAC8 || item.ItemID == 0xAFA) ++rugs;
            if (item.ItemID == 0x56C5 && item.Z == -127 && item.ItemData.Height == 1) ++mosaics;
            if (item.ItemID == 0x6380 || item.ItemID == 0x6385)
            {
                if (item.Light != LightType.Circle150) throw new Exception("Unlit crystal fixture");
                ++lit;
            }
        }
        Check(raised > 100 && rugs > 100 && mosaics > 0 && lit == lamps, "raised table settings, carpet and mosaic overlays, pedestal statues, and lit wall crystals are placed");

        result = Call("ApplyPreview", Map.Atlantis, groups, "all", false);
        Check(Count(result, "Items") == 0 && Count(result, "Existing") == groupCount && PreviewItems().Count == total, "repeated preview creates no duplicates");

        Mobile visitor = new Mobile();
        Call("Go", visitor, Map.Atlantis, groups, "east");
        Check(visitor.Map == Map.Atlantis && visitor.X >= 4380 && visitor.X < 4472 &&
            visitor.Y >= 2584 && visitor.Y < 2636 && visitor.Z == -127, "go visits the requested district on the correct floor");
        visitor.Delete();

        string invalidManifest = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "invalid-atlantis-test.json");
        File.WriteAllText(invalidManifest, "{\"placements\":[{\"district\":\"north\",\"group\":\"north_v2_valid\",\"art\":\"0x77B5\",\"hue\":0,\"x\":4320,\"y\":2520,\"z\":-127,\"mode\":\"floor\"},{\"district\":\"north\",\"group\":\"north_v2_bad\",\"art\":\"0x77B5\",\"hue\":0,\"x\":4280,\"y\":2610,\"z\":-127,\"mode\":\"floor\"}]}");
        rejected = false;
        try { Call("ReadManifest", invalidManifest); }
        catch (TargetInvocationException) { rejected = true; }
        Check(rejected && PreviewItems().Count == total, "invalid manifest is rejected without changing the world");

        string validText = File.ReadAllText(Path.Combine(WorldPath, "Data/Atlantis/Decorations.json")).Replace("\r\n", "\n");
        List<string> unsupportedLines = new List<string>();
        foreach (string line in validText.Split('\n'))
            if (!(line.Contains("\"art\":\"0x1DC5\"") && line.Contains("\"x\":4324,\"y\":2526,"))) unsupportedLines.Add(line);
        File.WriteAllLines(invalidManifest, unsupportedLines.ToArray());
        rejected = false;
        try { Call("ReadManifest", invalidManifest); }
        catch (TargetInvocationException) { rejected = true; }
        Check(rejected && PreviewItems().Count == total, "a tabletop detail without its supporting table is rejected atomically");
        Check(validText.Contains("\n ]\n}"), "manifest ends with the expected JSON footer");
        File.WriteAllText(invalidManifest, validText.Replace("\n ]\n}", ",\n  {\"district\":\"north\",\"group\":\"north_v2_overlap\",\"art\":\"0x1DC5\",\"hue\":2826,\"x\":4324,\"y\":2526,\"z\":-127,\"mode\":\"floor\"}\n ]\n}"));
        rejected = false;
        try { Call("ReadManifest", invalidManifest); }
        catch (TargetInvocationException) { rejected = true; }
        Check(rejected, "duplicate artwork across rooms is rejected");

        AtlantisPreviewItem sample = PreviewItems()[0];
        MemoryStream stream = new MemoryStream();
        BinaryFileWriter writer = new BinaryFileWriter(stream, true);
        sample.Serialize(writer); writer.Flush(); stream.Position = 0;
        AtlantisPreviewItem loaded = new AtlantisPreviewItem(Serial.NewItem);
        World.AddItem(loaded);
        loaded.Deserialize(new BinaryFileReader(new BinaryReader(stream)));
        Check(loaded.PreviewGroup == sample.PreviewGroup && loaded.PreviewDistrict == sample.PreviewDistrict &&
            loaded.ItemID == sample.ItemID && loaded.Hue == sample.Hue && loaded.Location == sample.Location &&
            loaded.Map == Map.Atlantis && !loaded.Movable && !loaded.Decays, "save round trip preserves ownership, art, hue, position, and immovability");
        loaded.Delete(); stream.Dispose();

        sample.MoveToWorld(new Point3D(x, y, -127), Map.Sosaria);
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", false);
        Check(Count(result, "Items") == 0, "moving a preview item does not duplicate its group");
        PreviewItems()[1].Delete();
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", false);
        Check(Count(result, "Items") == 0, "partially removed groups are not duplicated");

        int removed = (int)Call("ClearPreview", "north");
        Check(removed == north - 1 && PreviewItems().Count == total - north && sample.Deleted, "district clear also finds moved items and leaves other districts");
        Check(!existingAtlantis.Deleted && !existingSosaria.Deleted, "clear preserves unrelated world items on both facets");
        result = Call("ApplyPreview", Map.Atlantis, groups, "all", false);
        Check(Count(result, "Items") == north && PreviewItems().Count == total, "cleared district can be rebuilt");
        removed = (int)Call("ClearPreview", "all");
        Check(removed == total && PreviewItems().Count == 0 && World.Items.Count == 2, "clear all removes only owned preview items");
        Check((int)Call("ClearPreview", "all") == 0, "clear is safe to repeat");

        // [AtlantisSave: snapshot a room back into a copy of the manifest.
        Call("ApplyPreview", Map.Atlantis, groups, "all", false);
        string saveManifest = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "save-test.json");
        File.Copy(Path.Combine(WorldPath, "Data/Atlantis/Decorations.json"), saveManifest, true);
        IList firstPieces = (IList)Field(firstGroup, "Items");
        int gx0 = Count(firstGroup, "MinX"), gy0 = Count(firstGroup, "MinY"), gx1 = Count(firstGroup, "MaxX"), gy1 = Count(firstGroup, "MaxY");
        HashSet<string> occupied = new HashSet<string>();
        foreach (object piece in firstPieces) occupied.Add(Count(piece, "X") + ":" + Count(piece, "Y"));
        int freeX = -1, freeY = -1;
        for (int fx = gx0; fx <= gx1 && freeX < 0; ++fx)
            for (int fy = gy0; fy <= gy1; ++fy)
            {
                bool floorTile = false, walled = false;
                foreach (StaticTile tile in Map.Atlantis.Tiles.GetStaticTiles(fx, fy, true)) if (tile.ID >= 0x519 && tile.ID <= 0x524 && tile.Z == -127) floorTile = true;
                foreach (StaticTile tile in Map.Atlantis.Tiles.GetStaticTiles(fx, fy - 1, true)) if (tile.ID >= 0xC7 && tile.ID <= 0xE6) walled = true;
                foreach (StaticTile tile in Map.Atlantis.Tiles.GetStaticTiles(fx - 1, fy, true)) if (tile.ID >= 0xC7 && tile.ID <= 0xE6) walled = true;
                if (floorTile && !walled && !occupied.Contains(fx + ":" + fy) && Map.Atlantis.CanFit(fx, fy, -127, 5, true, true, true)) { freeX = fx; freeY = fy; break; }
            }
        Check(freeX > 0, "a free floor tile exists inside the first room for the save test");
        Static added = new Static(0x1AFC);
        added.MoveToWorld(new Point3D(freeX, freeY, -127), Map.Atlantis);
        Static raisedNowhere = new Static(0x3031);
        raisedNowhere.MoveToWorld(new Point3D(freeX, freeY, -100), Map.Atlantis);
        object save = typeof(AtlantisSave).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { Map.Atlantis, new Rectangle2D(freeX - 1, freeY - 1, 3, 3), "harness_room", saveManifest, "harness", true });
        int saved = Count(save, "Items");
        Check(saved == firstSize + 1 && Count(save, "DroppedGroups") == 1 && Count(save, "Converted") == 1 && Count(save, "Rekeyed") == firstSize,
            "save absorbs the touched group, adopts the added static and drops the old group");
        Check(((IList)Field(save, "Notes")).Count == 1, "a raised item with nothing under it is reported and skipped");
        IList savedGroups = (IList)Call("ReadManifest", saveManifest);
        int savedTotal = 0, savedSurface = 0, savedWall = 0; bool savedKey = false;
        foreach (object group in savedGroups)
        {
            savedTotal += ((IList)Field(group, "Items")).Count;
            if ((string)Field(group, "Key") == (string)Field(firstGroup, "District") + "_v2_harness_room")
            {
                savedKey = true;
                foreach (object piece in (IList)Field(group, "Items"))
                {
                    string mode = (string)Field(piece, "Mode");
                    if (mode == "surface") ++savedSurface;
                    if (mode == "north" || mode == "west") ++savedWall;
                }
            }
        }
        Check(savedKey && savedGroups.Count == groupCount && savedTotal == total + 1 && savedSurface > 0 && savedWall > 0,
            "saved manifest parses, keeps every other group, and infers surface and wall placements");
        Check(added.Deleted && !raisedNowhere.Deleted && PreviewItems().Count == total + 1 && File.Exists(saveManifest),
            "the static became an owned preview item and the manifest file was written");
        Check(File.ReadAllText(saveManifest).Contains("\"savedInGame\": true") && (bool)Call("SavedInGame", saveManifest), "saved manifest carries the in-game flag");
        int frozen = (int)typeof(AtlantisSave).GetMethod("SetMovable", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "all", true });
        int refrozen = (int)typeof(AtlantisSave).GetMethod("SetMovable", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "north", false });
        Check(frozen == total + 1 && refrozen == north + 1, "unfreeze and freeze toggle movability by district");
        raisedNowhere.Delete();
        Check((int)Call("ClearPreview", "all") == total + 1 && World.Items.Count == 2, "clear removes adopted items too");

        Console.WriteLine("Completed {0} checks; no save files or MUL files were written.", Checks);
    }
}
