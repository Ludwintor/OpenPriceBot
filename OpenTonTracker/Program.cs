using System.Globalization;
using System.Text;
using DedustNet.Api;
using DedustNet.Api.Entities;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace OpenTonTracker
{
    public static class Program
    {
        private const string BOT_TOKEN_ENV = "TG_BOT_TOKEN";
        private const string OPENTON_POOL = "EQClitEiuIqbEs7QX06Bo75E6nx9C6h4VYS1TDxh2dAYtKpQ";
        private const string USDTTON_POOL = "EQCk6tGPlFoQ_1TgZJjuiulfSJz5aoJgnyy29eLsXtOmeYDw";
        private const string OPEN_ADDRESS = "EQDf84FT8tdHZeI2-LXdb8gPMRqHRSABrmi8jI7MzvVpGJKZ";
        private const string TONVIEWER = "https://tonviewer.com/";
        private const int MS_DELAY = 10000;
        private const int RETRY_DELAY = 120;
        private const int PRICE_CHANGE_RETRIES = 4;

        private static readonly EventId _botEvent = new(420, "OpenPrice");
#if !DEBUG
        private static readonly ChatId _channelPriceId = new(-1002056517262);
#else
        private static readonly ChatId _channelPriceId = new(-1002070106680); // test channel
#endif
        private static readonly InlineKeyboardMarkup _urls;

        private static TelegramBotClient _botClient = null!;
        private static readonly DedustClient _dedustClient = new(LogLevel.Debug);
        private static double _lastTonPrice;

        static Program()
        {
            _urls = new(new[]
            {
                InlineKeyboardButton.WithUrl($"{Emojis.MoneyBag}Buy on Dedust", "https://dedust.io/swap/TON/OPEN"),
                InlineKeyboardButton.WithUrl($"{Emojis.BarChart}Chart", "https://dyor.io/ru/token/EQDf84FT8tdHZeI2-LXdb8gPMRqHRSABrmi8jI7MzvVpGJKZ")
            });
        }

        private static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            string? token;
            if (args.Length > 0)
                token = args[0];
            else
                token = Environment.GetEnvironmentVariable(BOT_TOKEN_ENV);
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), $"Set telegram bot token to environment variable \"{BOT_TOKEN_ENV}\" or provide it as first argument (argument in high priority)");
            _botClient = new(token);
            Run().GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            StringBuilder sb = new();
            TonTradeWriter tradeWriter = new("OPEN", OPEN_ADDRESS, 5, TONVIEWER);
            UInt128? lastLt = null;
            do
            {
                try
                {
                    Trade[] trades = await _dedustClient.FetchPoolTradesAsync(OPENTON_POOL, 1);
                    Pool[] pools = await _dedustClient.FetchPoolsAsync();
                    Pool openTonPool = pools.Where(x => x.Address == OPENTON_POOL).First();
                    _lastTonPrice = openTonPool.CalculateRightToLeft(1d, null, 5);
                    lastLt = trades[0].Lt;
                }
                catch
                {
                    _dedustClient.Logger.LogError(_botEvent, "Unable to fetch first trade. Retrying...");
                }
            } while (lastLt == null);
            await Task.Delay(4000);
            while (true)
            {
                sb.Clear();
                Trade[] trades = null!;
                Pool tonUsdtPool = default;
                Pool openTonPool = default;
                try
                {
                    trades = await _dedustClient.FetchPoolTradesAsync(OPENTON_POOL, 5, lastLt);
                    if (trades.Length > 0)
                    {
                        await Task.Delay(250);
                        Pool[] pools = await _dedustClient.FetchPoolsAsync();
                        tonUsdtPool = pools.Where(x => x.Address == USDTTON_POOL).First();
                        openTonPool = pools.Where(x => x.Address == OPENTON_POOL).First();
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    _dedustClient.Logger.LogError(_botEvent, ex, "Unable to fetch last trades. Retrying after {Seconds} seconds", MS_DELAY / 1000);
                    await Task.Delay(MS_DELAY);
                    continue;
                }
                catch (InvalidOperationException ex)
                {
                    _dedustClient.Logger.LogError(_botEvent, ex, "Unable to find TON/jUSDT pool or TON/OPEN pool. Retrying after {Seconds} seconds", MS_DELAY / 1000);
                    await Task.Delay(MS_DELAY);
                    continue;
                }
                if (trades.Length > 0)
                {
                    tradeWriter.WriteTrades(trades, sb, tonUsdtPool);
                    double tonPrice = await RetrievePrice(openTonPool);
                    double usdtPrice = tonUsdtPool.CalculateLeftToRight(tonPrice);
                    double priceChange = tonPrice / _lastTonPrice - 1d;
                    bool changeUp = priceChange >= 0d;
                    string changeEmoji = changeUp ? Emojis.UptrendChart : Emojis.DowntrendChart;
                    sb.AppendLine().Append(Emojis.BarChart.ToString()).Append("Price: ")
                        .Append(Utils.EscapeMarkdown(tonPrice.ToString("0.000000")))
                        .Append(" TON \\($")
                        .Append(Utils.EscapeMarkdown(usdtPrice.ToString("0.000000")))
                        .Append("\\) ").Append(changeEmoji).Append(" \\")
                        .Append(changeUp ? '+' : '-').Append(Utils.EscapeMarkdown(Math.Abs(priceChange * 100d).ToString("0.00"))).Append('%');
                    _lastTonPrice = tonPrice;
                    lastLt = trades[^1].Lt;
                    await _botClient.SendTextMessageAsync(_channelPriceId, sb.ToString(), disableWebPagePreview: true, parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2, replyMarkup: _urls);
                }
                await Task.Delay(MS_DELAY);
            }
        }

        private static async ValueTask<double> RetrievePrice(Pool tokenTonPool)
        {
            double tonPrice = tokenTonPool.CalculateRightToLeft(1d, null, 5);
            int retries = 0;
            while (tonPrice == _lastTonPrice && retries < PRICE_CHANGE_RETRIES)
            {
                await Task.Delay(RETRY_DELAY);
                try
                {
                    Pool[] pools = await _dedustClient.FetchPoolsAsync();
                    tokenTonPool = pools.Where(x => x.Address == OPENTON_POOL).First();
                }
                catch (Exception ex)
                {
                    _dedustClient.Logger.LogError(_botEvent, ex, "Unable to retrieve pools durting price retrieve retry. Retrying after {Seconds} seconds", (RETRY_DELAY / 1000d).ToString("0.00"));
                    continue;
                }
                tonPrice = tokenTonPool.CalculateRightToLeft(1d, null, 5);
                retries++;
            }
            return tonPrice;
        }
    }
}