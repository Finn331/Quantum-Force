#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
namespace cowsins
{
    public static partial class CowsinsEditorWindowUtilities
    {
        public static GUIStyle TitleStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

        }
        public static GUIStyle SubtitleStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };

        }
        public static GUIStyle BigHeadingStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 1f, 1f, 0.3f) }
            };
        }

        public static void DrawTutorialCard(Texture2D image, string url, float size)
        {
            GUILayout.Label(image, GUILayout.Width(400 * size), GUILayout.Height(250 * size));

            Rect imageRect = GUILayoutUtility.GetLastRect();
            float buttonSize = 50;
            float buttonX = imageRect.x + (imageRect.width / 2) - (buttonSize / 2);
            float buttonY = imageRect.y + (imageRect.height / 2) - (buttonSize / 2);

            if (string.IsNullOrWhiteSpace(url)) return;

            var playButtonStyle = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.white } };
            GUI.backgroundColor = new Color(0, 0, 0, 0.5f);

            if (GUI.Button(new Rect(buttonX, buttonY, buttonSize, buttonSize), "▶", playButtonStyle))
            {
                Application.OpenURL(url);
            }
        }

        public static void DrawLinkCard(Texture2D icon, string title, string url, float sizeX, float sizeY)
        {
            GUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"), GUILayout.Width(300 * sizeX), GUILayout.Height(300 * sizeY));
            GUILayout.FlexibleSpace();

            // Center the icon horizontally
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(icon, GUILayout.Width(40 * sizeX), GUILayout.Height(40 * sizeX));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Add some space before the title
            GUILayout.Space(5);

            // Center the button horizontally
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(title, GUILayout.Width(100), GUILayout.Height(30)))
                Application.OpenURL(url);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}
#endif