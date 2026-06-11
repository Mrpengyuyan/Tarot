using System;

namespace TarotUnity.Network
{
    [Serializable]
    public sealed class ApiSessionState
    {
        public string accessToken;
        public string cookieHeader;
        public string csrfToken;

        public bool HasSession =>
            !string.IsNullOrWhiteSpace(accessToken)
            || !string.IsNullOrWhiteSpace(cookieHeader);
    }
}
