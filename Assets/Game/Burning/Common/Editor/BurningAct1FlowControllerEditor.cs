using FilmInspiredGames.Burning;
using UnityEditor;

namespace FilmInspiredGames.Burning.Editor
{
    [CustomEditor(typeof(BurningAct1FlowController))]
    public sealed class BurningAct1FlowControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 연결값은 씬 빌더가 관리. 진행 확인은 Chapter Debugger 사용
        }
    }
}
