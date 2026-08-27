namespace TarotUnity.Network
{
    public static class ApiRoutes
    {
        public const string Login = "/login";
        public const string GuestSession = "/guest-session";
        public const string UsersMe = "/users/me";
        public const string Refresh = "/refresh";
        public const string Logout = "/logout";
        public const string Spreads = "/spreads/";
        public const string Records = "/records/";

        public static string SpreadDetail(int spreadId)
        {
            return $"/spreads/{spreadId}";
        }

        public static string RecordDraw(int predictionId)
        {
            return $"/records/{predictionId}/draw";
        }

        public static string RecordCards(int predictionId)
        {
            return $"/records/{predictionId}/cards";
        }

        public static string RecordInterpret(int predictionId)
        {
            return $"/records/{predictionId}/interpret";
        }

        public static string RecordDetail(int predictionId)
        {
            return $"/records/{predictionId}";
        }
    }
}
