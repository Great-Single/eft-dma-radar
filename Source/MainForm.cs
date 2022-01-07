﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eft_dma_radar
{
    public partial class MainForm : Form
    {
        private readonly Memory _memory; // Reference to memory module
        private readonly object _renderLock = new object();
        private readonly List<Map> _allMaps; // Contains all maps from \\Maps folder
        private int _mapIndex = 0;
        private Map _currentMap; // Current Selected Map
        private Bitmap _currentRender; // Currently rendered frame
        private float _zoom = 1.0f;
        private int _lastZoom = 0;
        private const int _maxZoom = 3500;
        private Player CurrentPlayer
        {
            get
            {
                return _memory.Players.FirstOrDefault(x => x.Value.Type is PlayerType.CurrentPlayer).Value;
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm(Memory memory)
        {
            InitializeComponent();
            _memory = memory;
            _allMaps = new List<Map>();
            LoadMaps();
            this.DoubleBuffered = true; // Prevent flickering
            this.mapCanvas.Paint += mapCanvas_OnPaint;
            this.Shown += MainForm_Shown;
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            while (true)
            {
                await Task.Delay(5);
                mapCanvas.Invalidate();
            }
        }

        /// <summary>
        /// Load map files (.PNG) and Configs (.JSON) from \\Maps folder.
        /// </summary>
        private void LoadMaps()
        {
            var dir = new DirectoryInfo($"{Environment.CurrentDirectory}\\Maps");
            if (!dir.Exists)
            {
                dir.Create();
                throw new IOException("Unable to locate Maps folder!");
            }
            var maps = dir.GetFiles("*.png"); // Get all PNG Files
            if (maps.Length == 0) throw new IOException("Maps folder is empty!");
            foreach (var map in maps)
            {
                var name = Path.GetFileNameWithoutExtension(map.Name); // map name ex. 'CUSTOMS' w/o extension
                var config = new FileInfo(Path.Combine(dir.FullName, name + ".json")); // Full config path
                if (!config.Exists) throw new IOException($"Map JSON Config missing for {map}");
                _allMaps.Add(new Map
                (
                    name.ToUpper(),
                    new Bitmap(Image.FromFile(map.FullName)),
                    MapConfig.LoadFromFile(config.FullName))
                );
            }
            _currentMap = _allMaps[0];
            label_Map.Text = _currentMap.Name;
            _currentRender = (Bitmap)_currentMap.MapFile.Clone();
        }

        /// <summary>
        /// Draw/Render on Map Canvas
        /// </summary>
        private void mapCanvas_OnPaint(object sender, PaintEventArgs e)
        {
            lock (_renderLock)
            {
                Player currentPlayer;
                if (_memory.InGame && (currentPlayer = CurrentPlayer) is not null)
                {
                    var render = GetRender(currentPlayer); // Construct next frame
                    mapCanvas.Image = render; // Render next frame

                    // Cleanup Resources
                    Recycler.Bitmaps.Add(_currentRender); // Queue previous frame for disposal
                    _currentRender = render; // Store reference of current frame
                }
            }
        }

        /// <summary>
        /// Draws next render frame and returns a completed Bitmap
        /// </summary>
        private Bitmap GetRender(Player currentPlayer)
        {
            int zoom = (int)(_maxZoom * _zoom); // Get zoom level
            double aspect = (double)mapCanvas.Width / (double)mapCanvas.Height; // Get aspect ratio of drawing canvas (ex. 16:9)
            int strokeLength = zoom / 125; // Lower constant = longer stroke
            int fontSize = zoom / 100;
            if (strokeLength < 5) strokeLength = 5; // Min value
            int strokeWidth = zoom / 300; // Lower constant = wider stroke
            if (strokeWidth < 4) strokeWidth = 4; // Min value
            var render = (Bitmap)_currentMap.MapFile.Clone(); // Get a fresh map to draw on
            using (var drawFont = new Font("Arial", fontSize, FontStyle.Bold))
            using (var drawBrush = new SolidBrush(Color.Black))
            using (var grn = new Pen(Color.DarkGreen)
            {
                Width = strokeWidth
            })
            using (var ltGrn = new Pen(Color.LightGreen)
            {
                Width = strokeWidth
            })
            using (var red = new Pen(Color.Red)
            {
                Width = strokeWidth
            })
            using (var ylw = new Pen(Color.Yellow)
            {
                Width = strokeWidth
            })
            using (var vlt = new Pen(Color.Violet)
            {
                Width = strokeWidth
            })
            using (var wht = new Pen(Color.White)
            {
                Width = strokeWidth
            })
            using (var blk = new Pen(Color.Black)
            {
                Width = strokeWidth
            })
            {
                MapPosition currentPlayerPos;
                Vector3 currentPlayerRawPos;
                double currentPlayerDirection;
                lock (currentPlayer) // Obtain object lock
                {
                    currentPlayerRawPos = currentPlayer.Position;
                    currentPlayerDirection = Deg2Rad(currentPlayer.Direction);
                    label_Pos.Text = $"X: {currentPlayer.Position.X}\r\nY: {currentPlayer.Position.Y}\r\nZ: {currentPlayer.Position.Z}";
                }
                currentPlayerPos = VectorToMapPos(currentPlayerRawPos);
                // Get map frame bounds (Based on Zoom Level, centered on Current Player)
                var xZoom = (int)Math.Round(zoom * aspect);
                var bounds = new Rectangle(currentPlayerPos.X - xZoom / 2, currentPlayerPos.Y - zoom / 2, xZoom, zoom);
                Console.WriteLine($"ASPECT: {aspect}, X:{xZoom}, Y:{zoom}");
                using (var gr = Graphics.FromImage(render)) // Get fresh frame
                {
                    // Draw Current Player
                    {
                        gr.DrawEllipse(grn, new Rectangle(currentPlayerPos.GetPlayerCirclePoint(strokeLength), new Size(strokeLength * 2, strokeLength * 2)));
                        Point point1 = new Point(currentPlayerPos.X, currentPlayerPos.Y);
                        Point point2 = new Point((int)(currentPlayerPos.X + Math.Cos(currentPlayerDirection) * trackBar_AimLength.Value), (int)(currentPlayerPos.Y + Math.Sin(currentPlayerDirection) * trackBar_AimLength.Value));
                        gr.DrawLine(grn, point1, point2);
                    }
                    // Draw Other Players
                    var allPlayers = _memory.Players;
                    if (allPlayers is not null) foreach (KeyValuePair<string, Player> player in allPlayers) // Draw PMCs
                    {
                        lock (player.Value) // Obtain object lock
                        {
                            if (player.Value.Type is PlayerType.CurrentPlayer) continue; // Already drawn current player, move on
                            var playerPos = VectorToMapPos(player.Value.Position);
                            if (playerPos.X >= bounds.Left // Only draw if in bounds
                                && playerPos.Y >= bounds.Top
                                && playerPos.X <= bounds.Right
                                && playerPos.Y <= bounds.Bottom)
                            {
                                Pen pen;
                                var playerDirection = Deg2Rad(player.Value.Direction);
                                var aimLength = trackBar_EnemyAim.Value;
                                if (player.Value.IsAlive is false)
                                { // Draw 'X'
                                    gr.DrawLine(blk, new Point(playerPos.X - strokeLength / 2, playerPos.Y + strokeLength / 2), new Point(playerPos.X + strokeLength / 2, playerPos.Y - strokeLength / 2));
                                    gr.DrawLine(blk, new Point(playerPos.X - strokeLength / 2, playerPos.Y - strokeLength / 2), new Point(playerPos.X + strokeLength / 2, playerPos.Y + strokeLength / 2));
                                    continue;
                                }
                                else if (player.Value.Type is PlayerType.Teammate)
                                {
                                    pen = ltGrn;
                                    aimLength = trackBar_AimLength.Value; // Allies use player's aim length
                                }
                                else if (player.Value.Type is PlayerType.PMC) pen = red;
                                else if (player.Value.Type is PlayerType.PlayerScav) pen = wht;
                                else if (player.Value.Type is PlayerType.AIBoss) pen = vlt;
                                else if (player.Value.Type is PlayerType.AIScav) pen = ylw;
                                else pen = red; // Default
                                {
                                    var height = playerPos.Height - currentPlayerPos.Height;
                                    var dist = (int)Math.Round(Math.Sqrt((Math.Pow(currentPlayerRawPos.X - player.Value.Position.X, 2) + Math.Pow(currentPlayerRawPos.Y - player.Value.Position.Y, 2))));
                                    gr.DrawString($"{player.Value.Name} ({player.Value.Health})\nH: {height} D: {dist}", drawFont, drawBrush, playerPos.GetNamePoint(fontSize));
                                    gr.DrawEllipse(pen, new Rectangle(playerPos.GetPlayerCirclePoint(strokeLength / 2), new Size((int)(strokeLength), (int)(strokeLength)))); // smaller circle
                                    Point point1 = new Point(playerPos.X, playerPos.Y);
                                    Point point2 = new Point((int)(playerPos.X + Math.Cos(playerDirection) * aimLength), (int)(playerPos.Y + Math.Sin(playerDirection) * aimLength));
                                    gr.DrawLine(pen, point1, point2);
                                }
                            }
                        }
                    }
                    /// ToDo - Handle Loot/Items
                }
                return CropImage(render, bounds); // Return the portion of the map to be rendered based on Zoom Level
            }
        }

        private static double Deg2Rad(float deg)
        {
            deg = deg - 90; // Degrees offset needed for game
            return (Math.PI / 180) * deg;
        }

        /// <summary>
        /// Returns a rectangular section of a source Bitmap
        /// </summary>
        private Bitmap CropImage(Bitmap source, Rectangle section)
        {
            try
            {
                var bitmap = new Bitmap(section.Width, section.Height);
                using (var gr = Graphics.FromImage(bitmap))
                {
                    gr.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                    return bitmap;
                }
            }
            finally
            {
                Recycler.Bitmaps.Add(source); // Queue for disposal
            }
        }

        /// <summary>
        /// Convert game positional values to UI Map Coordinates.
        /// </summary>
        private MapPosition VectorToMapPos(Vector3 vector)
        {
            var zeroX = _currentMap.ConfigFile.X;
            var zeroY = _currentMap.ConfigFile.Y;
            var scale = _currentMap.ConfigFile.Scale;

            var x = zeroX + (vector.X * scale);
            var y = zeroY - (vector.Y * scale); // Invert 'Y' unity 0,0 bottom left, C# top left
            return new MapPosition()
            {
                X = (int)Math.Round(x),
                Y = (int)Math.Round(y),
                Height = (int)Math.Round(vector.Z)
            };
        }


        private void trackBar_Zoom_Scroll(object sender, EventArgs e)
        {
            int amtChanged = trackBar_Zoom.Value - _lastZoom;
            _lastZoom = trackBar_Zoom.Value;
            _zoom -= (.01f) * (amtChanged);
        }

        private void button_Map_Click(object sender, EventArgs e)
        {
            if (_mapIndex == _allMaps.Count - 1) _mapIndex = 0; // Start over when end of maps reached
            else _mapIndex++; // Move onto next map
            lock (_renderLock) // Don't switch map mid-render
            {
                _currentMap = _allMaps[_mapIndex]; // Swap map
            }
            label_Map.Text = _currentMap.Name;
        }
    }
}
