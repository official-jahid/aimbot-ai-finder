using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FontAwesome.Sharp;
using JahidColliderFinder.Auth;

namespace JahidColliderFinder.Views
{
    public static class ThemedShell
    {
        public static readonly Color BG = Color.FromArgb(11, 12, 16);
        public static readonly Color Inner = Color.FromArgb(18, 20, 29);
        public static readonly Color Card = Color.FromArgb(24, 26, 38);
        public static readonly Color Border = Color.FromArgb(35, 39, 54);
        public static readonly Color White = Color.FromArgb(255, 255, 255);
        public static readonly Color Muted = Color.FromArgb(90, 98, 117);
        public static readonly Color Dim = Color.FromArgb(59, 66, 82);
        public static readonly Color Accent = Color.FromArgb(138, 148, 248);

        public static void StyleForm(Form f, string title, int w = 1020, int h = 540)
        {
            f.Text = title;
            f.Size = new Size(w, h);
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = BG;
            f.StartPosition = FormStartPosition.CenterScreen;
        }

        public static Panel BuildSidebar(Form host, int activeIndex, Action<int> onNav)
        {
            var side = new Panel
            {
                Bounds = new Rectangle(0, 42, 64, host.Height - 42 - 30),
                BackColor = Inner
            };
            side.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1f);
                e.Graphics.DrawLine(pen, 63, 0, 63, side.Height);
            };

            var items = new (IconChar icon, string tip)[]
            {
                (IconChar.Crosshairs, "Collider Finder"),
                (IconChar.SlidersH, "Visual Settings"),
                (IconChar.UserCircle, "Profile"),
                (IconChar.SignOutAlt, "Logout"),
            };

            var tips = new ToolTip
            {
                AutoPopDelay = 3000,
                InitialDelay = 400,
                ReshowDelay = 200,
                ShowAlways = true
            };

            for (int i = 0; i < items.Length; i++)
            {
                int idx = i;
                var b = new IconButton
                {
                    IconChar = items[i].icon,
                    IconColor = i == activeIndex ? White : Muted,
                    IconSize = 22,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = i == activeIndex ? Card : Color.Transparent,
                    ForeColor = Color.White,
                    Size = new Size(48, 48),
                    Location = new Point(8, 18 + i * 62),
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                b.FlatAppearance.BorderSize = i == activeIndex ? 1 : 0;
                b.FlatAppearance.BorderColor = Accent;
                b.FlatAppearance.MouseOverBackColor = Card;
                b.FlatAppearance.MouseDownBackColor = Card;
                tips.SetToolTip(b, items[i].tip);
                b.Click += (s, e) => onNav(idx);
                side.Controls.Add(b);
            }

            host.Controls.Add(side);
            side.BringToFront();
            return side;
        }

        public static void NavigateFrom(Form current, int index)
        {
            if (index == 0)
            {
                if (current is MainForm) return;
                var f = OpenForms().OfType<MainForm>().FirstOrDefault() ?? new MainForm();
                f.Show();
                current.Hide();
            }
            else if (index == 1)
            {
                var f = OpenForms().OfType<VisualSettingsForm>().FirstOrDefault() ?? new VisualSettingsForm();
                f.Show();
                if (!(current is VisualSettingsForm)) current.Hide();
            }
            else if (index == 2)
            {
                var f = OpenForms().OfType<ProfileForm>().FirstOrDefault() ?? new ProfileForm();
                f.Show();
                if (!(current is ProfileForm)) current.Hide();
            }
            else
            {
                try { AuthSession.Api.logout(); } catch { }
                AuthSession.IsAuthed = false;
                foreach (var f in OpenForms().OfType<Form>().ToList())
                {
                    if (f is AuthChoiceForm || f is UserPassLoginForm || f is LicenseLoginForm) continue;
                    f.Hide();
                }
                var choice = OpenForms().OfType<AuthChoiceForm>().FirstOrDefault();
                if (choice == null) { choice = new AuthChoiceForm(); choice.Show(); }
                else { choice.Show(); }
                current.Hide();
            }
        }

        private static FormCollection OpenForms() => Application.OpenForms;
    }
}
