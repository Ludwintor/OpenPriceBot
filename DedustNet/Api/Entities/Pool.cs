using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Newtonsoft.Json;

namespace DedustNet.Api.Entities
{
    public struct Pool
    {
        [JsonProperty("assets")]
        private readonly Asset[] _assets = null!;

        [JsonProperty("reserves")]
        private readonly UInt128[] _reserves = null!;

        [JsonProperty("tradeFee")]
        private readonly double _tradeFee = 0f;

        public Pool() { }

        /// <summary>
        /// Get liquidity pool address
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; private set; } = string.Empty;

        [JsonProperty("lt")]
        public UInt128 Lt { get; private set; }

        /// <summary>
        /// Get total supply of LP tokens
        /// </summary>
        [JsonProperty("totalSupply")]
        public UInt128 TotalSupply { get; private set; }

        /// <summary>
        /// Get pool type
        /// </summary>
        [JsonProperty("type")]
        public PoolType Type { get; private set; } = PoolType.Unknown;

        /// <summary>
        /// Get last trade price.
        /// </summary>
        /// <remarks>
        /// ATTENTION: Can be inversed or not, it depends on last trade direction within this pool. For "fixed-direction" prices use <see cref="PricePerLeft"/> and <see cref="PricePerRight"/> properties
        /// </remarks>
        [JsonProperty("lastPrice")]
        public double? LastPrice { get; private set; }

        /// <summary>
        /// Get pool stats for last 24 hours (fees and volume)
        /// </summary>
        [JsonProperty("stats")]
        public PoolStats Stats { get; private set; }

        /// <summary>
        /// Get trade fee as normalized percentage [0, 1]
        /// </summary>
        [JsonIgnore]
        public double TradeFee => _tradeFee / 100d; // cuz Dedust API returns trade fee from 0 to 100

        /// <summary>
        /// Get left token in the pool
        /// </summary>
        [JsonIgnore]
        public Asset Left => _assets[0];

        /// <summary>
        /// Get right token in the pool
        /// </summary>
        [JsonIgnore]
        public Asset Right => _assets[1];

        /// <summary>
        /// Get total amount of left token in the pool
        /// </summary>
        [JsonIgnore]
        public UInt128 LeftReserve => _reserves[0];

        /// <summary>
        /// Get total amount of right token in the pool
        /// </summary>
        [JsonIgnore]
        public UInt128 RightReserve => _reserves[1];

        /// <summary>
        /// Get the amount of right tokens per 1 left token (without trade fee)
        /// </summary>
        [JsonIgnore]
        public double PricePerLeft => Type == PoolType.Stable ? CalculateStablePriceLeft(1d) : CalculateVolatilePriceLeft(1d);

        /// <summary>
        /// Get the amount of left tokens per 1 right token (without trade fee)
        /// </summary>
        [JsonIgnore]
        public double PricePerRight => Type == PoolType.Stable ? CalculateStablePriceRight(1d) : CalculateVolatilePriceRight(1d);

        /// <summary>
        /// Calculate amount of right assets you receive for swapping <paramref name="leftAmount"/> in the pool
        /// </summary>
        /// <param name="leftAmount">Left amount to swap</param>
        /// <returns>Amount of right assets to receive</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="leftAmount"/> less or equals to zero</exception>
        public double CalculateLeftToRight(double leftAmount)
        {
            if (leftAmount <= 0)
                throw new InvalidOperationException($"{nameof(leftAmount)} must be greater than zero");
            return Type == PoolType.Stable ? CalculateStablePriceLeft(leftAmount) : CalculateVolatilePriceLeft(leftAmount);
        }

        /// <summary>
        /// Calculate amount of left assets you receive for swapping <paramref name="rightAmount"/> in the pool
        /// </summary>
        /// <param name="rightAmount">Right amount to swap</param>
        /// <returns>Amount of left assets to receive</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="rightAmount"/> less or equals to zero</exception>
        public double CalculateRightToLeft(double rightAmount, int? overrideLeftDecimals = null, int? overrideRightDecimals = null)
        {
            if (rightAmount <= 0)
                throw new InvalidOperationException($"{nameof(rightAmount)} must be greater than zero");
            return Type == PoolType.Stable ? CalculateStablePriceRight(rightAmount) : CalculateVolatilePriceRight(rightAmount, overrideLeftDecimals, overrideRightDecimals);
        }

        // Volatile constant product is x • y = k
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double CalculateVolatilePriceLeft(double leftAmount)
        {
            if (LeftReserve == UInt128.Zero || RightReserve == UInt128.Zero)
                return 0d;
            double leftReserve = (double)LeftReserve / Math.Pow(10, Left.Decimals);
            double rightReserve = (double)RightReserve / Math.Pow(10, Right.Decimals);
            double constantProduct = leftReserve * rightReserve;
            return rightReserve - constantProduct / (leftReserve + leftAmount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double CalculateVolatilePriceRight(double rightAmount, int? overrideLeftDecimals = null, int? overrideRightDecimals = null)
        {
            if (LeftReserve == UInt128.Zero || RightReserve == UInt128.Zero)
                return 0d;
            double leftReserve = (double)LeftReserve / Math.Pow(10, overrideLeftDecimals != null ? (double)overrideLeftDecimals : Left.Decimals);
            double rightReserve = (double)RightReserve / Math.Pow(10, overrideRightDecimals != null ? (double)overrideRightDecimals : Right.Decimals);
            double constantProduct = leftReserve * rightReserve;
            return leftReserve - constantProduct / (rightReserve + rightAmount);
        }

        // Stable constant product is x^3 • y + y^3 • x = k
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double CalculateStablePriceLeft(double leftAmount)
        {
            if (LeftReserve == UInt128.Zero || RightReserve == UInt128.Zero)
                return 0d;
            double leftReserve = (double)LeftReserve / Math.Pow(10, Left.Decimals);
            double rightReserve = (double)RightReserve / Math.Pow(10, Right.Decimals);
            double constantProduct = leftReserve * leftReserve * leftReserve * rightReserve + rightReserve * rightReserve * rightReserve * leftReserve;
            // represent this as cubic equation to find new y. x = leftReserve + amount
            // so formula look like `x • y^3 + x^3 • y - constantProduct = 0` and now solve it
            double newLReserve = leftReserve + leftAmount;
            double newRReserve = CalculateNewStableReserve(newLReserve, newLReserve * newLReserve * newLReserve, -constantProduct);
            return rightReserve - newRReserve;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double CalculateStablePriceRight(double rightAmount)
        {
            if (LeftReserve == UInt128.Zero || RightReserve == UInt128.Zero)
                return 0d;
            double leftReserve = (double)LeftReserve / Math.Pow(10, Left.Decimals);
            double rightReserve = (double)RightReserve / Math.Pow(10, Right.Decimals);
            double constantProduct = leftReserve * leftReserve * leftReserve * rightReserve + rightReserve * rightReserve * rightReserve * leftReserve;
            // represent this as cubic equation to find new y. x = leftReserve + amount
            // so formula look like `x • y^3 + x^3 • y - constantProduct = 0` and now solve it
            double newRReserve = rightReserve + rightAmount;
            double newLReserve = CalculateNewStableReserve(newRReserve, newRReserve * newRReserve * newRReserve, -constantProduct);
            return leftReserve - newLReserve;
        }

        // simplified cubic equation solver where b = 0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double CalculateNewStableReserve(double a, double c, double d)
        {
            c /= a;
            d /= a;
            double h = Math.Sqrt(d * d / 4d + c * c * c / 27d);

            double r = (-d / 2) + h;
            double s = r >= 0 ? Math.Pow(r, 1 / 3d) : -Math.Pow(-r, 1d / 3d);

            double t = (-d / 2) - h;
            double u = t >= 0 ? Math.Pow(t, 1d / 3d) : -Math.Pow(-t, 1 / 3d);

            return s + u;
        }
    }
}