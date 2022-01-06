﻿using System.Text.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Text.Json.Serialization;

namespace eft_dma_radar
{

    // GUI Testing Structures, may change

    public class DebugStopwatch
    {
        private readonly Stopwatch _sw;
        private readonly string _name;

        public DebugStopwatch(string name = null)
        {
            _name = name;
            _sw = new Stopwatch();
            _sw.Start();
        }

        public void Stop()
        {
            _sw.Stop();
            TimeSpan ts = _sw.Elapsed;
            Console.WriteLine($"{_name} Stopwatch Runtime: {ts.Milliseconds}ms");
        }
    }

    /// <summary>
    /// Defines Player Unit Type (Player,PMC,Scav,etc.)
    /// </summary>
    public enum PlayerType
    {
        Default,
        CurrentPlayer,
        Teammate,
        PMC, // side 0x1 0x2
        AIScav, // side 0x4
        AIBoss, // ToDo
        PlayerScav // side 0x4
    }

    /// <summary>
    /// Defines map position for the UI Map.
    /// </summary>
    public struct MapPosition
    {
        public int X;
        public int Y;
        public int Height; // Z

        /// <summary>
        /// Get exact player location.
        /// </summary>
        public Point GetPlayerCirclePoint(int offset)
        {
            return new Point(X - offset, Y - offset);
        }

        /// <summary>
        /// Gets Point where player name should be drawn.
        /// </summary>
        public Point GetNamePoint(int offset)
        {
            return new Point(X + offset, Y - offset);
        }
    }

    /// <summary>
    /// Defines a .PNG Map File and corresponding .JSON config file from \\Maps folder.
    /// </summary>
    public class Map
    {
        public readonly string Name;
        public readonly Bitmap MapFile;
        public readonly MapConfig ConfigFile;

        public Map(string name, Bitmap map, MapConfig config)
        {
            Name = name;
            MapFile = map;
            ConfigFile = config;
        }
    }

    /// <summary>
    /// Defines a .JSON Map Config File
    /// </summary>
    public class MapConfig
    {
        [JsonPropertyName("x")]
        public float X { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
        [JsonPropertyName("z")]
        public float Z { get; set; }
        [JsonPropertyName("scale")]
        public float Scale { get; set; }


        public static MapConfig LoadFromFile(string file)
        {
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<MapConfig>(json);
        }
    }

    // EFT/Unity Structures (WIP)

    public struct GameObjectManager
    {
        public ulong LastTaggedNode; // 0x0

        public ulong TaggedNodes; // 0x8

        public ulong LastMainCameraTaggedNode; // 0x10

        public ulong MainCameraTaggedNodes; // 0x18

        public ulong LastActiveNode; // 0x20

        public ulong ActiveNodes; // 0x28

    }

    public struct BaseObject
    {
        public ulong previousObjectLink; //0x0000
        public ulong nextObjectLink; //0x0008
        public ulong obj; //0x0010
	};


    public struct Matrix34
    {
        public Vector4 vec0;
        public Vector4 vec1;
        public Vector4 vec2;
    }

}
