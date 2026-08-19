using System.Reflection;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Reads / writes text from Unity UI <c>InputField</c> or TMP <c>TMP_InputField</c>
    /// without a hard TMP package dependency.
    /// </summary>
    /// <remarks>
    /// Drag the <b>input field root</b> (the object with TMP_InputField / InputField), not the
    /// child <c>Text</c> / <c>Placeholder</c>. If a child Text or RectTransform is assigned,
    /// this helper walks parents/children to find the real input field (password fields must
    /// use TMP_InputField.text — the child Text only shows ****).
    /// </remarks>
    public static class FastGameUiText
    {
        static readonly BindingFlags PropFlags = BindingFlags.Instance | BindingFlags.Public;

        public static string Read(Component field, string fallback = "")
        {
            var input = ResolveInput(field);
            if (input != null && TryReadText(input, out var value))
                return value ?? "";
            return fallback ?? "";
        }

        public static void Write(Component field, string value)
        {
            var input = ResolveInput(field);
            if (input == null)
                return;
            TryWriteText(input, value ?? "");
        }

        /// <summary>
        /// Write to a label (TMP_Text / UI.Text). Does not prefer InputField — use for error messages.
        /// </summary>
        public static void WriteLabel(Component label, string value)
        {
            if (label == null)
                return;

            var textComp = ResolveLabel(label);
            if (textComp != null && TryWriteText(textComp, value ?? ""))
                return;

            // Last resort: write .text on whatever was assigned
            TryWriteText(label, value ?? "");
        }

        public static Component ResolveLabel(Component label)
        {
            if (label == null)
                return null;

            var name = label.GetType().Name;
            if (IsLabelTypeName(name))
                return label;

            var go = label.gameObject;
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null || c is Transform)
                    continue;
                if (IsLabelTypeName(c.GetType().Name))
                    return c;
            }

            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform)
                    continue;
                if (IsLabelTypeName(c.GetType().Name))
                    return c;
            }

            return null;
        }

        static bool IsLabelTypeName(string name) =>
            name == "TextMeshProUGUI"
            || name == "TextMeshPro"
            || name == "TMP_Text"
            || name == "Text";

        /// <summary>
        /// Prefer TMP_InputField / InputField on the same object, parents, or children.
        /// </summary>
        public static Component ResolveInput(Component field)
        {
            if (field == null)
                return null;

            var byName = FindInputByTypeName(field);
            if (byName != null)
                return byName;

            // Assigned Transform / RectTransform / child Text — search hierarchy
            var t = field.transform;
            for (var p = t; p != null; p = p.parent)
            {
                var found = FindInputOn(p.gameObject);
                if (found != null)
                    return found;
            }

            return FindInputInChildren(t.gameObject);
        }

        static Component FindInputByTypeName(Component c)
        {
            var name = c.GetType().Name;
            if (name == "TMP_InputField" || name == "InputField")
                return c;
            return null;
        }

        static Component FindInputOn(GameObject go)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null || c is Transform)
                    continue;
                var found = FindInputByTypeName(c);
                if (found != null)
                    return found;
            }
            return null;
        }

        static Component FindInputInChildren(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform)
                    continue;
                var found = FindInputByTypeName(c);
                if (found != null)
                    return found;
            }
            return null;
        }

        static bool TryReadText(Component c, out string value)
        {
            value = null;
            var prop = c.GetType().GetProperty("text", PropFlags);
            if (prop == null || prop.PropertyType != typeof(string) || !prop.CanRead)
                return false;
            value = (string)prop.GetValue(c);
            return true;
        }

        static bool TryWriteText(Component c, string value)
        {
            var prop = c.GetType().GetProperty("text", PropFlags);
            if (prop == null || prop.PropertyType != typeof(string) || !prop.CanWrite)
                return false;
            prop.SetValue(c, value);
            return true;
        }
    }
}
