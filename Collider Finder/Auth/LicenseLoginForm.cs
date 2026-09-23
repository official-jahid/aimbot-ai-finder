using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using JahidColliderFinder.Auth;
using JahidColliderFinder.Views;

namespace JahidColliderFinder
{
    public partial class LicenseLoginForm : Form
    {
        private bool _dragging;
        private Point _dragStart;

        public LicenseLoginForm()
        {
            InitializeComponent();
            ThemedShell.StyleForm(this, "Jahid Aimbot Ai Finder", 460, 560);
            BuildUI();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(460, 560);
            this.Name = "LicenseLoginForm";
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
                string t = "Licence Key Login";
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

            card.Controls.Add(new Label { Text = "LICENCE KEY", Font = new Font("Consolas", 8), ForeColor = ThemedShell.Muted, AutoSize = true, Location = new Point(40, 20) });
            var tbKey = new GlowTextBox { Bounds = new Rectangle(40, 40, card.Width - 80, 34), PlaceholderText = "XXXXXX-XXXXXX-XXXXXX", AccentColor = ThemedShell.Accent };
            card.Controls.Add(tbKey);

            var btnGo = new GlowButton { Text = "Activate", Bounds = new Rectangle(40, 95, card.Width - 80, 44), AccentColor = ThemedShell.Accent };
            card.Controls.Add(btnGo);

            var btnBack = new Button { Text = "Back", Size = new Size(120, 30), Location = new Point(40, 155), FlatStyle = FlatStyle.Flat, ForeColor = ThemedShell.Muted, Cursor = Cursors.Hand };
            btnBack.FlatAppearance.BorderSize = 0;
            card.Controls.Add(btnBack);

            var status = new Label { Text = "", Font = new Font("Consolas", 8), ForeColor = ThemedShell.Muted, AutoSize = true, Location = new Point(40, 195) };
            card.Controls.Add(status);

            btnBack.Click += (s, e) =>
            {
                var c = Application.OpenForms.OfType<AuthChoiceForm>().FirstOrDefault() ?? new AuthChoiceForm();
                c.Show();
                this.Close();
            };

            btnGo.Click += (s, e) =>
            {
                try
                {
                    status.Text = "Checking...";
                    AuthSession.Api.license(tbKey.InnerText.Trim());
                    if (AuthSession.Api.response.success)
                    {
                        AuthSession.IsAuthed = true;
                        AuthSession.AuthMode = "license";
                        var m = Application.OpenForms.OfType<MainForm>().FirstOrDefault() ?? new MainForm();
                        m.Show();
                        m.BringToFront();
                        this.Close();
                    }
                    else
                    {
                        status.Text = "Status: " + AuthSession.Api.response.message;
                        MessageBox.Show("Status: " + AuthSession.Api.response.message);
                    }
                }
                catch (Exception ex)
                {
                    status.Text = "Activation failed";
                    MessageBox.Show("Activation failed: " + ex.Message);
                }
            };
        }
    }
}
