using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JahidColliderFinder.Auth;
using JahidColliderFinder.Views;

namespace JahidColliderFinder
{
    public partial class ProfileForm : Form
    {
        private bool _dragging;
        private Point _dragStart;

        public ProfileForm()
        {
            InitializeComponent();
            ThemedShell.StyleForm(this, "Jahid Profile", 1020, 540);
            BuildUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(1020, 540);
            this.Name = "ProfileForm";
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
                string t = "Jahid Profile";
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

            ThemedShell.BuildSidebar(this, 2, (idx) => ThemedShell.NavigateFrom(this, idx));

            int mainX = 64;
            var card = new GlowCard { Bounds = new Rectangle(mainX + 40, 70, Width - mainX - 80, Height - 70 - 50), AccentColor = ThemedShell.Accent };
            Controls.Add(card);

            card.Controls.Add(new Label { Text = "PROFILE", Font = new Font("Consolas", 9, FontStyle.Bold), ForeColor = ThemedShell.Muted, AutoSize = true, Location = new Point(24, 14) });

            string username = AuthSession.SafeUsername();
            string sub = AuthSession.SubscriptionText();
            string exp = AuthSession.ExpiryText();
            string left = AuthSession.TimeLeftText();
            string ip = "", hwid = "", created = "", last = "";
            try
            {
                ip = AuthSession.Api?.user_data?.ip ?? "-";
                hwid = AuthSession.Api?.user_data?.hwid ?? "-";
                created = AuthSession.Api?.user_data?.createdate ?? "-";
                last = AuthSession.Api?.user_data?.lastlogin ?? "-";
                if (long.TryParse(created, out long c)) created = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).AddSeconds(c).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                if (long.TryParse(last, out long l)) last = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).AddSeconds(l).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }
            catch { }

            string[] rows = new string[]
            {
                "Username: " + username,
                "Subscription: " + sub,
                "Expires: " + exp,
                "Time Left: " + left,
                "IP: " + ip,
                "HWID: " + hwid,
                "Created: " + created,
                "Last Login: " + last,
                "Mode: " + AuthSession.AuthMode
            };

            int y = 48;
            foreach (var r in rows)
            {
                card.Controls.Add(new Label { Text = r, Font = new Font("Consolas", 9), ForeColor = ThemedShell.White, AutoSize = true, Location = new Point(24, y) });
                y += 26;
            }

            var btnLogout = new GlowButton { Text = "Logout", Bounds = new Rectangle(24, y + 10, 200, 40), AccentColor = ThemedShell.Accent };
            card.Controls.Add(btnLogout);
            btnLogout.Click += (s, e) => ThemedShell.NavigateFrom(this, 3);

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
    }
}
