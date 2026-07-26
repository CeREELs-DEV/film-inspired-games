using UnityEditor;
using UnityEngine;

namespace FilmInspiredGames.Burning.Editor
{
    [CustomEditor(typeof(BurningChapterSettings))]
    public sealed class BurningChapterSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty transitions = serializedObject.FindProperty("transitions");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Transition", EditorStyles.miniBoldLabel, GUILayout.Width(92f));
                GUILayout.Label("Fade Out", EditorStyles.miniBoldLabel);
                GUILayout.Label("Black Hold", EditorStyles.miniBoldLabel);
                GUILayout.Label("Fade In", EditorStyles.miniBoldLabel);
            }

            for (int index = 0; index < transitions.arraySize; index++)
            {
                SerializedProperty transition = transitions.GetArrayElementAtIndex(index);
                SerializedProperty from = transition.FindPropertyRelative("fromChapter");
                SerializedProperty to = transition.FindPropertyRelative("toChapter");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"{from.stringValue} → {to.stringValue}", GUILayout.Width(92f));
                    DrawDuration(transition.FindPropertyRelative("fadeOutDuration"));
                    DrawDuration(transition.FindPropertyRelative("blackHoldDuration"));
                    DrawDuration(transition.FindPropertyRelative("fadeInDuration"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawDuration(SerializedProperty property)
        {
            property.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(property.floatValue));
        }
    }
}
