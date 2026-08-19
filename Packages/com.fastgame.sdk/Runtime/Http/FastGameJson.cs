using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FastGame
{
    /// <summary>Minimal JSON parse/stringify for Fast Game DTOs (no external deps).</summary>
    public static class FastGameJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return new Parser(json).ParseValue();
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            return Parse(json) as Dictionary<string, object>;
        }

        public static List<object> ParseArray(string json)
        {
            return Parse(json) as List<object>;
        }

        public static string Stringify(object obj)
        {
            var sb = new StringBuilder();
            WriteValue(sb, obj);
            return sb.ToString();
        }

        public static string GetString(Dictionary<string, object> obj, string key, string fallback = null)
        {
            if (obj == null || !obj.TryGetValue(key, out var v) || v == null) return fallback;
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public static int GetInt(Dictionary<string, object> obj, string key, int fallback = 0)
        {
            if (obj == null || !obj.TryGetValue(key, out var v) || v == null) return fallback;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public static bool GetBool(Dictionary<string, object> obj, string key, bool fallback = false)
        {
            if (obj == null || !obj.TryGetValue(key, out var v) || v == null) return fallback;
            try { return Convert.ToBoolean(v, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public static float GetFloat(Dictionary<string, object> obj, string key, float fallback = 0f)
        {
            if (obj == null || !obj.TryGetValue(key, out var v) || v == null) return fallback;
            try { return Convert.ToSingle(v, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        public static Dictionary<string, object> GetObject(Dictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.TryGetValue(key, out var v)) return null;
            return v as Dictionary<string, object>;
        }

        public static List<object> GetArray(Dictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.TryGetValue(key, out var v)) return null;
            return v as List<object>;
        }

        /// <summary>Shop unlock/restore/complete JSON: gateway_message, message, detail.</summary>
        public static string ExtractShopUnlockMessage(Dictionary<string, object> obj, string fallback = "")
        {
            var gw = GetString(obj, "gateway_message");
            if (!string.IsNullOrWhiteSpace(gw)) return gw;
            var msg = GetString(obj, "message");
            if (!string.IsNullOrWhiteSpace(msg)) return msg;
            var detail = GetString(obj, "detail");
            if (!string.IsNullOrWhiteSpace(detail)) return detail;
            return fallback;
        }

        /// <summary>Parse FastGameException bodies like "502: {\"detail\":...}".</summary>
        public static string ParseApiErrorMessage(string err, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(err)) return fallback;
            var colon = err.IndexOf(':');
            if (colon > 0 && int.TryParse(err.Substring(0, colon).Trim(), out _))
            {
                var body = err.Substring(colon + 1).Trim();
                return ExtractShopUnlockMessage(ParseObject(body), body);
            }
            return err;
        }

        static void WriteValue(StringBuilder sb, object value)
        {
            if (value == null) { sb.Append("null"); return; }
            switch (value)
            {
                case string s:
                    sb.Append('"');
                    foreach (var c in s)
                    {
                        if (c == '"' || c == '\\') sb.Append('\\');
                        sb.Append(c);
                    }
                    sb.Append('"');
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case IDictionary dict:
                    sb.Append('{');
                    var first = true;
                    foreach (DictionaryEntry e in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteValue(sb, e.Key?.ToString() ?? "");
                        sb.Append(':');
                        WriteValue(sb, e.Value);
                    }
                    sb.Append('}');
                    break;
                case IList list:
                    sb.Append('[');
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        WriteValue(sb, list[i]);
                    }
                    sb.Append(']');
                    break;
                default:
                    if (value is IFormattable f)
                        sb.Append(f.ToString(null, CultureInfo.InvariantCulture));
                    else
                        WriteValue(sb, value.ToString());
                    break;
            }
        }

        sealed class Parser
        {
            readonly string _json;
            int _i;

            public Parser(string json) { _json = json; }

            public object ParseValue()
            {
                Skip();
                if (_i >= _json.Length) return null;
                var c = _json[_i];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == 't' || c == 'f') return ParseBool();
                if (c == 'n') { _i += 4; return null; }
                return ParseNumber();
            }

            Dictionary<string, object> ParseObject()
            {
                var obj = new Dictionary<string, object>();
                _i++;
                while (true)
                {
                    Skip();
                    if (_i < _json.Length && _json[_i] == '}') { _i++; break; }
                    var key = ParseString();
                    Skip();
                    _i++; // :
                    var val = ParseValue();
                    obj[key] = val;
                    Skip();
                    if (_i < _json.Length && _json[_i] == ',') { _i++; continue; }
                    if (_i < _json.Length && _json[_i] == '}') { _i++; break; }
                }
                return obj;
            }

            List<object> ParseArray()
            {
                var list = new List<object>();
                _i++;
                while (true)
                {
                    Skip();
                    if (_i < _json.Length && _json[_i] == ']') { _i++; break; }
                    list.Add(ParseValue());
                    Skip();
                    if (_i < _json.Length && _json[_i] == ',') { _i++; continue; }
                    if (_i < _json.Length && _json[_i] == ']') { _i++; break; }
                }
                return list;
            }

            string ParseString()
            {
                var sb = new StringBuilder();
                _i++;
                while (_i < _json.Length)
                {
                    var c = _json[_i++];
                    if (c == '"') break;
                    if (c == '\\' && _i < _json.Length)
                    {
                        var n = _json[_i++];
                        sb.Append(n == 'n' ? '\n' : n == 't' ? '\t' : n == 'r' ? '\r' : n);
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }

            bool ParseBool()
            {
                if (_json[_i] == 't') { _i += 4; return true; }
                _i += 5;
                return false;
            }

            object ParseNumber()
            {
                var start = _i;
                while (_i < _json.Length && "0123456789+-.eE".IndexOf(_json[_i]) >= 0) _i++;
                var s = _json.Substring(start, _i - start);
                if (s.IndexOf('.') >= 0 || s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0)
                    return double.Parse(s, CultureInfo.InvariantCulture);
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    return l;
                return double.Parse(s, CultureInfo.InvariantCulture);
            }

            void Skip()
            {
                while (_i < _json.Length && char.IsWhiteSpace(_json[_i])) _i++;
            }
        }
    }
}
