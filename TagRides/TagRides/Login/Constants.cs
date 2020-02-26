using System;
namespace TagRides.Login
{
    public static class Constants
    {
        public static string AppName = "OAuthNativeFlow";

        // OAuth
        // For Google login, configure at https://console.developers.google.com/
        // Found under APIs -> Credentials
        public static string iOSClientId = "";
        public static string AndroidClientId = "";

        // These values do not need changing
        public static string Scope = "https://www.googleapis.com/auth/userinfo.email";
        public static string AuthorizeUrl = "https://accounts.google.com/o/oauth2/auth";
        public static string AccessTokenUrl = "https://www.googleapis.com/oauth2/v4/token";
        public static string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        // Set these to reversed iOS/Android client ids, with :/oauth2redirect appended
        public static string iOSRedirectUrl = "com.googleusercontent.apps.448465685942-ut1tdecpea5shjpt3s8d4c07rdigieva:/oauth2redirect";
        public static string AndroidRedirectUrl = "com.googleusercontent.apps.448465685942-lnurea6j3f4l10vtj8rtnv5nincctp5k:/oauth2redirect";
    }
}
