using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;

namespace FastGame
{
    /// <summary>
    /// Designer Realtime.JoinMap — mint seat, then sibling Colyseus join with seat_token.
    /// Fast Game does not wrap Colyseus join/send/leave.
    /// </summary>
    public sealed class FastGameRealtime
    {
        readonly FastGameHttp _http;

        public FastGameRealtime(FastGameHttp http)
        {
            _http = http;
        }

        /// <summary>
        /// POST /apps/games/realtime/seat — short-lived one-time JoinMap ticket.
        /// Prefer over <see cref="FastGameCatalog.GetGameServerAsync"/> for online join.
        /// </summary>
        public async Task<SeatMintResult> MintSeatAsync(
            string gameCode,
            string mapId,
            string modeId = null)
        {
            var body = new Dictionary<string, object>
            {
                { "game_code", gameCode ?? "" },
                { "map_id", mapId ?? "" },
            };
            if (!string.IsNullOrEmpty(modeId))
                body["mode_id"] = modeId;

            var text = await _http.RequestRawAsync(
                "POST",
                "/apps/games/realtime/seat",
                FastGameJson.Stringify(body));
            return ParseSeat(FastGameJson.ParseObject(text));
        }

        /// <summary>
        /// Designer JoinMap step 1: mint seat. Then join sibling Colyseus with
        /// <see cref="SeatMintResult.SeatToken"/> / <see cref="SeatMintResult.RoomName"/> /
        /// <see cref="SeatMintResult.GameServerUrl"/>. Do not pass designer-chosen
        /// gameId/mapId as authority.
        /// </summary>
        public Task<SeatMintResult> JoinMapAsync(
            string gameCode,
            string mapId,
            string modeId = null) =>
            MintSeatAsync(gameCode, mapId, modeId);

        static SeatMintResult ParseSeat(Dictionary<string, object> o)
        {
            if (o == null) return new SeatMintResult();
            return new SeatMintResult
            {
                SeatToken = FastGameJson.GetString(o, "seat_token"),
                ExpiresAt = FastGameJson.GetString(o, "expires_at"),
                GameServerUrl = FastGameJson.GetString(o, "game_server_url"),
                RoomName = FastGameJson.GetString(o, "room_name"),
                GameId = FastGameJson.GetString(o, "game_id"),
                MapId = FastGameJson.GetString(o, "map_id"),
                ModeId = FastGameJson.GetString(o, "mode_id"),
            };
        }
    }
}
