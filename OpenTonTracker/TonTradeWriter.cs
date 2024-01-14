using System.Text;
using DedustNet.Api.Entities;

namespace OpenTonTracker
{
    public sealed class TonTradeWriter
    {
        private readonly string _assetSymbol;
        private readonly string _assetAddress;
        private readonly int _assetDecimals;
        private readonly string _tonExplorerDomain;

        public TonTradeWriter(string assetSymbol, string assetAddress, int assetDecimals, string tonExplorerDomain)
        {
            _assetSymbol = assetSymbol;
            _assetAddress = assetAddress;
            _assetDecimals = assetDecimals;
            _tonExplorerDomain = tonExplorerDomain;
        }

        public void WriteTrades(Trade[] trades, StringBuilder sb, Pool? tonUsdtPool)
        {
            foreach (Trade trade in trades)
            {
                bool isBuy = trade.AssetOut.Type == AssetType.Jetton && trade.AssetOut.Address == _assetAddress;
                Rune buySellEmoji = isBuy ? Emojis.GreenDot : Emojis.RedDot;
                string buySell = isBuy ? "BUY" : "SELL";
                double ton = (double)(isBuy ? trade.AmountIn : trade.AmountOut) / Math.Pow(10d, 9d);
                double jetton = (double)(isBuy ? trade.AmountOut : trade.AmountIn) / Math.Pow(10d, _assetDecimals);
                double usd = tonUsdtPool?.CalculateLeftToRight(ton) ?? 0d;

                // :RED_DOT:SELL 500 TOKEN for 5 TON ($2.50) EQAA...FOO_
                sb.Append(buySellEmoji.ToString()).Append(buySell).Append(' ')
                  .Append(Utils.EscapeMarkdown(jetton.ToString("0.00"))).Append(' ').Append(_assetSymbol).Append(" for ")
                  .Append(Utils.EscapeMarkdown(ton.ToString("0.00"))).Append(" TON ")
                  .Append("\\($").Append(Utils.EscapeMarkdown(usd.ToString("0.00"))).Append("\\) [")
                  .Append(Utils.ShortAddress(trade.Sender, true)).Append("](")
                  .Append(_tonExplorerDomain).Append(trade.Sender).Append(')').AppendLine();
            }
        }
    }
}