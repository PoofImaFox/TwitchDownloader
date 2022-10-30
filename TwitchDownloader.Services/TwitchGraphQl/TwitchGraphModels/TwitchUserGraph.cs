using Newtonsoft.Json;

namespace TwitchDownloader.Services.TwitchGraphQl.TwitchGraphModels {
    public class TwitchUserGraph {

        [JsonProperty("user")]
        public TwitchUser TwitchUser { get; set; }
    }

    public class TwitchUser {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("hasPrime")]
        public bool HasPrime { get; set; }

        [JsonProperty("hasTurbo")]
        public bool HasTurbo { get; set; }

        [JsonProperty("isEmailReusable")]
        public bool IsEmailReusable { get; set; }

        [JsonProperty("isEmailVerified")]
        public bool IsEmailVerified { get; set; }

        [JsonProperty("isFlaggedToDelete")]
        public bool IsFlaggedToDelete { get; set; }

        [JsonProperty("isSiteAdmin")]
        public bool IsSiteAdmin { get; set; }

        [JsonProperty("isStaff")]
        public bool IsStaff { get; set; }

        [JsonProperty("isGlobalMod")]
        public bool IsGlobalMod { get; set; }

        [JsonProperty("isPartner")]
        public bool IsPartner { get; set; }

        [JsonProperty("isPhoneNumberVerified")]
        public bool IsPhoneNumberVerified { get; set; }

        [JsonProperty("chatColor")]
        public string ChatColor { get; set; }

        [JsonProperty("primaryColorHex")]
        public string PrimaryColorHex { get; set; }

        [JsonProperty("profileURL")]
        public string ProfileURL { get; set; }

        [JsonProperty("profileViewCount")]
        public int ProfileViewCount { get; set; }

        [JsonProperty("bannerImageURL")]
        public string BannerImageURL { get; set; }
    }
}