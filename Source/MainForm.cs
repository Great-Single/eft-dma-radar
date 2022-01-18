using System;
using System.Collections.Concurrent;
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
        private readonly object _renderLock = new object();
        private readonly List<Map> _allMaps; // Contains all maps from \\Maps folder
        private int _mapIndex = 0;
        private Map _currentMap; // Current Selected Map
        private Bitmap _currentRender; // Currently rendered frame
        private Rectangle _bounds;
        private const int _strokeWidth = 6;
        private Player CurrentPlayer
        {
            get
            {
                return Memory.Players.FirstOrDefault(x => x.Value.Type is PlayerType.CurrentPlayer).Value;
            }
        }
        private ConcurrentDictionary<string, Player> AllPlayers
        {
            get
            {
                return Memory.Players;
            }
        }
        private bool InGame
        {
            get
            {
                return Memory.InGame;
            }
        }

        private readonly Font _drawFont = new Font("Arial", 17, FontStyle.Bold);
        private readonly SolidBrush _drawBrush = new SolidBrush(Color.Black);
        private readonly Pen _greenPen = new Pen(Color.DarkGreen)
        {
            Width = _strokeWidth
        };
        private readonly Pen _ltgreenPen = new Pen(Color.LightGreen)
        {
            Width = _strokeWidth
        };
        private readonly Pen _redPen = new Pen(Color.Red)
        {
            Width = _strokeWidth
        };
        private readonly Pen _yellowPen = new Pen(Color.Yellow)
        {
            Width = _strokeWidth
        };
        private readonly Pen _violetPen = new Pen(Color.Violet)
        {
            Width = _strokeWidth
        };
        private readonly Pen _whitePen = new Pen(Color.White)
        {
            Width = _strokeWidth
        };
        private readonly Pen _blackPen = new Pen(Color.Black)
        {
            Width = _strokeWidth
        };


        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
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
                await Task.Delay(1);
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
                if (this.InGame && (currentPlayer = this.CurrentPlayer) is not null)
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
        /// Draws next render frame and returns a completed Bitmap.
        /// </summary>
        private Bitmap GetRender(Player currentPlayer)
        {
            int zoom = (int)Math.Round(_currentMap.ConfigFile.MaxZoom * (.01f * trackBar_Zoom.Value)); // Get zoom level
            double aspect = (double)mapCanvas.Width / (double)mapCanvas.Height; // Get aspect ratio of drawing canvas (ex. 16:9)

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
            var xPos = currentPlayerPos.X - xZoom / 2;
            if (xPos < 0) xPos = 0;
            var yPos = currentPlayerPos.Y - zoom / 2;
            if (yPos < 0) yPos = 0;
            if (xPos + xZoom > _currentMap.MapFile.Width) xZoom = _currentMap.MapFile.Width - xPos;
            if (yPos + zoom > _currentMap.MapFile.Height) zoom = _currentMap.MapFile.Height - yPos;
            _bounds = new Rectangle(xPos, yPos, xZoom, zoom);

            var render = ZoomImage((Bitmap)_currentMap.MapFile.Clone(), _bounds); // Get a zoomed section to draw on

            using (var gr = Graphics.FromImage(render)) // Get fresh frame
            {
                // Draw Current Player
                {
                    IsInZoomedBounds(currentPlayerPos, out var zoomedCurrentPlayerPos);
                    gr.DrawEllipse(_greenPen, new Rectangle(zoomedCurrentPlayerPos.GetPlayerCirclePoint(), new Size(24, 24)));
                    Point point1 = new Point(zoomedCurrentPlayerPos.X, zoomedCurrentPlayerPos.Y);
                    Point point2 = new Point((int)(zoomedCurrentPlayerPos.X + Math.Cos(currentPlayerDirection) * trackBar_AimLength.Value), (int)(zoomedCurrentPlayerPos.Y + Math.Sin(currentPlayerDirection) * trackBar_AimLength.Value));
                    gr.DrawLine(_greenPen, point1, point2);
                }
                // Draw Other Players
                var allPlayers = this.AllPlayers;
                if (allPlayers is not null) foreach (KeyValuePair<string, Player> player in allPlayers) // Draw PMCs
                {
                    lock (player.Value) // Obtain object lock
                    {
                        if (player.Value.Type is PlayerType.CurrentPlayer) continue; // Already drawn current player, move on
                        if (!player.Value.IsActive && player.Value.IsAlive) continue; // Skip exfil'd players
                        var playerPos = VectorToMapPos(player.Value.Position);
                        if (!IsInZoomedBounds(playerPos, out var zoomedPlayerPos)) continue;
                        Pen pen;
                        var playerDirection = Deg2Rad(player.Value.Direction);
                        var aimLength = trackBar_EnemyAim.Value;
                        if (player.Value.IsAlive is false)
                        { // Draw 'X'
                            gr.DrawLine(_blackPen, new Point(zoomedPlayerPos.X - 10, zoomedPlayerPos.Y + 10), new Point(zoomedPlayerPos.X + 10, zoomedPlayerPos.Y - 10));
                            gr.DrawLine(_blackPen, new Point(zoomedPlayerPos.X - 10, zoomedPlayerPos.Y - 10), new Point(zoomedPlayerPos.X + 10, zoomedPlayerPos.Y + 10));
                            continue;
                        }
                        else if (player.Value.Type is PlayerType.Teammate)
                        {
                            pen = _ltgreenPen;
                            aimLength = trackBar_AimLength.Value; // Allies use player's aim length
                        }
                        else if (player.Value.Type is PlayerType.PMC) pen = _redPen;
                        else if (player.Value.Type is PlayerType.PlayerScav) pen = _whitePen;
                        else if (player.Value.Type is PlayerType.AIBoss) pen = _violetPen;
                        else if (player.Value.Type is PlayerType.AIScav) pen = _yellowPen;
                        else pen = _redPen; // Default
                        {
                            var plyrHeight = (int)Math.Round(playerPos.Height - currentPlayerPos.Height);
                            var plyrDist = (int)Math.Round(Math.Sqrt((Math.Pow(currentPlayerRawPos.X - player.Value.Position.X, 2) + Math.Pow(currentPlayerRawPos.Y - player.Value.Position.Y, 2))));
                            gr.DrawString($"{player.Value.Name} ({player.Value.Health})\nH: {plyrHeight} D: {plyrDist}", _drawFont, _drawBrush, zoomedPlayerPos.GetNamePoint());
                            gr.DrawEllipse(pen, new Rectangle(zoomedPlayerPos.GetPlayerCirclePoint(), new Size(24, 24))); // smaller circle
                            Point point1 = new Point(zoomedPlayerPos.X, zoomedPlayerPos.Y);
                            Point point2 = new Point((int)Math.Round((zoomedPlayerPos.X + Math.Cos(playerDirection) * aimLength)), (int)Math.Round((zoomedPlayerPos.Y + Math.Sin(playerDirection) * aimLength)));
                            gr.DrawLine(pen, point1, point2);
                        }
                    }
                }
                /// ToDo - Handle Loot/Items
            }
            return render; // Return the portion of the map to be rendered based on Zoom Level
        }

        /// <summary>
        /// Provide a zoomed bitmap.
        /// </summary>
        private static Bitmap ZoomImage(Bitmap source, Rectangle bounds)
        {
            try
            {
                Bitmap cropped = source.Clone(bounds, source.PixelFormat);
                return cropped;
            }
            catch (OutOfMemoryException) { return null; }
        }

        private bool IsInZoomedBounds(MapPosition location, out MapPosition offsetLocation)
        {
            if (location.X >= _bounds.Left && location.X <= _bounds.Right 
                && location.Y >= _bounds.Top && location.Y <= _bounds.Bottom)
            {
                offsetLocation = new MapPosition()
                {
                    X = location.X - _bounds.Left,
                    Y = location.Y - _bounds.Top,
                    Height = location.Height
                };
                return true;
            }
            else
            {
                offsetLocation = new MapPosition();
                return false;
            }
        }

        private static double Deg2Rad(float deg)
        {
            deg = deg - 90; // Degrees offset needed for game
            return (Math.PI / 180) * deg;
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

        private void ToggleMap()
        {
            if (_mapIndex == _allMaps.Count - 1) _mapIndex = 0; // Start over when end of maps reached
            else _mapIndex++; // Move onto next map
            lock (_renderLock) // Don't switch map mid-render
            {
                _currentMap = _allMaps[_mapIndex]; // Swap map
            }
            label_Map.Text = _currentMap.Name;
        }


        private void button_Map_Click(object sender, EventArgs e)
        {
            ToggleMap();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) // Raised on Close()
        {
            try
            {
                Memory.Shutdown();
            }
            finally { base.OnFormClosing(e); }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.F1))
            {
                if (trackBar_Zoom.Value - 5 > 1) trackBar_Zoom.Value-=5;
                return true;
            }
            else if (keyData == (Keys.F2))
            {
                if (trackBar_Zoom.Value + 5 < 100) trackBar_Zoom.Value+=5;
                return true;
            }
            else if (keyData == (Keys.F3))
            {
                ToggleMap();
                return true;
            }
            else if (keyData == (Keys.F4))
            {
                this.checkBox_Loot.Checked = !this.checkBox_Loot.Checked; // Toggle loot
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void checkBox_Loot_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
