using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TarotUnity.Core;
using TarotUnity.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace TarotUnity.Network
{
    public sealed class ApiClient : MonoBehaviour
    {
        public const string CsrfHeaderName = "X-CSRF-Token";
        public const string CsrfCookieName = "csrf_token";

        [SerializeField] private string baseUrl = "http://localhost:8000/api/v1";
        [SerializeField] private int requestTimeoutSeconds = DesktopRuntimeConfig.DefaultRequestTimeoutSeconds;

        private string accessToken;
        private string cookieHeader;
        private string csrfToken;

        public string BaseUrl
        {
            get => baseUrl;
            set => baseUrl = value;
        }

        public bool HasAccessToken => !string.IsNullOrWhiteSpace(accessToken);
        public bool HasSession => HasAccessToken || !string.IsNullOrWhiteSpace(cookieHeader);
        public string AccessToken => accessToken ?? string.Empty;
        public string CookieHeader => cookieHeader ?? string.Empty;
        public string CsrfToken => csrfToken ?? string.Empty;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;

        public void ApplyRuntimeConfig(DesktopRuntimeConfig config)
        {
            if (config == null)
            {
                return;
            }

            baseUrl = config.BackendBaseUrl;
            requestTimeoutSeconds = config.RequestTimeoutSeconds;
        }

        public void SetAccessToken(string token)
        {
            accessToken = token;
        }

        public void ApplySession(ApiSessionState session)
        {
            accessToken = session?.accessToken;
            cookieHeader = session?.cookieHeader;
            csrfToken = session?.csrfToken;
        }

        public ApiSessionState ExportSession()
        {
            return new ApiSessionState
            {
                accessToken = accessToken,
                cookieHeader = cookieHeader,
                csrfToken = csrfToken,
            };
        }

        public void ClearSession()
        {
            accessToken = null;
            cookieHeader = null;
            csrfToken = null;
        }

        public IEnumerator Login(string username, string password, Action<TokenResponse> onSuccess, Action<string> onError)
        {
            var form = new WWWForm();
            form.AddField("username", username);
            form.AddField("password", password);

            using var request = UnityWebRequest.Post(BuildUrl(ApiRoutes.Login), form);
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError, token => accessToken = token.access_token);
        }

        public IEnumerator CreateGuestSession(Action<TokenResponse> onSuccess, Action<string> onError)
        {
            yield return PostEmpty(
                ApiRoutes.GuestSession,
                onSuccess,
                onError,
                token => accessToken = token?.access_token);
        }

        public IEnumerator GetCurrentUser(Action<UserProfile> onSuccess, Action<string> onError)
        {
            yield return Get(ApiRoutes.UsersMe, onSuccess, onError);
        }

        public IEnumerator Refresh(Action<TokenResponse> onSuccess, Action<string> onError)
        {
            yield return PostEmpty(ApiRoutes.Refresh, onSuccess, onError, token => accessToken = token.access_token);
        }

        public IEnumerator Logout(Action<ApiMessageResponse> onSuccess, Action<string> onError)
        {
            yield return PostEmpty(ApiRoutes.Logout, onSuccess, onError, _ => ClearSession());
        }

        public IEnumerator GetSpreads(Action<SpreadSummary[]> onSuccess, Action<string> onError)
        {
            yield return GetArray(ApiRoutes.Spreads, onSuccess, onError);
        }

        public IEnumerator GetSpread(int spreadId, Action<SpreadDetail> onSuccess, Action<string> onError)
        {
            yield return Get(ApiRoutes.SpreadDetail(spreadId), onSuccess, onError);
        }

        public IEnumerator CreateRecord(PredictionCreateRequest payload, Action<PredictionResponse> onSuccess, Action<string> onError)
        {
            yield return PostJson(ApiRoutes.Records, payload, onSuccess, onError);
        }

        public IEnumerator DrawCards(int predictionId, Action<DrawCardsResponse> onSuccess, Action<string> onError)
        {
            yield return PostEmpty(ApiRoutes.RecordDraw(predictionId), onSuccess, onError);
        }

        public IEnumerator GetRecordCards(int predictionId, Action<CardDrawData[]> onSuccess, Action<string> onError)
        {
            yield return GetArray(ApiRoutes.RecordCards(predictionId), onSuccess, onError);
        }

        public IEnumerator CreateInterpretation(int predictionId, Action<InterpretationResponse> onSuccess, Action<string> onError)
        {
            yield return PostEmpty(ApiRoutes.RecordInterpret(predictionId), onSuccess, onError);
        }

        public IEnumerator GetRecord(int predictionId, Action<PredictionDetailResponse> onSuccess, Action<string> onError)
        {
            yield return Get(ApiRoutes.RecordDetail(predictionId), onSuccess, onError);
        }

        private IEnumerator Get<TResponse>(string path, Action<TResponse> onSuccess, Action<string> onError)
        {
            using var request = UnityWebRequest.Get(BuildUrl(path));
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private IEnumerator GetArray<TItem>(string path, Action<TItem[]> onSuccess, Action<string> onError)
        {
            using var request = UnityWebRequest.Get(BuildUrl(path));
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleArrayResponse(request, onSuccess, onError);
        }

        private IEnumerator PostEmpty<TResponse>(string path, Action<TResponse> onSuccess, Action<string> onError, Action<TResponse> afterSuccess = null)
        {
            using var request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST);
            request.downloadHandler = new DownloadHandlerBuffer();
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError, afterSuccess);
        }

        private IEnumerator PostJson<TPayload, TResponse>(string path, TPayload payload, Action<TResponse> onSuccess, Action<string> onError)
        {
            var json = JsonUtility.ToJson(payload);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            PrepareRequest(request);
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private void PrepareRequest(UnityWebRequest request)
        {
            request.timeout = requestTimeoutSeconds;
            request.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            }

            if (!string.IsNullOrWhiteSpace(cookieHeader))
            {
                request.SetRequestHeader("Cookie", cookieHeader);
            }

            if (!string.IsNullOrWhiteSpace(csrfToken))
            {
                request.SetRequestHeader(CsrfHeaderName, csrfToken);
            }
        }

        private void HandleResponse<TResponse>(
            UnityWebRequest request,
            Action<TResponse> onSuccess,
            Action<string> onError,
            Action<TResponse> afterSuccess = null)
        {
            CaptureCookies(request);

            if (IsError(request))
            {
                onError?.Invoke(BuildError(request));
                return;
            }

            var text = request.downloadHandler?.text;
            var response = string.IsNullOrWhiteSpace(text)
                ? default
                : JsonUtility.FromJson<TResponse>(text);

            afterSuccess?.Invoke(response);
            onSuccess?.Invoke(response);
        }

        private void HandleArrayResponse<TItem>(UnityWebRequest request, Action<TItem[]> onSuccess, Action<string> onError)
        {
            CaptureCookies(request);

            if (IsError(request))
            {
                onError?.Invoke(BuildError(request));
                return;
            }

            var text = request.downloadHandler?.text;
            var wrapper = JsonUtility.FromJson<ArrayWrapper<TItem>>($"{{\"items\":{text}}}");
            onSuccess?.Invoke(wrapper?.items ?? Array.Empty<TItem>());
        }

        public string BuildUrl(string path)
        {
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        public void StoreCookieHeaderForTesting(string setCookieHeader)
        {
            StoreCookieHeader(setCookieHeader);
        }

        private static bool IsError(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError
                || request.result == UnityWebRequest.Result.DataProcessingError;
        }

        private static string BuildError(UnityWebRequest request)
        {
            var body = request.downloadHandler?.text;
            return string.IsNullOrWhiteSpace(body)
                ? $"{request.responseCode}: {request.error}"
                : $"{request.responseCode}: {body}";
        }

        private void CaptureCookies(UnityWebRequest request)
        {
            var headers = request.GetResponseHeaders();
            if (headers == null)
            {
                return;
            }

            foreach (var header in headers)
            {
                if (!string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                StoreCookieHeader(header.Value);
            }
        }

        private void StoreCookieHeader(string setCookieHeader)
        {
            if (string.IsNullOrWhiteSpace(setCookieHeader))
            {
                return;
            }

            var cookies = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(cookieHeader))
            {
                foreach (var existing in cookieHeader.Split(';'))
                {
                    var trimmed = existing.Trim();
                    var equals = trimmed.IndexOf('=');
                    if (equals > 0)
                    {
                        cookies[trimmed[..equals]] = trimmed[(equals + 1)..];
                    }
                }
            }

            foreach (var rawPart in setCookieHeader.Split(','))
            {
                var firstSegment = rawPart.Split(';')[0].Trim();
                var equals = firstSegment.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                var name = firstSegment[..equals];
                var value = firstSegment[(equals + 1)..];
                cookies[name] = value;

                if (name == CsrfCookieName)
                {
                    csrfToken = value;
                }
            }

            var pairs = new List<string>();
            foreach (var cookie in cookies)
            {
                pairs.Add($"{cookie.Key}={cookie.Value}");
            }

            cookieHeader = string.Join("; ", pairs);
        }

        [Serializable]
        private sealed class ArrayWrapper<T>
        {
            public T[] items;
        }
    }

    [Serializable]
    public sealed class ApiMessageResponse
    {
        public string message;
    }
}
