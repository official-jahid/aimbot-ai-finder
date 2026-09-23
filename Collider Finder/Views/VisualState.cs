using System;

namespace JahidColliderFinder
{
    public static class VisualState
    {
        public static bool ShowParticles = true;
        public static bool ShowGrid = true;
        public static bool GlowEnabled = true;
        public static bool ShowClock = true;

        public static event Action? Changed;

        public static void Notify() => Changed?.Invoke();
    }
}
