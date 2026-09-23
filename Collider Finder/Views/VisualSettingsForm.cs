using System;
using System.Drawing;
using System.Windows.Forms;
using JahidColliderFinder.Views;

namespace JahidColliderFinder
{
    public partial class VisualSettingsForm : Form
    {
        private bool _dragging;
        private Point _dragStart;
        private int _scrollOff = 0;

        public VisualSettingsForm()
        {
            InitializeComponent();
            ThemedShell.StyleForm(this, "Jahid Visual Settings", 1020, 540);
            BuildUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(1020, 540);
            this.Name = "VisualSettingsForm";
            this.ResumeLayout(false);
        }

        private void BuildUI()
        {
            var bar = new Panel { Bounds = new Rectangle(0, 0, Width, 42), BackColor = ThemedShell.BG };
            bar.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemedShell.Border, 1f);
                e.Graphics.DrawLine(pen, 0, 41, Width, 41);
                using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                string t = "Jahid Visual Settings";
                var sz = TextRenderer.MeasureText(e.Graphics, t, font);
                TextRenderer.DrawText(e.Graphics, t, font, new Point((Width - sz.Width) / 2, 12), ThemedShell.White);
            };
            bar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = PointToClient(Cursor.Position); } };
            bar.MouseMove += (s, e) => { if (_dragging) { var p = PointToScreen(e.Location); Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y); } };
            bar.MouseUp += (s, e) => _dragging = false;
            Controls.Add(bar);

            var btnClose = new Button { Text = "X", Size = new Size(28, 24), Location = new Point(Width - 40, 9), FlatStyle = FlatStyle.Flat, ForeColor = ThemedShell.Muted, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { this.Hide(); ThemedShell.NavigateFrom(this, 0); };
            bar.Controls.Add(btnClose);

            ThemedShell.BuildSidebar(this, 1, (idx) => ThemedShell.NavigateFrom(this, idx));

            int mainX = 64;
            int mainW = Width - mainX;
            var container = new Panel { Bounds = new Rectangle(mainX + 40, 70, mainW - 80, Height - 70 - 50), BackColor = ThemedShell.Inner };
            container.Paint += (s, e) =>
            {
                using var bg = new SolidBrush(ThemedShell.Inner);
                GlowCard.FillRR(e.Graphics, bg, new Rectangle(0, 0, container.Width - 1, container.Height - 1), 12);
                using var pen = new Pen(ThemedShell.Border, 1f);
                GlowCard.DrawRR(e.Graphics, pen, new Rectangle(0, 0, container.Width - 1, container.Height - 1), 12);
            };
            Controls.Add(container);

            var card = new GlowCard { Bounds = new Rectangle(24, 16, container.Width - 48, container.Height - 32), AccentColor = ThemedShell.Accent };
            container.Controls.Add(card);

            card.Controls.Add(new Label { Text = "VISUAL SETTINGS", Font = new Font("Consolas", 9, FontStyle.Bold), ForeColor = ThemedShell.Muted, AutoSize = true, Location = new Point(20, 12) });

            var viewport = new Panel { Bounds = new Rectangle(12, 40, card.Width - 28, card.Height - 52), BackColor = Color.Transparent };
            card.Controls.Add(viewport);
            var inner = new Panel { Bounds = new Rectangle(0, 0, viewport.Width, 4 * 44), BackColor = Color.Transparent };
            viewport.Controls.Add(inner);

            var thin = new ThinScrollBar { Bounds = new Rectangle(card.Width - 16, 40, 2, card.Height - 52), BarColor = ThemedShell.Border, ThumbColor = ThemedShell.Accent };
            card.Controls.Add(thin);

            void Refresh()
            {
                int maxOff = Math.Max(0, inner.Height - viewport.Height);
                thin.SetRange(viewport.Height, inner.Height, _scrollOff);
                thin.Visible = inner.Height > viewport.Height;
                inner.Top = -Math.Min(_scrollOff, maxOff);
            }

            viewport.MouseWheel += (s, e) =>
            {
                int maxOff = Math.Max(0, inner.Height - viewport.Height);
                _scrollOff = Math.Max(0, Math.Min(maxOff, _scrollOff - e.Delta / 4));
                Refresh();
            };
            thin.Scrolled += (v) =>
            {
                int maxOff = Math.Max(0, inner.Height - viewport.Height);
                _scrollOff = Math.Max(0, Math.Min(maxOff, v));
                inner.Top = -_scrollOff;
            };

            AddRow(inner, 0, "Particles", VisualState.ShowParticles, (v) => { VisualState.ShowParticles = v; VisualState.Notify(); });
            AddRow(inner, 1, "Grid Lines", VisualState.ShowGrid, (v) => { VisualState.ShowGrid = v; VisualState.Notify(); });
            AddRow(inner, 2, "Glow Effects", VisualState.GlowEnabled, (v) => { VisualState.GlowEnabled = v; VisualState.Notify(); });
            AddRow(inner, 3, "Clock", VisualState.ShowClock, (v) => { VisualState.ShowClock = v; VisualState.Notify(); });
            Refresh();

            var sb = new Panel { Bounds = new Rectangle(0, Height - 30, Width, 30), BackColor = ThemedShell.Inner };
            sb.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemedShell.Border, 1f);
                e.Graphics.DrawLine(pen, 0, 0, Width, 0);
                using var font = new Font("Consolas", 8);
                TextRenderer.DrawText(e.Graphics, "DEVELOPED BY REGIX STUDIO", font, new Point(30, 8), ThemedShell.Muted);
            };
            Controls.Add(sb);
        }

        private void AddRow(Panel parent, int index, string name, bool current, Action<bool> onChange)
        {
            int rowH = 44;
            var row = new Panel { Bounds = new Rectangle(8, index * rowH, parent.Width - 16, rowH), BackColor = Color.Transparent };
            parent.Controls.Add(row);

            var lbl = new Label { Text = name, Font = new Font("Segoe UI", 10), ForeColor = current ? ThemedShell.White : ThemedShell.Muted, AutoSize = true, Location = new Point(10, 12) };
            row.Controls.Add(lbl);
            var status = new Label { Text = current ? "On" : "Off", Font = new Font("Consolas", 9), ForeColor = current ? ThemedShell.White : ThemedShell.Muted, AutoSize = true, Location = new Point(row.Width - 100, 14) };
            row.Controls.Add(status);
            var tg = new ToggleSwitch { IsOn = current, AccentColor = ThemedShell.Accent, Bounds = new Rectangle(row.Width - 56, 12, 40, 20) };
            row.Controls.Add(tg);
            row.Resize += (s, e) => { status.Location = new Point(row.Width - 100, 14); tg.Location = new Point(row.Width - 56, 12); };
            tg.StateChanged += (v) =>
            {
                lbl.ForeColor = v ? ThemedShell.White : ThemedShell.Muted;
                status.Text = v ? "On" : "Off";
                status.ForeColor = v ? ThemedShell.White : ThemedShell.Muted;
                onChange(v);
            };
            row.Paint += (s, e) =>
            {
                if (index < 3)
                {
                    using var pen = new Pen(Color.FromArgb(30, 34, 48), 1f);
                    e.Graphics.DrawLine(pen, 10, rowH - 1, row.Width - 10, rowH - 1);
                }
            };
        }
    }
}
