using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;


public class MapData
{
    public string Name { get; set; } = "Ostrov Parodie";
    public List<RoadData> Roads { get; set; } = new();
    public List<RoundaboutData> Roundabouts { get; set; } = new();
    public List<SquareZoneData> Squares { get; set; } = new();
    public List<BuildingData> Buildings { get; set; } = new();

    public static string GetMapPath()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        return Path.Combine(desktop, "ostrov_map.json");
    }

    public string Save(string customPath = null)
    {
        string path = Path.GetFullPath(customPath ?? GetMapPath());
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(path, json);

        Debug.WriteLine($"[MAP EDITOR] Ulozeno: {path}");
        return path;
    }

    public static MapData Load(string customPath = null)
    {
        string path = Path.GetFullPath(customPath ?? GetMapPath());
        if (!File.Exists(path))
            return new MapData();

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MapData>(json) ?? new MapData();
    }
}

public class RoadData
{
    public string Name { get; set; } = "Ulice";
    public float RoadWidth { get; set; } = 64f;
    public float SidewalkWidth { get; set; } = 96f; // Šířka chodníkového pásu (vlevo i vpravo od cesty)
    public string Surface { get; set; } = "Asphalt";
    public bool SmoothCurve { get; set; } = true;
    public List<Vector2> ControlPoints { get; set; } = new();
}

public class RoundaboutData
{
    public Vector2 Center { get; set; }
    public float Radius { get; set; } = 120f;
    public float RoadWidth { get; set; } = 56f;
    public float SidewalkWidth { get; set; } = 84f;
    public bool HasCenterIsland { get; set; } = true;
}

public class SquareZoneData
{
    public string Name { get; set; } = "Namesti";
    public Vector2 Position { get; set; } // Levý horní roh
    public float Width { get; set; }
    public float Height { get; set; }
    public string Surface { get; set; } = "Plaza"; // Pro volný pohyb chodců + jiný grip
}

public class BuildingData
{
    public Vector2 Position { get; set; } // Střed budovy (sedí na tvůj Collider)
    public float Width { get; set; }
    public float Height { get; set; }
    public float Rotation { get; set; }
    public string RoofPreset { get; set; } = "Flat"; // Připravené pro tvary střech / textury
    public int R { get; set; } = 145;
    public int G { get; set; } = 75;
    public int B { get; set; } = 75;
}