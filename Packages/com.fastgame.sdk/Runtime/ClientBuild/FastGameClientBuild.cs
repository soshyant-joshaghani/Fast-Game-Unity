using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FastGame
{
    public static class FastGameClientBuild
    {
        public static string ProjectStageToWire(FastGameProjectStage stage)
        {
            switch (stage)
            {
                case FastGameProjectStage.Production:
                    return "production";
                case FastGameProjectStage.EarlyAccess:
                    return "early_access";
                default:
                    return "dev";
            }
        }

        public static async Task<(bool ok, string message)> InitializeAsync(
            FastGameHttp http,
            FastGameConfig config)
        {
            if (http == null || config == null)
                return (false, "FastGame client not ready");

            var token = (config.ClientAccessToken ?? "").Trim();
            if (string.IsNullOrEmpty(token))
                return (false, "Client access token is required");

            var gameCode = (config.GameCode ?? "").Trim();
            if (string.IsNullOrEmpty(gameCode))
                return (false, "GameCode is required — call Initialize Game first");

            var body = new InitializeBody
            {
                game_code = gameCode,
                project_stage = ProjectStageToWire(config.ProjectStage),
                access_token = token,
                client_instance_id = string.IsNullOrEmpty(config.ClientInstanceId)
                    ? null
                    : config.ClientInstanceId,
            };

            try
            {
                var parsed = await http.PostJsonAsync<InitializeResponse>(
                    "/apps/games/client/initialize",
                    body);

                if (parsed == null || !parsed.ok)
                    return (false, "Client initialize rejected");

                if (!string.IsNullOrEmpty(parsed.client_instance_id))
                    config.ClientInstanceId = parsed.client_instance_id;
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        [Serializable]
        sealed class InitializeBody
        {
            public string game_code;
            public string project_stage;
            public string access_token;
            public string client_instance_id;
        }

        [Serializable]
        sealed class InitializeResponse
        {
            public bool ok;
            public string client_instance_id;
        }
    }
}
