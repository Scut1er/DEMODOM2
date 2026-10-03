using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RealityDirector.EditorTools
{
    // Обход экранов в плей-моде: снимок + список активных кнопок, клик по пути или подписи.
    public static class UiTour
    {
        public static string Dir => System.IO.Path.Combine(Application.temporaryCachePath, "UiTour") + "/";

        public static string Shot(string name, int width = 1600, int height = 900)
        {
            Application.runInBackground = true;
            System.IO.Directory.CreateDirectory(Dir);
            UiCapture.Capture(Dir + name + ".png", width, height);
            return Buttons();
        }

        public static string Buttons()
        {
            var sb = new StringBuilder();
            foreach (var b in Object.FindObjectsByType<Button>())
            {
                if (!b.gameObject.activeInHierarchy)
                    continue;
                var txt = b.GetComponentInChildren<Text>();
                sb.Append(PathOf(b.transform)).Append(" | ").Append(txt != null ? txt.text.Replace("\n", " ") : "").Append(b.interactable ? "" : " (off)").Append('\n');
            }
            return sb.ToString();
        }

        // Клик по кнопке, чей путь заканчивается на key или чья подпись содержит key.
        public static string Click(string key)
        {
            foreach (var b in Object.FindObjectsByType<Button>())
            {
                if (!b.gameObject.activeInHierarchy || !b.interactable)
                    continue;
                var txt = b.GetComponentInChildren<Text>();
                if (PathOf(b.transform).EndsWith(key) || (txt != null && txt.text.Contains(key)))
                {
                    b.onClick.Invoke();
                    return "clicked " + PathOf(b.transform);
                }
            }
            return "not found: " + key;
        }

        // Дерево UI с якорями, размерами, спрайтами и текстами — в файл.
        public static void Dump(string rootName, string file, int maxDepth = 6)
        {
            var sb = new StringBuilder();
            var root = GameObject.Find(rootName);
            if (root != null)
                Dump(root.transform, 0, sb, maxDepth);
            System.IO.File.WriteAllText(Dir + file, sb.ToString());
        }

        public static void DumpPrefab(string assetPath, string file, int maxDepth = 6)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var sb = new StringBuilder();
            if (go != null)
                Dump(go.transform, 0, sb, maxDepth);
            System.IO.File.WriteAllText(Dir + file, sb.ToString());
        }

        static void Dump(Transform t, int depth, StringBuilder sb, int maxDepth)
        {
            var rt = t as RectTransform;
            var img = t.GetComponent<UnityEngine.UI.Image>();
            var txt = t.GetComponent<Text>();
            sb.Append(new string(' ', depth * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " [off]");
            if (rt != null)
                sb.AppendFormat(" a({0:0.##},{1:0.##})-({2:0.##},{3:0.##}) p({4:0},{5:0}) s({6:0},{7:0})", rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y, rt.anchoredPosition.x, rt.anchoredPosition.y, rt.sizeDelta.x, rt.sizeDelta.y);
            if (img != null)
                sb.AppendFormat(" img({0},{1})", img.sprite != null ? img.sprite.name : "-", ColorUtility.ToHtmlStringRGBA(img.color));
            if (txt != null)
                sb.AppendFormat(" txt[{0}|{1}|{2}]", txt.text.Replace("\n", "/").Substring(0, Mathf.Min(30, txt.text.Length)), txt.fontSize, ColorUtility.ToHtmlStringRGB(txt.color));
            foreach (var c in t.GetComponents<MonoBehaviour>())
            {
                if (c != null && !(c is Graphic) && !(c is Selectable))
                    sb.Append(" <").Append(c.GetType().Name).Append(">");
            }

            sb.Append('\n');
            if (depth < maxDepth)
            {
                foreach (Transform ch in t)
                    Dump(ch, depth + 1, sb, maxDepth);
            }
        }

        public static string PathOf(Transform t)
        {
            return t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
        }
    }
}
