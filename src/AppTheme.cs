using System;
using System.Drawing;

namespace WiiAnimeEncoder
{
    public sealed class AppTheme
    {
        public readonly string Name;
        public readonly Color Background, Surface, Accent;
        public string ArtworkFile { get { return "themes/" + Name.ToLowerInvariant().Replace(" ", "-") + ".jpg"; } }
        AppTheme(string name, int background, int accent)
        {
            Name = name;
            Background = FromHex(background);
            Surface = Mix(Background, Color.White, 0.06);
            Accent = FromHex(accent);
        }
        static Color FromHex(int value) { return Color.FromArgb((value >> 16) & 255, (value >> 8) & 255, value & 255); }
        public static Color Mix(Color a, Color b, double amount)
        {
            return Color.FromArgb((int)(a.R + (b.R-a.R)*amount), (int)(a.G + (b.G-a.G)*amount), (int)(a.B + (b.B-a.B)*amount));
        }
        static double Linear(byte value)
        {
            double channel = value / 255.0;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }
        public static Color ForWhiteText(Color color)
        {
            // Keep white button labels above 4.5:1 contrast, including hover.
            while (0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B) > 0.18)
                color = Mix(color, Color.Black, 0.08);
            return color;
        }
        public static readonly AppTheme[] All = {
            new AppTheme("Midnight", 0x12151C, 0x7185B5),
            new AppTheme("Graphite", 0x191A1C, 0x999DA5),
            new AppTheme("Slate", 0x171E25, 0x7B9BAE),
            new AppTheme("OLED Black", 0x000000, 0xB4BAC8),
            new AppTheme("Nord", 0x242B37, 0x88C0D0),
            new AppTheme("Ocean", 0x101D29, 0x39A7D0),
            new AppTheme("Forest", 0x142019, 0x70B58B),
            new AppTheme("Plum", 0x221828, 0xB28CD5),
            new AppTheme("Amber", 0x211C13, 0xD7A052),
            new AppTheme("Crimson", 0x190307, 0xC8102E),
            new AppTheme("Scarlet", 0x1B0406, 0xE3242B)
        };
        public static AppTheme Find(string name)
        {
            foreach (var item in All) if (String.Equals(item.Name, name, StringComparison.Ordinal)) return item;
            return All[0];
        }
        public override string ToString() { return Name; }
    }
}
