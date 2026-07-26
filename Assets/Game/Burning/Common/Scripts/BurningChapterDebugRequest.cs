namespace FilmInspiredGames.Burning
{
    public static class BurningChapterDebugRequest
    {
#if UNITY_EDITOR
        private const string ChapterKey = "FilmInspiredGames.Burning.DebugChapter";

        public static void Set(string chapter)
        {
            UnityEditor.SessionState.SetString(ChapterKey, chapter);
        }

        public static string Consume()
        {
            string chapter = UnityEditor.SessionState.GetString(ChapterKey, string.Empty);
            UnityEditor.SessionState.EraseString(ChapterKey);
            return chapter;
        }

        public static void Clear()
        {
            UnityEditor.SessionState.EraseString(ChapterKey);
        }
#else
        private static string pendingChapter = string.Empty;

        public static void Set(string chapter)
        {
            pendingChapter = chapter;
        }

        public static string Consume()
        {
            string chapter = pendingChapter;
            pendingChapter = string.Empty;
            return chapter;
        }

        public static void Clear()
        {
            pendingChapter = string.Empty;
        }
#endif
    }
}
