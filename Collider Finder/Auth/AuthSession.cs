using System;

namespace JahidColliderFinder.Auth
{
    public static class AuthSession
    {
        public static LicenseAuth.api Api = new LicenseAuth.api(
            name: "regix colloidor finder",
            ownerid: "RTgStl6UQK",
            secret: "9f9cf8a724ed8c50959fbb8ad4fe6621b13456109fb29b4d2a082b54143d64a2",
            version: "1.0"
        );

        public static bool IsAuthed = false;
        public static string AuthMode = "";

        public static string SafeUsername()
        {
            try { return Api?.user_data?.username ?? ""; }
            catch { return ""; }
        }

        public static string ExpiryText()
        {
            try
            {
                var subs = Api?.user_data?.subscriptions;
                if (subs == null || subs.Count == 0) return "-";
                long exp = long.Parse(subs[0].expiry);
                var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).AddSeconds(exp).ToLocalTime();
                return dt.ToString("yyyy-MM-dd HH:mm");
            }
            catch { return "-"; }
        }

        public static string TimeLeftText()
        {
            try
            {
                var subs = Api?.user_data?.subscriptions;
                if (subs == null || subs.Count == 0) return "-";
                long exp = long.Parse(subs[0].expiry);
                var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).AddSeconds(exp).ToLocalTime();
                var diff = dt - DateTime.Now;
                if (diff.TotalSeconds <= 0) return "Expired";
                return diff.Days + " Days " + diff.Hours + " Hours Left";
            }
            catch { return "-"; }
        }

        public static string SubscriptionText()
        {
            try
            {
                var subs = Api?.user_data?.subscriptions;
                if (subs == null || subs.Count == 0) return "-";
                return subs[0].subscription ?? "-";
            }
            catch { return "-"; }
        }
    }
}
