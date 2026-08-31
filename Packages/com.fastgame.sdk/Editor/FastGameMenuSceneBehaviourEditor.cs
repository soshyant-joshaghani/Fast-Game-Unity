using UnityEditor;
using UnityEngine;

namespace FastGame.Editor
{
    [CustomEditor(typeof(FastGameMenuSceneBehaviour))]
    public sealed class FastGameMenuSceneBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var menu = (FastGameMenuSceneBehaviour)target;
            if (menu == null)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Navigation diagnostics", EditorStyles.boldLabel);

            var issues = menu.NavButtons.Validate();
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("All navigation button slots assigned and names look correct.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    issues.Count + " navigation issue(s):\n• " + string.Join("\n• ", issues),
                    MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find missing buttons"))
                {
                    Undo.RecordObject(menu, "Find menu nav buttons");
                    menu.PopulateNavigationButtons(missingOnly: true);
                    EditorUtility.SetDirty(menu);
                }

                if (GUILayout.Button("Find all buttons (overwrite)"))
                {
                    if (EditorUtility.DisplayDialog(
                            "Fast Game Menu",
                            "Replace all Nav Buttons slots from hierarchy?",
                            "Replace",
                            "Cancel"))
                    {
                        Undo.RecordObject(menu, "Replace menu nav buttons");
                        menu.PopulateNavigationButtons(missingOnly: false);
                        EditorUtility.SetDirty(menu);
                    }
                }
            }

            if (GUILayout.Button("Wire navigation now (Play mode safe)"))
                menu.WireNavigationButtons();
        }
    }
}
