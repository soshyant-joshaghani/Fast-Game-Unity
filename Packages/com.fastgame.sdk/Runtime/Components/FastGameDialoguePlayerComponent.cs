using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable]
    public class FastGameDialogueNodeEvent : UnityEvent<string, string, string> { }

    [Serializable]
    public class FastGameDialogueChoicesEvent : UnityEvent<string, List<FastGameDialogueChoice>> { }

    [Serializable]
    public class FastGameDialogueChoice
    {
        public string ChoiceId;
        public string Label;
    }

    /// <summary>
    /// Fetch tip GetDialogue and walk the tree (V3). Choices wait for SelectChoice.
    /// Linear nodes auto-advance. Effects → progress flags (dialogue.flag).
    /// </summary>
    [AddComponentMenu("Fast Game/Scenarios/Dialogue Player")]
    public sealed class FastGameDialoguePlayerComponent : MonoBehaviour
    {
        public string DialogueId;
        public FastGameClientBehaviour ClientHost;

        [Tooltip("Seconds to show a linear node before auto-advance (0 = immediate).")]
        public float LinearNodeHoldSeconds = 0.05f;

        [Tooltip("If true and choices present with no UI, pick first choice (smoke / CI).")]
        public bool AutoPickFirstChoice;

        public UnityEvent<string> OnSuccess = new();
        public UnityEvent<string, string> OnFailed = new();
        public FastGameDialogueNodeEvent OnNodePresented;
        public FastGameDialogueChoicesEvent OnChoicesPresented;
        public UnityEvent<string> OnDialogueEnded;

        readonly Dictionary<string, object> _vars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        TaskCompletionSource<string> _choiceWait;
        Dictionary<string, object> _tip;
        string _activeId = "";

        public async Task PlayDialogueAsync(string dialogueId = null, CancellationToken ct = default)
        {
            var id = string.IsNullOrWhiteSpace(dialogueId) ? DialogueId : dialogueId;
            if (string.IsNullOrWhiteSpace(id))
            {
                OnFailed?.Invoke("", "DialogueId required");
                return;
            }
            _activeId = id.Trim();
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = (client.Config.GameCode ?? "").Trim();
                if (string.IsNullOrEmpty(code))
                    throw new FastGameException("FastGame: GameCode empty");

                _tip = await client.Content.GetDialogueAsync(code, _activeId);
                var payload = FastGameJson.GetObject(_tip, "payload") ?? _tip;
                var entry = FastGameJson.GetString(payload, "entry") ?? "start";
                InitVars(payload);
                await WalkNodesAsync(payload, entry, client, code, ct);
                OnSuccess?.Invoke(_activeId);
                OnDialogueEnded?.Invoke(_activeId);
            }
            catch (Exception e)
            {
                OnFailed?.Invoke(_activeId, e.Message);
            }
        }

        /// <summary>UI calls this when the player picks a choice.</summary>
        public void SelectChoice(string choiceId)
        {
            _choiceWait?.TrySetResult(choiceId ?? "");
        }

        void InitVars(Dictionary<string, object> payload)
        {
            _vars.Clear();
            var vars = FastGameJson.GetArray(payload, "variables");
            if (vars == null)
                return;
            for (var i = 0; i < vars.Count; i++)
            {
                var row = vars[i] as Dictionary<string, object>;
                if (row == null)
                    continue;
                var name = FastGameJson.GetString(row, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                _vars[name.Trim()] = row.TryGetValue("default", out var d) ? d : false;
            }
        }

        async Task WalkNodesAsync(
            Dictionary<string, object> payload,
            string entry,
            FastGameClient client,
            string code,
            CancellationToken ct)
        {
            var nodes = IndexNodes(payload);
            var current = entry;
            var guard = 0;
            while (!string.IsNullOrEmpty(current) && guard++ < 128)
            {
                ct.ThrowIfCancellationRequested();
                if (!nodes.TryGetValue(current, out var node))
                    break;

                var text = ResolveText(node);
                var speaker = FastGameJson.GetString(node, "speaker_id") ?? "";
                OnNodePresented?.Invoke(current, speaker, text);

                await ApplyEffectsAsync(node, client, code);

                var choices = FastGameJson.GetArray(node, "choices");
                if (choices != null && choices.Count > 0)
                {
                    var list = ParseChoices(choices);
                    OnChoicesPresented?.Invoke(current, list);
                    string picked;
                    if (AutoPickFirstChoice && list.Count > 0)
                        picked = list[0].ChoiceId;
                    else
                    {
                        _choiceWait = new TaskCompletionSource<string>();
                        using (ct.Register(() => _choiceWait.TrySetCanceled()))
                            picked = await _choiceWait.Task;
                    }
                    current = FindChoiceNext(choices, picked)
                        ?? FastGameJson.GetString(node, "next");
                }
                else
                {
                    if (LinearNodeHoldSeconds > 0f)
                        await Task.Delay(TimeSpan.FromSeconds(LinearNodeHoldSeconds), ct);
                    current = FastGameJson.GetString(node, "next");
                }
            }
        }

        async Task ApplyEffectsAsync(Dictionary<string, object> node, FastGameClient client, string code)
        {
            var effects = FastGameJson.GetArray(node, "effects");
            if (effects == null || effects.Count == 0)
                return;
            var flags = new Dictionary<string, object>();
            for (var i = 0; i < effects.Count; i++)
            {
                var fx = effects[i] as Dictionary<string, object>;
                if (fx == null)
                    continue;
                var varName = FastGameJson.GetString(fx, "set_var");
                if (string.IsNullOrWhiteSpace(varName))
                    continue;
                var value = fx.TryGetValue("value", out var v) ? v : true;
                _vars[varName.Trim()] = value;
                flags[varName.Trim()] = value;
            }
            if (flags.Count == 0)
                return;
            try
            {
                await client.Progress.SaveAsync(code, "dialogue.flag", "", flags);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FastGame Dialogue] flag save: " + e.Message, this);
            }
        }

        static Dictionary<string, Dictionary<string, object>> IndexNodes(Dictionary<string, object> payload)
        {
            var outDict = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            var nodes = FastGameJson.GetArray(payload, "nodes");
            if (nodes == null)
                return outDict;
            for (var i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i] as Dictionary<string, object>;
                if (n == null)
                    continue;
                var id = FastGameJson.GetString(n, "node_id");
                if (!string.IsNullOrWhiteSpace(id))
                    outDict[id.Trim()] = n;
            }
            return outDict;
        }

        static string ResolveText(Dictionary<string, object> node)
        {
            var text = FastGameJson.GetObject(node, "text");
            if (text != null)
            {
                var en = FastGameJson.GetString(text, "en");
                if (!string.IsNullOrEmpty(en))
                    return en;
                foreach (var kv in text)
                    if (kv.Value != null)
                        return kv.Value.ToString();
            }
            return FastGameJson.GetString(node, "text") ?? "";
        }

        static List<FastGameDialogueChoice> ParseChoices(List<object> choices)
        {
            var list = new List<FastGameDialogueChoice>();
            for (var i = 0; i < choices.Count; i++)
            {
                var c = choices[i] as Dictionary<string, object>;
                if (c == null)
                    continue;
                var id = FastGameJson.GetString(c, "choice_id") ?? $"c{i}";
                var labelObj = FastGameJson.GetObject(c, "label");
                var label = labelObj != null
                    ? (FastGameJson.GetString(labelObj, "en") ?? id)
                    : (FastGameJson.GetString(c, "label") ?? id);
                list.Add(new FastGameDialogueChoice { ChoiceId = id, Label = label });
            }
            return list;
        }

        static string FindChoiceNext(List<object> choices, string choiceId)
        {
            for (var i = 0; i < choices.Count; i++)
            {
                var c = choices[i] as Dictionary<string, object>;
                if (c == null)
                    continue;
                var id = FastGameJson.GetString(c, "choice_id");
                if (string.Equals(id, choiceId, StringComparison.OrdinalIgnoreCase))
                    return FastGameJson.GetString(c, "next");
            }
            return null;
        }
    }
}
