using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using JahidColliderFinder.Auth;
using JahidColliderFinder.Views;

namespace JahidColliderFinder
{
    public partial class AuthChoiceForm : Form
    {
        private static bool _inited = false;
        private bool _dragging;
        private Point _dragStart;

        public AuthChoiceForm()
        {
            InitializeComponent();
            ThemedShell.StyleForm(this, "Jahid Aimbot Ai Finder", 460, 560);
            BuildUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(460, 560);
            this.Name = "AuthChoiceForm";
            this.ResumeLayout(false);
        }

        private void BuildUI()
        {
            var bar = new Panel { Bounds = new Rectangle(0, 0, Width, 42), BackColor = ThemedShell.BG };
            bar.Paint += (s, e) =>
            {
                using var pen = new Pen(ThemedShell.Border, 1f);
                e.Graphics.DrawLine(pen, 0, 41, Width, 41);
                using var dot = new SolidBrush(ThemedShell.Accent);
                e.Graphics.FillEllipse(dot, 18, 17, 8, 8);
                using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                string t = "Jahid Aimbot Collider Finder";
                var sz = TextRenderer.MeasureText(e.Graphics, t, font);
                TextRenderer.DrawText(e.Graphics, t, font, new Point((Width - sz.Width) / 2, 12), ThemedShell.White);
            };
            bar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _dragStart = PointToClient(Cursor.Position); } };
            bar.MouseMove += (s, e) => { if (_dragging) { var p = PointToScreen(e.Location); Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y); } };
            bar.MouseUp += (s, e) => _dragging = false;
            Controls.Add(bar);

            var btnClose = new Button { Text = "X", Size = new Size(28, 24), Location = new Point(Width - 40, 9), FlatStyle = FlatStyle.Flat, ForeColor = ThemedShell.Muted, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Environment.Exit(0);
            bar.Controls.Add(btnClose);

            var card = new GlowCard { Bounds = new Rectangle(30, 70, Width - 60, Height - 110), AccentColor = ThemedShell.Accent };
            Controls.Add(card);

            var hdr = new Label { Text = "Welcome", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = ThemedShell.White, BackColor = Color.Transparent, AutoSize = true, Location = new Point(40, 20) };
            card.Controls.Add(hdr);
            var sub = new Label { Text = "Choose login method", Font = new Font("Consolas", 9), ForeColor = ThemedShell.Muted, BackColor = Color.Transparent, AutoSize = true, Location = new Point(42, 56) };
            card.Controls.Add(sub);

            var b1 = new GlowButton { Text = "Username + Password", Bounds = new Rectangle(40, 110, card.Width - 80, 48), AccentColor = ThemedShell.Accent };
            b1.Click += (s, e) => { var f = new UserPassLoginForm(); f.Show(); this.Hide(); };
            card.Controls.Add(b1);

            var b2 = new GlowButton { Text = "Licence Key", Bounds = new Rectangle(40, 175, card.Width - 80, 48), AccentColor = ThemedShell.Accent };
            b2.Click += (s, e) => { var f = new LicenseLoginForm(); f.Show(); this.Hide(); };
            card.Controls.Add(b2);

            var foot = new Label { Text = "DEVELOPED BY REGIX STUDIO", Font = new Font("Consolas", 8), ForeColor = ThemedShell.Muted, BackColor = Color.Transparent, AutoSize = true, Location = new Point(40, 250) };
            card.Controls.Add(foot);

            Load += (s, e) => DoInit();
        }

        private void DoInit()
        {
            if (_inited) return;
            try
            {
                AuthSession.Api.init();
                _inited = true;
                if (AuthSession.Api.response.message == "invalidver")
                {
                    string link = AuthSession.Api.app_data.downloadLink ?? "";
                    if (!string.IsNullOrEmpty(link))
                    {
                        var r = MessageBox.Show("New version available. Yes opens download link, No exits.", "Auto update", MessageBoxButtons.YesNo);
                        if (r == DialogResult.Yes) { try { Process.Start(link); } catch { } }
                    }
                    else
                    {
                        MessageBox.Show("Version mismatch and no download link is set. Contact developer.");
                    }
                    Environment.Exit(0);
                    return;
                }
                if (!AuthSession.Api.response.success)
                {
                    MessageBox.Show(AuthSession.Api.response.message);
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init failed: " + ex.Message);
                Environment.Exit(0);
            }
        }
    }
}
