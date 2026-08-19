using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;

namespace FastGame
{
    /// <summary>
    /// Provider-opaque advertisement request + event tracking.
    /// </summary>
    public sealed class FastGameAds
    {
        readonly FastGameHttp _http;

        public FastGameAds(FastGameHttp http)
        {
            _http = http;
        }

        /// <summary>
        /// Request an ad. Returns null when the server has no fill (HTTP 204).
        /// </summary>
        public async Task<Advertisement> GetAdvertisementAsync(AdvertisementRequest request)
        {
            if (request == null)
                throw new FastGameException("Advertisement request is required");
            var body = new Dictionary<string, object>
            {
                { "game_id", request.GameId ?? "" },
                { "slot", request.Slot },
                { "media_type", request.MediaType },
                { "format", request.Format },
                { "tags", request.Tags ?? new List<string>() },
                { "locale", request.Locale },
                { "country", request.Country },
                { "platform", request.Platform },
                { "engine", request.Engine },
                { "capabilities", request.Capabilities ?? new Dictionary<string, object>() },
            };
            var text = await _http.RequestRawAsync(
                "POST", "/apps/games/ads/request", FastGameJson.Stringify(body));
            if (string.IsNullOrWhiteSpace(text))
                return null;
            return FastGameDto.ParseAdvertisement(FastGameJson.ParseObject(text));
        }

        public async Task TrackEventAsync(AdvertisementEvent evt)
        {
            if (evt == null)
                throw new FastGameException("Advertisement event is required");
            var body = new Dictionary<string, object>
            {
                { "event_type", evt.EventType ?? "" },
                { "ad_id", evt.AdId },
                { "game_id", evt.GameId },
                { "campaign_id", evt.CampaignId },
                { "extras", evt.Extras ?? new Dictionary<string, object>() },
            };
            if (!string.IsNullOrEmpty(evt.Timestamp))
                body["timestamp"] = evt.Timestamp;
            await _http.RequestRawAsync(
                "POST", "/apps/games/ads/events", FastGameJson.Stringify(body));
        }
    }
}
