using System;

internal static class Compat
{
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static float Sqrt(float value)
    {
        return (float)Math.Sqrt(value);
    }
}