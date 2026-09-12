using System;
using System.Globalization;

namespace TarotUnity.Network
{
    public enum ApiErrorKind
    {
        Network,
        Timeout,
        Unauthorized,
        NotFound,
        BadRequest,
        RateLimited,
        Server,
        Unexpected,
    }

    // Phase 66: a structured request failure for the online interpretation flow.
    // Kind drives the player-facing ReleaseUxCopy line; RawMessage is only ever
    // written to the log, never shown to the player.
    public sealed class ApiError
    {
        public ApiError(long statusCode, ApiErrorKind kind, int retryAfterSeconds, string rawMessage)
        {
            StatusCode = statusCode;
            Kind = kind;
            RetryAfterSeconds = retryAfterSeconds;
            RawMessage = rawMessage ?? string.Empty;
        }

        public long StatusCode { get; }
        public ApiErrorKind Kind { get; }
        public int RetryAfterSeconds { get; }
        public string RawMessage { get; }

        public bool IsTransient =>
            Kind == ApiErrorKind.Network || Kind == ApiErrorKind.Timeout || Kind == ApiErrorKind.Server;

        public static ApiError FromResponse(long statusCode, string requestError, string body, string retryAfterHeader)
        {
            var detail = string.IsNullOrWhiteSpace(body) ? requestError ?? string.Empty : body;
            return new ApiError(
                statusCode,
                Classify(statusCode, requestError),
                ParseRetryAfter(retryAfterHeader),
                $"{statusCode}: {detail}");
        }

        public static ApiError Local(string message)
        {
            return new ApiError(0, ApiErrorKind.Network, -1, message);
        }

        public static ApiErrorKind Classify(long statusCode, string requestError)
        {
            if (statusCode == 0)
            {
                return !string.IsNullOrEmpty(requestError)
                    && requestError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                        ? ApiErrorKind.Timeout
                        : ApiErrorKind.Network;
            }

            if (statusCode == 401)
            {
                return ApiErrorKind.Unauthorized;
            }

            if (statusCode == 404)
            {
                return ApiErrorKind.NotFound;
            }

            if (statusCode == 400 || statusCode == 422)
            {
                return ApiErrorKind.BadRequest;
            }

            if (statusCode == 429)
            {
                return ApiErrorKind.RateLimited;
            }

            return statusCode >= 500 ? ApiErrorKind.Server : ApiErrorKind.Unexpected;
        }

        public static int ParseRetryAfter(string header)
        {
            return int.TryParse(header?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
                && seconds >= 0
                    ? seconds
                    : -1;
        }
    }
}
