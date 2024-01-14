using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DedustNet.Api.Entities;
using DedustNet.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DedustNet.Api
{
    public sealed class DedustClient : IDisposable
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        /// <summary>
        /// Initialize client with default console logger and default timeout (100 seconds)
        /// </summary>
        /// <remarks>
        /// NOTE: Minimum log level will be determined by build config (Debug level for debug config, Information level for release config)
        /// </remarks>
        public DedustClient() : this(null, null) { }

        /// <summary>
        /// Initialize client with default console logger and provided timeout
        /// </summary>
        /// <param name="minimumLevel">Minimum log level</param>
        /// <param name="timeout">Timeout for each request (default 100 seconds)</param>
        public DedustClient(LogLevel minimumLevel, TimeSpan? timeout = null) : this(new ConsoleLogger(minimumLevel), timeout) { }

        /// <summary>
        /// Initialize client with custom timeout and (optionally) custom logger
        /// </summary>
        /// <param name="logger">Custom logger. If null, use default console logger</param>
        /// <param name="timeout">Timeout for each request (default 100 seconds)</param>
        public DedustClient(ILogger? logger, TimeSpan? timeout = null)
        {
            _client = new()
            {
                BaseAddress = new(Endpoints.BASE_URL),
                Timeout = timeout ?? TimeSpan.FromSeconds(100)
            };
            if (logger == null)
            {
#if DEBUG
                logger = new ConsoleLogger(LogLevel.Debug);
#else
                logger = new ConsoleLogger(LogLevel.Information);
#endif
            }
            _logger = logger;
        }

        /// <summary>
        /// Gets current logger
        /// </summary>
        public ILogger Logger => _logger;

        /// <summary>
        /// Fetch available pools and their info from Dedust
        /// </summary>
        /// <returns>All fetched available pools</returns>
        /// <exception cref="HttpRequestException">Thrown if REST request is not OK or errored</exception>
        /// <exception cref="TaskCanceledException">Thrown if REST request timed out</exception>
        public async Task<Pool[]> FetchPoolsAsync()
        {
            string content = await ExecuteRequest(Endpoints.POOLS);
            Pool[] pools = JsonConvert.DeserializeObject<Pool[]>(content)!;
            _logger.LogDebug(LoggerEvents.Misc, "{Length} pools fetched successfully", pools.Length);
            return pools;
        }

        /// <summary>
        /// Fetch pool's latest rades
        /// </summary>
        /// <param name="poolAddress">Address of a pool</param>
        /// <param name="count">Number of trades to fetch. If no value provided, returns 50 trades (default value for Dedust API). May return less trades if there's no sufficient trades after <paramref name="afterLt"/></param>
        /// <param name="afterLt">Returns <paramref name="count"/> of trades after provided <paramref name="afterLt"/>. If no value provided, returns latest <paramref name="count"/> trades</param>
        /// <returns>Fetched trades in a pool</returns>
        /// <exception cref="HttpRequestException">Thrown if REST request is not OK or errored</exception>
        /// <exception cref="TaskCanceledException">Thrown if REST request timed out</exception>
        public async Task<Trade[]> FetchPoolTradesAsync(string poolAddress, int? count = null, UInt128? afterLt = null)
        {
            UrlQueryBuilder builder = new($"{Endpoints.POOLS}/{Uri.EscapeDataString(poolAddress)}/{Endpoints.TRADES}");
            if (count != null)
                builder.AddParameter("page_size", count.Value.ToString());
            if (afterLt != null)
                builder.AddParameter("after_lt", afterLt.Value.ToString());
            string content = await ExecuteRequest(builder.Build());
            Trade[] trades = JsonConvert.DeserializeObject<Trade[]>(content)!;
            _logger.LogDebug(LoggerEvents.Misc, "{Length} trades from pool {Address} fetched successfully", trades.Length, poolAddress);
            return trades;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private async Task<string> ExecuteRequest(string url)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();

                _logger.LogTrace(LoggerEvents.RestRecv, "{Content}", content);

                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpRequestException($"Bad status code: {statusCode}", null, statusCode);
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(LoggerEvents.RestError, ex, "Request to {Url} failed", $"{_client.BaseAddress}/{url}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(LoggerEvents.RestError, ex, "Request to {Url} timed out", $"{_client.BaseAddress}/{url}");
                throw;
            }
        }
    }
}
