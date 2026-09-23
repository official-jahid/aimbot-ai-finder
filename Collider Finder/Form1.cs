#pragma warning disable CA1416

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using JahidColliderFinder.Auth;
using JahidColliderFinder.Views;

namespace JahidColliderFinder
{


    public partial class MainForm : Form
    {
        private static readonly HttpClient Client = new HttpClient();




        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS m);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS { public int Left, Right, Top, Bottom; }

        // -- Palette: tailored dark periwinkle theme --
        static readonly Color BG = Color.FromArgb(11, 12, 16);
        static readonly Color InnerPanel = Color.FromArgb(18, 20, 29);
        static readonly Color CardSurface = Color.FromArgb(24, 26, 38);
        static readonly Color BorderColor = Color.FromArgb(35, 39, 54);
        static readonly Color RowLine = Color.FromArgb(30, 34, 48);
        static readonly Color TextColor = Color.FromArgb(255, 255, 255);
        static readonly Color TextMuted = Color.FromArgb(90, 98, 117);
        static readonly Color TextDim = Color.FromArgb(59, 66, 82);
        static readonly Color Placeholder = Color.FromArgb(59, 66, 82);
        static readonly Color Accent = Color.FromArgb(138, 148, 248);
        static readonly Color AccentBlue = Color.FromArgb(138, 148, 248);
        static readonly Color AccentIndigo = Color.FromArgb(138, 148, 248);
        static readonly Color AccentGreen = Color.FromArgb(138, 148, 248);

        // -- Fields --
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly Random _rnd = new Random();
        private readonly Timer _animTimer = new Timer() { Interval = 30 };
        private readonly Timer _clockTimer = new Timer() { Interval = 1000 };

        private Panel? _canvasPanel;
        private Label? _clockLabel;
        private GlowTextBox? tb_collider, tb_value, tb_read, tb_result;
        private GlowLabel? lbl_r1, lbl_r2, lbl_r3;
        private bool _dragging;
        private Point _dragStart;

        public MainForm()
        {
            InitializeComponent();
            SetupWindow();
            SetupParticles();
            SetupUI();
            VisualState.Changed += ApplyVisualState;
            ApplyVisualState();
            if (!AuthSession.IsAuthed)
            {
                Load += (s, e) =>
                {
                    if (!AuthSession.IsAuthed)
                    {
                        var c = Application.OpenForms.OfType<AuthChoiceForm>().FirstOrDefault() ?? new AuthChoiceForm();
                        c.Show();
                        this.Hide();
                    }
                };
            }
        }

        private void ApplyVisualState()
        {
            if (_clockLabel != null) _clockLabel.Visible = VisualState.ShowClock;
            if (VisualState.ShowClock) { try { _clockTimer.Start(); } catch { } }
            else { try { _clockTimer.Stop(); } catch { } }
            try { _canvasPanel?.Invalidate(); } catch { }
            try { Invalidate(true); } catch { }
        }

        // -------------------------------------------------------------
        private void SetupWindow()
        {
            Text = "Jahid Aimbot Ai Finder";
            Size = new Size(1020, 540);
            FormBorderStyle = FormBorderStyle.None;
            BackColor = BG;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            int v = 2;
            DwmSetWindowAttribute(Handle, 2, ref v, 4);
            var m = new MARGINS { Left = 1, Right = 1, Top = 1, Bottom = 1 };
            DwmExtendFrameIntoClientArea(Handle, ref m);

            MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = e.Location; } };
            MouseMove += (s, e) => { if (_dragging) { var p = PointToScreen(e.Location); Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y); } };
            MouseUp += (s, e) => _dragging = false;
        }

        // -------------------------------------------------------------
        private void SetupParticles()
        {
            for (int i = 0; i < 80; i++)
                _particles.Add(new Particle(_rnd, Width, Height, init: true));

            _canvasPanel = new Panel { Bounds = new Rectangle(0, 0, Width, Height), BackColor = Color.Transparent };
            _canvasPanel.Paint += DrawParticles;
            Controls.Add(_canvasPanel);
            _canvasPanel.SendToBack();

            _animTimer.Tick += (s, e) =>
            {
                foreach (var p in _particles) p.Update(Width, Height, _rnd);
                _canvasPanel?.Invalidate();
            };
            _animTimer.Start();
        }

        private void DrawParticles(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (VisualState.ShowGrid)
            {
                using var gridPen = new Pen(Color.FromArgb(7, 138, 148, 248), 1f);
                for (int x = 0; x < Width; x += 40) g.DrawLine(gridPen, x, 0, x, Height);
                for (int y = 0; y < Height; y += 40) g.DrawLine(gridPen, 0, y, Width, y);
            }

            if (!VisualState.ShowParticles) return;

            for (int i = 0; i < _particles.Count; i++)
                for (int j = i + 1; j < _particles.Count; j++)
                {
                    float dx = _particles[i].X - _particles[j].X;
                    float dy = _particles[i].Y - _particles[j].Y;
                    float d = Compat.Sqrt(dx * dx + dy * dy);
                    if (d < 90)
                    {
                        int alpha = (int)((1 - d / 90f) * 22);
                        using var pen = new Pen(Color.FromArgb(alpha, Accent), 0.5f);
                        g.DrawLine(pen, _particles[i].X, _particles[i].Y, _particles[j].X, _particles[j].Y);
                    }
                }

            foreach (var p in _particles)
            {
                int a = Compat.Clamp((int)(p.Alpha * 255), 0, 255);
                using var br = new SolidBrush(Color.FromArgb(a, p.Color));
                g.FillEllipse(br, p.X - p.Size, p.Y - p.Size, p.Size * 2, p.Size * 2);
            }
        }

        // -------------------------------------------------------------
        private void SetupUI()
        {
            int W = Width, H = Height;
            int sideW = 64;

            // -- Title bar --
            var bar = new Panel { Bounds = new Rectangle(0, 0, W, 42), BackColor = BG };
            bar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var divPen = new Pen(BorderColor, 1f);
                g.DrawLine(divPen, 0, 41, W, 41);
                using var glowBr = new SolidBrush(Color.FromArgb(40, Accent));
                g.FillEllipse(glowBr, 14, 13, 16, 16);
                using var dotBr = new SolidBrush(Accent);
                g.FillEllipse(dotBr, 18, 17, 8, 8);
                using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                string title = "Jahid Aimbot Collider Finder";
                Size sz = TextRenderer.MeasureText(g, title, font);
                int cx = (W - sz.Width) / 2;
                TextRenderer.DrawText(g, title, font, new Point(cx, 12), TextColor);
            };
            bar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = PointToClient(Cursor.Position); } };
            bar.MouseMove += (s, e) => { if (_dragging) { var p = PointToScreen(e.Location); Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y); } };
            bar.MouseUp += (s, e) => _dragging = false;

            var btnClose = MakeTitleBtn("X", TextMuted);
            btnClose.Location = new Point(W - 40, 9);
            btnClose.Click += (s, e) => Application.Exit();

            var btnMin = MakeTitleBtn("-", TextMuted);
            btnMin.Location = new Point(W - 78, 9);
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _clockLabel = new Label
            {
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Consolas", 9),
                BackColor = Color.Transparent,
                Location = new Point(W - 210, 14)
            };
            _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            _clockTimer.Tick += (s, e) => { if (_clockLabel != null) _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss"); };
            _clockTimer.Start();

            bar.Controls.AddRange(new Control[] { btnClose, btnMin, _clockLabel });
            Controls.Add(bar);
            bar.BringToFront();

            // -- Left navigation sidebar: FontAwesome icons only with hover tooltip --
            var sidebar = new Panel { Bounds = new Rectangle(0, 42, sideW, H - 42 - 30), BackColor = InnerPanel };
            sidebar.Paint += (s, e) =>
            {
                using var pen = new Pen(BorderColor, 1f);
                e.Graphics.DrawLine(pen, sideW - 1, 0, sideW - 1, sidebar.Height);
            };
            Controls.Add(sidebar);
            sidebar.BringToFront();

            var navItems = new (IconChar icon, string tip)[]
            {
                (IconChar.Crosshairs, "Collider Finder"),
                (IconChar.SlidersH, "Visual Settings"),
                (IconChar.UserCircle, "Profile"),
                (IconChar.SignOutAlt, "Logout"),
            };
            var navTips = new ToolTip { AutoPopDelay = 3000, InitialDelay = 400, ReshowDelay = 200, ShowAlways = true };
            for (int i = 0; i < navItems.Length; i++)
            {
                int idx = i;
                var ib = new IconButton
                {
                    IconChar = navItems[i].icon,
                    IconColor = i == 0 ? TextColor : TextMuted,
                    IconSize = 22,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = i == 0 ? CardSurface : Color.Transparent,
                    Size = new Size(48, 48),
                    Location = new Point(8, 18 + i * 62),
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                ib.FlatAppearance.BorderSize = i == 0 ? 1 : 0;
                ib.FlatAppearance.BorderColor = Accent;
                ib.FlatAppearance.MouseOverBackColor = CardSurface;
                ib.FlatAppearance.MouseDownBackColor = CardSurface;
                navTips.SetToolTip(ib, navItems[i].tip);
                ib.Click += (s, e) => ThemedShell.NavigateFrom(this, idx);
                sidebar.Controls.Add(ib);
            }

            // -- Header centered in main area --
            int mainX = sideW;
            int mainW = W - sideW;
            var hdr = new Label
            {
                Text = "Jahid Collider Finder",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(mainX + mainW / 2 - 155, 56)
            };
            Controls.Add(hdr); hdr.BringToFront();

            // -- Inner container panel --
            var container = new Panel
            {
                Bounds = new Rectangle(mainX + 16, 100, mainW - 32, H - 100 - 30 - 12),
                BackColor = InnerPanel
            };
            container.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var bgBr = new SolidBrush(InnerPanel);
                GlowCard.FillRR(g, bgBr, new Rectangle(0, 0, container.Width - 1, container.Height - 1), 12);
                using var pen = new Pen(BorderColor, 1f);
                GlowCard.DrawRR(g, pen, new Rectangle(0, 0, container.Width - 1, container.Height - 1), 12);
            };
            Controls.Add(container);
            container.BringToFront();

            // -- Three finder cards fill container; visual settings live in dedicated window --
            int cy = 34, ch = 300, cw = 272, gap = 28;
            int sx = (container.Width - (cw * 3 + gap * 2)) / 2;

            BuildCardIn(container, sx, cy, cw, ch, "Write Finder", Accent, CardType.Collider);
            BuildCardIn(container, sx + cw + gap, cy, cw, ch, "Basic To Ai Read", Accent, CardType.ReadResult);
            BuildCardIn(container, sx + (cw + gap) * 2, cy, cw, ch, "Ai To Basic Read", Accent, CardType.ResultRead);

            AddArrowIn(container, sx + cw + 8, cy + ch / 2 - 16);
            AddArrowIn(container, sx + cw * 2 + gap + 8, cy + ch / 2 - 16);

            var movedNote = new Label
            {
                Text = "Visual settings moved to sidebar > Visual Settings",
                Font = new Font("Consolas", 8),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point((container.Width - 340) / 2, cy + ch + 14)
            };
            container.Controls.Add(movedNote);

            // -- Status bar --
            var sb = new Panel { Bounds = new Rectangle(0, H - 30, W, 30), BackColor = InnerPanel };
            sb.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new Pen(BorderColor, 1f);
                g.DrawLine(pen, 0, 0, W, 0);
                using var dotBr = new SolidBrush(Accent);
                g.FillEllipse(dotBr, 16, 11, 7, 7);
                using var font = new Font("Consolas", 8);
                TextRenderer.DrawText(g, "DEVELOPED BY REGIX STUDIO", font, new Point(30, 8), TextMuted);
            };
            Controls.Add(sb); sb.BringToFront();
        }

        private void AddSettingRow(Panel parent, int index, string name, ref bool current, Action<bool> onChange)
        {
            bool local = current;
            int rowH = 30;
            var row = new Panel
            {
                Bounds = new Rectangle(8, index * rowH, parent.Width - 16, rowH),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(row);

            var lbl = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9),
                ForeColor = local ? TextColor : TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(8, 7)
            };
            row.Controls.Add(lbl);

            var status = new Label
            {
                Text = local ? "On" : "Off",
                Font = new Font("Consolas", 8),
                ForeColor = local ? TextColor : TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(row.Width - 92, 9)
            };
            row.Controls.Add(status);

            var tg = new ToggleSwitch
            {
                IsOn = local,
                AccentColor = Accent,
                Bounds = new Rectangle(row.Width - 52, 5, 40, 20)
            };
            row.Controls.Add(tg);
            row.Resize += (s, e) =>
            {
                status.Location = new Point(row.Width - 92, 9);
                tg.Location = new Point(row.Width - 52, 5);
            };

            tg.StateChanged += (isOn) =>
            {
                lbl.ForeColor = isOn ? TextColor : TextMuted;
                status.Text = isOn ? "On" : "Off";
                status.ForeColor = isOn ? TextColor : TextMuted;
                onChange(isOn);
            };

            row.Paint += (s, e) =>
            {
                if (index < 3)
                {
                    using var pen = new Pen(RowLine, 1f);
                    e.Graphics.DrawLine(pen, 8, rowH - 1, row.Width - 8, rowH - 1);
                }
            };
        }

        private enum CardType { Collider, ReadResult, ResultRead }

        private void BuildCard(int x, int y, int w, int h, string title, Color accent, CardType type)
        {
            BuildCardIn(this, x, y, w, h, title, accent, type);
        }

        private void BuildCardIn(Control host, int x, int y, int w, int h, string title, Color accent, CardType type)
        {
            var card = new GlowCard { Bounds = new Rectangle(x, y, w, h), AccentColor = accent, GlowOn = VisualState.GlowEnabled };
            host.Controls.Add(card);
            card.BringToFront();

            card.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextColor,
                BackColor = Color.Transparent,
                Location = new Point(w / 2 - 72, 14)
            });

            int iy = 52;
            if (type == CardType.Collider)
            {
                tb_collider = AddField(card, "COLLIDER INPUT", "0x8C, 0xA3...", accent, iy); iy += 66;
                tb_value = AddField(card, "TARGET VALUE", "Enter value...", accent, iy); iy += 66;
            }
            else if (type == CardType.ReadResult)
            {
                tb_read = AddField(card, "READ VALUE HEX", "0x8C", accent, iy); iy += 66;
            }
            else
            {
                tb_result = AddField(card, "RESULT VALUE HEX", "0xD4", accent, iy); iy += 66;
            }

            string btnTxt = type == CardType.ResultRead ? "FIND READ" : "Find";
            var btn = new GlowButton { Text = btnTxt, Bounds = new Rectangle(20, iy, w - 40, 36), AccentColor = accent };
            btn.Click += (s, e) => OnCalculate(type);
            card.Controls.Add(btn); iy += 46;

            var resBox = new GlowResultBox { Bounds = new Rectangle(20, iy, w - 40, 42), AccentColor = accent };
            resBox.SetValue(type == CardType.ResultRead ? "Read: " : "Result: ", "-");
            card.Controls.Add(resBox);

            if (type == CardType.Collider && host == this) lbl_r1 = resBox.ValueLabel;
            else if (type == CardType.ReadResult && host == this) lbl_r2 = resBox.ValueLabel;
            else if (type == CardType.ResultRead && host == this) lbl_r3 = resBox.ValueLabel;

            if (host != this)
            {
                if (type == CardType.Collider) lbl_r1 = resBox.ValueLabel;
                else if (type == CardType.ReadResult) lbl_r2 = resBox.ValueLabel;
                else lbl_r3 = resBox.ValueLabel;
            }
        }

        private static GlowTextBox AddField(Control parent, string label, string placeholder, Color accent, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = "> " + label,
                Font = new Font("Consolas", 8),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, y)
            });
            var tb = new GlowTextBox
            {
                Bounds = new Rectangle(20, y + 18, parent.Width - 40, 32),
                PlaceholderText = placeholder,
                AccentColor = accent
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private void AddArrow(int x, int y)
        {
            AddArrowIn(this, x, y);
        }

        private void AddArrowIn(Control host, int x, int y)
        {
            var a = new Label
            {
                Text = ">",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = TextDim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(x, y)
            };
            host.Controls.Add(a); a.BringToFront();
        }

        private static Button MakeTitleBtn(string text, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(28, 24),
                FlatStyle = FlatStyle.Flat,
                ForeColor = fore,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, fore);
            return b;
        }

        // -- Calculation: unchanged logic --
        private async void OnCalculate(CardType type)
        {
            try
            {

                int baseOffset =
                    (int)Offsets.WeaponOnHand
                    - (int)Offsets.Camera
                    + (int)Offsets.WeaponRecoil;

                int Jahidggg =
                    (int)Offsets.Player_Data
                    + (int)Offsets.WeaponRecoil;


                if (type == CardType.Collider)
                {
                    if (string.IsNullOrWhiteSpace(tb_collider?.InnerText) ||
                        string.IsNullOrWhiteSpace(tb_value?.InnerText))
                    {
                        SetResult(lbl_r1, "INVALID");
                        return;
                    }

                    int collider = ParseHex(tb_collider.InnerText);
                    int value = ParseHex(tb_value.InnerText);

                    int result = collider - value - Jahidggg;


                    if (result < 0)
                    {
                        SetResult(lbl_r1, "INVALID");
                        return;
                    }

                    SetResult(lbl_r1, ToHex(result));



                }


                else if (type == CardType.ReadResult)
                {

                    await Task.Delay(2000);

                    if (string.IsNullOrWhiteSpace(tb_read?.InnerText))
                    {
                        SetResult(lbl_r2, "INVALID");
                        return;
                    }

                    int read = ParseHex(tb_read.InnerText);


                    int result = read + baseOffset;

                    if (result <= 0)
                    {
                        SetResult(lbl_r2, "INVALID");
                        return;
                    }

                    SetResult(lbl_r2, ToHex(result));
                }


                else
                {

                    if (string.IsNullOrWhiteSpace(tb_result?.InnerText))
                    {
                        SetResult(lbl_r3, "INVALID");
                        return;
                    }

                    int result = ParseHex(tb_result.InnerText);


                    int read = result - baseOffset;

                    if (read < 0)
                    {
                        SetResult(lbl_r3, "INVALID");
                        return;
                    }

                    SetResult(lbl_r3, ToHex(read));
                }
            }
            catch
            {
                if (type == CardType.Collider)
                    SetResult(lbl_r1, "INVALID");
                else if (type == CardType.ReadResult)
                    SetResult(lbl_r2, "INVALID");
                else
                    SetResult(lbl_r3, "INVALID");
            }
        }
        private static void SetResult(GlowLabel? lbl, string val)
        {
            if (lbl == null) return;
            lbl.Text = val;
            lbl.HasValue = val != "-";
            lbl.RefreshColor();
            lbl.Parent?.Invalidate();
        }

        private async void MainForm_Load_1(object sender, EventArgs e)
        {

        }

        private static int ParseHex(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2); ; ;
            return Convert.ToInt32(s, 16);
        }

        private static string ToHex(int n) => "0x" + n.ToString("X2");

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(BorderColor, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _animTimer.Dispose(); _clockTimer.Dispose(); }
            base.Dispose(disposing);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }

    // ================================================================
    internal class Particle
    {
        public float X, Y, Vx, Vy, Size, Alpha, Life, MaxLife;
        public Color Color;
        private static readonly Color[] Palette = {
            Color.FromArgb(138, 148, 248),
            Color.FromArgb(146, 155, 255),
            Color.FromArgb(90, 98, 117)
        };
        public Particle(Random rnd, int w, int h, bool init = false) => Reset(rnd, w, h, init);
        public void Reset(Random rnd, int w, int h, bool init = false)
        {
            X = (float)(rnd.NextDouble() * w);
            Y = init ? (float)(rnd.NextDouble() * h) : h + 10f;
            Vx = (float)((rnd.NextDouble() - 0.5) * 0.4);
            Vy = -(float)(rnd.NextDouble() * 0.5 + 0.15);
            Size = (float)(rnd.NextDouble() * 1.6 + 0.4);
            Alpha = (float)(rnd.NextDouble() * 0.5 + 0.1);
            Color = Palette[rnd.Next(Palette.Length)];
            Life = 0; MaxLife = rnd.Next(150, 350);
        }
        public void Update(int w, int h, Random rnd)
        {
            X += Vx; Y += Vy; Life++;
            Alpha = (float)(0.55 * Math.Sin(Life / MaxLife * Math.PI));
            if (Life >= MaxLife || Y < -10) Reset(rnd, w, h);
        }
    }

    // ================================================================
    internal class GlowCard : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(138, 148, 248);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool GlowOn { get; set; } = true;
        public GlowCard() { DoubleBuffered = true; BackColor = Color.FromArgb(24, 26, 38); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var bgBr = new SolidBrush(BackColor);
            FillRR(g, bgBr, rect, 10);
            using var borderPen = new Pen(Color.FromArgb(35, 39, 54), 1f);
            DrawRR(g, borderPen, rect, 10);

            if (GlowOn)
            {
                using var topBr = new LinearGradientBrush(new Point(20, 0), new Point(Width - 20, 0),
                    Color.Transparent, Color.Transparent);
                topBr.InterpolationColors = new ColorBlend(3)
                {
                    Colors = new[] { Color.Transparent, Color.FromArgb(60, AccentColor), Color.Transparent },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                g.FillRectangle(topBr, 20, 0, Width - 40, 1);

                using var glowBr = new LinearGradientBrush(new Rectangle(Width - 60, 0, 60, 60),
                    Color.FromArgb(14, AccentColor), Color.Transparent, 135f);
                g.FillRectangle(glowBr, Width - 60, 0, 60, 60);
            }

            using var divPen = new Pen(Color.FromArgb(30, 34, 48), 1f);
            g.DrawLine(divPen, 20, 42, Width - 20, 42);

            base.OnPaint(e);
        }

        public static void FillRR(Graphics g, Brush b, Rectangle r, int radius)
        { using var p = RRPath(r, radius); g.FillPath(b, p); }

        public static void DrawRR(Graphics g, Pen pen, Rectangle r, int radius)
        { using var p = RRPath(r, radius); g.DrawPath(pen, p); }

        public static GraphicsPath RRPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ================================================================
    internal class GlowTextBox : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(138, 148, 248);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlaceholderText { get; set; } = "";
        public string InnerText => _inner.Text;

        private readonly TextBox _inner;
        private readonly Label _trail;
        private bool _focused;

        public GlowTextBox()
        {
            DoubleBuffered = true;
            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(24, 26, 38),
                ForeColor = Color.FromArgb(255, 255, 255),
                Font = new Font("Segoe UI", 9),
                Location = new Point(12, 8),
                Width = 150
            };
            _trail = new Label
            {
                Text = "o",
                Font = new Font("Consolas", 8),
                ForeColor = Color.FromArgb(59, 66, 82),
                BackColor = Color.Transparent,
                AutoSize = true
            };
            _inner.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            _inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            _inner.TextChanged += (s, e) => Invalidate();
            Controls.Add(_inner);
            Controls.Add(_trail);
            _trail.BringToFront();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _inner.Width = Width - 48;
            _trail.Location = new Point(Width - 24, 9);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var bgBr = new SolidBrush(Color.FromArgb(24, 26, 38));
            GlowCard.FillRR(g, bgBr, rect, 7);

            Color bc = _focused ? AccentColor : Color.FromArgb(35, 39, 54);
            using var pen = new Pen(bc, 1f);
            GlowCard.DrawRR(g, pen, rect, 7);

            if (_focused)
            { using var gp = new Pen(Color.FromArgb(22, AccentColor), 3f); GlowCard.DrawRR(g, gp, new Rectangle(1, 1, Width - 3, Height - 3), 7); }

            if (!_focused && string.IsNullOrEmpty(_inner.Text))
            { using var ph = new SolidBrush(Color.FromArgb(59, 66, 82)); g.DrawString(PlaceholderText, _inner.Font, ph, 12f, 7f); }
        }
    }

    // ================================================================
    internal class GlowButton : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(138, 148, 248);
        private bool _hover, _pressed;

        public GlowButton() { DoubleBuffered = true; Cursor = Cursors.Hand; Font = new Font("Segoe UI", 10, FontStyle.Bold); }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e)
        { _pressed = false; Invalidate(); if (ClientRectangle.Contains(e.Location)) OnClick(EventArgs.Empty); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int o = _pressed ? 1 : 0;
            var rect = new Rectangle(o, o, Width - o * 2 - 1, Height - o * 2 - 1);

            Color c1 = Scale(AccentColor, _hover ? 0.55f : 0.45f);
            Color c2 = Scale(AccentColor, _hover ? 0.75f : 0.65f);
            using var grad = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), c1, c2, 135f);
            GlowCard.FillRR(g, grad, rect, 7);

            if (_hover) { using var gp = new Pen(Color.FromArgb(70, AccentColor), 2f); GlowCard.DrawRR(g, gp, new Rectangle(1, 1, Width - 3, Height - 3), 7); }

            using var bp = new Pen(Color.FromArgb(60, AccentColor), 1f);
            GlowCard.DrawRR(g, bp, rect, 7);

            using var tb2 = new SolidBrush(Color.White);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Text, Font, tb2, new RectangleF(0, 0, Width, Height), sf);
        }

        private static Color Scale(Color c, float f) => Color.FromArgb(
            Compat.Clamp((int)(c.R * f), 0, 255),
            Compat.Clamp((int)(c.G * f), 0, 255),
            Compat.Clamp((int)(c.B * f), 0, 255));
    }

    // ================================================================
    internal class GlowResultBox : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(138, 148, 248);

        public GlowLabel ValueLabel { get; } = new GlowLabel
        {
            AutoSize = false,
            Width = 85,
            Height = 24,
            Font = new Font("Consolas", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(90, 98, 117),
            BackColor = Color.Transparent,
            Location = new Point(70, 9),
            TextAlign = ContentAlignment.MiddleLeft
        };

        private readonly Label _prefix = new Label
        {
            AutoSize = true,
            Font = new Font("Consolas", 8),
            ForeColor = Color.FromArgb(59, 66, 82),
            BackColor = Color.Transparent,
            Location = new Point(14, 14)
        };

        private readonly Button _copyBtn = new Button
        {
            Text = "COPY",
            Width = 50,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 34, 48),
            ForeColor = Color.White,
            Location = new Point(165, 8),
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 7.5F, FontStyle.Bold)
        };

        public GlowResultBox()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;

            _copyBtn.FlatAppearance.BorderSize = 0;

            _copyBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(ValueLabel.Text) &&
                    ValueLabel.Text != "-")
                {
                    Clipboard.SetText(ValueLabel.Text);
                }
            };

            Controls.Add(_prefix);
            Controls.Add(ValueLabel);
            Controls.Add(_copyBtn);
        }

        public void SetValue(string prefix, string val)
        {
            _prefix.Text = prefix.ToUpper();
            ValueLabel.Text = val;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using var bgBr = new SolidBrush(Color.FromArgb(18, 20, 29));
            GlowCard.FillRR(g, bgBr, rect, 7);

            Color bc = ValueLabel.HasValue
                ? Color.FromArgb(60, AccentColor)
                : Color.FromArgb(35, 39, 54);

            using var pen = new Pen(bc, 1f);
            GlowCard.DrawRR(g, pen, rect, 7);
        }
    }



    // ================================================================
    internal class GlowLabel : Label
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasValue { get; set; } = false;

        public void RefreshColor()
        {
            ForeColor = (HasValue && Parent is GlowResultBox rb)
                ? rb.AccentColor
                : Color.FromArgb(90, 98, 117);
        }
    }

    // ================================================================
    internal class SidebarButton : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string IconText { get; set; } = "H";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Tip { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsActive { get; set; } = false;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(138, 148, 248);

        public SidebarButton()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Size = new Size(48, 48);
            Font = new Font("Segoe UI", 13, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(4, 4, Width - 9, Height - 9);

            if (IsActive)
            {
                using var glow = new Pen(Color.FromArgb(45, AccentColor), 4f);
                GlowCard.DrawRR(g, glow, new Rectangle(6, 6, Width - 13, Height - 13), 10);
                using var bg = new SolidBrush(Color.FromArgb(24, 26, 38));
                GlowCard.FillRR(g, bg, rect, 10);
                using var pen = new Pen(AccentColor, 1.5f);
                GlowCard.DrawRR(g, pen, rect, 10);
            }

            Color fc = IsActive ? Color.White : Color.FromArgb(90, 98, 117);
            using var tb = new SolidBrush(fc);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(IconText, Font, tb, new RectangleF(0, 0, Width, Height), sf);
        }
    }

    // ================================================================
    internal class ToggleSwitch : Control
    {
        private bool _isOn = true;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsOn
        {
            get => _isOn;
            set { if (_isOn != value) { _isOn = value; Invalidate(); } }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(138, 148, 248);
        public event Action<bool>? StateChanged;

        public ToggleSwitch()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Size = new Size(40, 20);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            IsOn = !IsOn;
            StateChanged?.Invoke(IsOn);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new Rectangle(0, 0, Width - 1, Height - 1);

            if (IsOn)
            {
                using var bg = new SolidBrush(Color.FromArgb(38, 40, 62));
                FillPill(g, bg, track);
                using var pen = new Pen(Color.FromArgb(70, AccentColor), 1f);
                DrawPill(g, pen, track);
                int d = Height - 6;
                using var knob = new SolidBrush(AccentColor);
                g.FillEllipse(knob, Width - d - 3, 3, d, d);
            }
            else
            {
                using var bg = new SolidBrush(Color.FromArgb(24, 26, 38));
                FillPill(g, bg, track);
                using var pen = new Pen(Color.FromArgb(35, 39, 54), 1f);
                DrawPill(g, pen, track);
                int d = Height - 6;
                using var knob = new SolidBrush(Color.FromArgb(59, 66, 82));
                g.FillEllipse(knob, 3, 3, d, d);
            }
        }

        private static void FillPill(Graphics g, Brush b, Rectangle r)
        {
            using var p = PillPath(r);
            g.FillPath(b, p);
        }
        private static void DrawPill(Graphics g, Pen pen, Rectangle r)
        {
            using var p = PillPath(r);
            g.DrawPath(pen, p);
        }
        private static GraphicsPath PillPath(Rectangle r)
        {
            int d = r.Height;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 90, 180);
            p.AddArc(r.Right - d - 1, r.Y, d, d, 270, 180);
            p.CloseFigure();
            return p;
        }
    }

    // ================================================================
    internal class ThinScrollBar : Control
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BarColor { get; set; } = Color.FromArgb(35, 39, 54);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ThumbColor { get; set; } = Color.FromArgb(138, 148, 248);
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Value { get; private set; } = 0;
        public event Action<int>? Scrolled;

        private int _viewH = 1;
        private int _contentH = 1;
        private bool _dragging = false;
        private int _dragY = 0;
        private int _dragVal = 0;

        public ThinScrollBar()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Width = 2;
        }

        public void SetRange(int viewH, int contentH, int val)
        {
            _viewH = Math.Max(1, viewH);
            _contentH = Math.Max(1, contentH);
            Value = Math.Max(0, val);
            Invalidate();
        }

        private int MaxOff() => Math.Max(0, _contentH - _viewH);

        private Rectangle ThumbRect()
        {
            int max = MaxOff();
            if (max <= 0) return new Rectangle(0, 0, Width, Height);
            float ratio = (float)_viewH / _contentH;
            int th = Math.Max(14, (int)(Height * ratio));
            int track = Height - th;
            int y = max == 0 ? 0 : (int)(track * ((float)Value / max));
            return new Rectangle(0, y, Width, th);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _dragging = true;
            _dragY = e.Y;
            _dragVal = Value;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            int max = MaxOff();
            if (max <= 0) return;
            int th = ThumbRect().Height;
            int track = Math.Max(1, Height - th);
            int dy = e.Y - _dragY;
            int dv = (int)(dy * ((float)max / track));
            Value = Math.Max(0, Math.Min(max, _dragVal + dv));
            Scrolled?.Invoke(Value);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using var bg = new SolidBrush(Color.FromArgb(24, 26, 38));
            g.FillRectangle(bg, 0, 0, Width, Height);
            var tr = ThumbRect();
            using var th = new SolidBrush(MaxOff() <= 0 ? BarColor : ThumbColor);
            g.FillRectangle(th, tr);
        }
    }
}
