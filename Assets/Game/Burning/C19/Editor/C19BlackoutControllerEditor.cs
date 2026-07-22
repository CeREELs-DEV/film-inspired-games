using FilmInspiredGames.Burning.C19;
using UnityEditor;

namespace FilmInspiredGames.Burning.C19.Editor
{
    [CustomEditor(typeof(C19BlackoutController))]
    public sealed class C19BlackoutControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            C19BlackoutController sequence = (C19BlackoutController)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 챕터", sequence.CurrentChapter);
            EditorGUILayout.LabelField("현재 상태", sequence.CurrentState);
        }
    }
}
