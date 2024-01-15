using System.Text;

namespace OpenTonTracker
{
    public static class Emojis
    {
        public static string GreenDot { get; } = new Rune(0x1F7E2).ToString();
        public static string RedDot { get; } = new Rune(0x1F534).ToString();
        public static string BarChart { get; } = new Rune(0x1F4CA).ToString();
        public static string UptrendChart { get; } = new Rune(0x1F4C8).ToString();
        public static string DowntrendChart { get; } = new Rune(0x1F4C9).ToString();
        public static string MoneyBag { get; } = new Rune(0x1F4B0).ToString();
    }
}