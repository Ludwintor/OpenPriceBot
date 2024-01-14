using Microsoft.Extensions.Logging;

namespace DedustNet.Logging
{
    public static class LoggerEvents
    {
        public static EventId Misc { get; } = new(100, "DedustNET");

        public static EventId RestRecv { get; } = new(101, nameof(RestRecv));

        public static EventId RestError { get; } = new(102, nameof(RestError));
    }
}
