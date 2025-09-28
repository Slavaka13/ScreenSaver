using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenSaver
{
    /// <summary>
    /// ��� �� ������� ����, ����� � ��������� ��� � �������� ��������.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly List<Snowflake> snowflakes = new();
        private readonly Random rnd = new();

        // �������� �������
        private readonly Stopwatch sw = new();
        private long lastTicks;
        private double acc;
        private const double STEP = 1.0 / 120.0; // � �������� ������ 120 ��� � �������

        // �������
        private Bitmap? bg;
        private Bitmap? flakeSrc;
        private string? contentDir;

        // � ������� ����� ��������� �������� �������� ������ ��������
        private readonly int[] cachedSizes = new[] { 12, 16, 20, 24, 28, 32, 36, 40, 44 };
        private readonly Dictionary<int, Bitmap> flakeCache = new();

        private int flakesCount = 120;

        public MainForm()
        {
            InitializeComponent();

            // ��� � ������� �����, ����� �������� ���� ������� � ��� ��������
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.Opaque, true);

            // � ����� ���� �� ���� �����, ��� �����
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(0, 0);

            KeyPreview = true;
            KeyDown += (_, __) => Close(); // �� ����� ������� ��������
            Load += MainForm_Load;
            FormClosed += MainForm_FormClosed;

            // �������� ������� ����, ����� ���� ��� ��������
            Shown += (_, __) =>
            {
                lastTicks = Stopwatch.GetTimestamp();
                sw.Start();
                Application.Idle += Loop;
            };
        }

        /// <summary>
        /// ����� � �������� ��� � �������� �������� �� ����� Content.
        /// </summary>
        private void MainForm_Load(object? sender, EventArgs e)
        {
            string baseDir = AppContext.BaseDirectory;
            string c1 = Path.Combine(baseDir, "Content");
            string c2 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Content"));
            contentDir = Directory.Exists(c1) ? c1 : (Directory.Exists(c2) ? c2 : c1);

            string bgJpg = Path.Combine(contentDir, "background.jpg");
            string bgPng = Path.Combine(contentDir, "background.png");
            string? bgPath = File.Exists(bgJpg) ? bgJpg : (File.Exists(bgPng) ? bgPng : null);
            string flakePath = Path.Combine(contentDir, "snowflake.png");

            if (bgPath is null || !File.Exists(flakePath))
            {
                MessageBox.Show($"�� ����� ��� ��� �������� � {contentDir}", "������",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            bg = (Bitmap)Image.FromFile(bgPath);
            flakeSrc = (Bitmap)Image.FromFile(flakePath);

            BuildFlakeCache();
            InitSnowflakes();
        }

        /// <summary>
        /// ����������� ����� ��������, ����� ������ �������� �� ��� ��������.
        /// </summary>
        private void BuildFlakeCache()
        {
            flakeCache.Clear();
            if (flakeSrc == null) return;

            foreach (int s in cachedSizes)
            {
                var bmp = new Bitmap(s, s);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(flakeSrc, new Rectangle(0, 0, s, s));
                }
                flakeCache[s] = bmp;
            }
        }

        /// <summary>
        /// ������ ��� �������� ��������� �������: �������, ��������, �����.
        /// </summary>
        private void InitSnowflakes()
        {
            snowflakes.Clear();
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);

            for (int i = 0; i < flakesCount; i++)
            {
                float t = (float)rnd.NextDouble();
                float size = Lerp(12f, 44f, t);
                float vy = Lerp(70f, 200f, t);
                float amp = Lerp(3f, 14f, 1f - t);
                float spd = Lerp(0.6f, 1.2f, (float)rnd.NextDouble());
                float ph = (float)(rnd.NextDouble() * Math.PI * 2);

                int x = rnd.Next(0, w);
                int y = rnd.Next(-h, h);
                snowflakes.Add(new Snowflake(new PointF(x, y), size, vy, amp, spd, ph));
            }
        }

        /// <summary>
        /// ����� � ������ ����� � �������� �������.
        /// </summary>
        private void Loop(object? sender, EventArgs e)
        {
            while (IsIdle())
            {
                long now = Stopwatch.GetTimestamp();
                double dt = (now - lastTicks) / (double)Stopwatch.Frequency;
                lastTicks = now;

                acc += dt;
                int safety = 0;
                while (acc >= STEP && safety++ < 8)
                {
                    UpdateWorld((float)STEP);
                    acc -= STEP;
                }

                Invalidate();
                System.Threading.Thread.Sleep(1);
            }
        }

        /// <summary>
        /// �������� ��� ��������.
        /// </summary>
        private void UpdateWorld(float dt)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            foreach (var s in snowflakes)
                s.Update(dt, w, h, rnd);
        }

        /// <summary>
        /// ����� ��� � ��� ��������.
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;

            if (bg != null) g.DrawImage(bg, ClientRectangle);
            else g.Clear(Color.Black);

            if (flakeSrc == null || flakeCache.Count == 0) return;


            foreach (var s in snowflakes)
            {
                int sz = ClosestCachedSize((int)Math.Round(s.PixelSize));
                Bitmap bmp = flakeCache[sz];

                int x = (int)Math.Round(s.Position.X);
                int y = (int)Math.Round(s.Position.Y);

                g.DrawImageUnscaled(bmp, x, y);
            }
        }


        /// <summary>
        /// ����� ��� � OnPaint, ������� ����������� ��� ��������.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent) { }

        /// <summary>
        /// ��� �������� ���� ���� ��� �������, ����� �����
        /// </summary>
        private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            Application.Idle -= Loop;
            sw.Stop();
            bg?.Dispose();
            flakeSrc?.Dispose();
            foreach (var kv in flakeCache) kv.Value.Dispose();
            flakeCache.Clear();
        }

        /// <summary>
        /// �������� ��������� ������������� ������ ��������.
        /// </summary>
        private static int ClosestCachedSize(int wanted)
        {
            int[] sizes = { 12, 16, 20, 24, 28, 32, 36, 40, 44 };
            int best = sizes[0];
            int diff = Math.Abs(wanted - best);
            for (int i = 1; i < sizes.Length; i++)
            {
                int d = Math.Abs(wanted - sizes[i]);
                if (d < diff) { diff = d; best = sizes[i]; }
            }
            return best;
        }

        /// <summary>
        /// ��������, �������� �� ������� ��������� Windows.
        /// ���� �������� � ������ ����� ��������� ��������� ����.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MSG { IntPtr hWnd; uint message; IntPtr wParam; IntPtr lParam; uint time; System.Drawing.Point p; }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint fmin, uint fmax, uint remove);

        private static bool IsIdle() => !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);

        /// <summary>
        /// ������� ��� �������� �������� ����� ����� �������.
        /// </summary>
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
