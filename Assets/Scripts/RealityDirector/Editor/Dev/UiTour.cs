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

        // Сыграть карту в квартире без руки и денег: на участника (actorId) или в точку (x, y).
        public static string PlayCard(string cardId, string actorId = null, float x = float.NaN, float y = float.NaN)
        {
            var executor = Object.FindAnyObjectByType<RealityDirector.Events.EventExecutor>();
            if (executor == null)
                return "no executor";
            RealityDirector.Events.EventDefinition def = null;
            foreach (var d in Resources.LoadAll<RealityDirector.Events.EventDefinition>("Content/Cards"))
            {
                if (d.id == cardId)
                    def = d;
            }

            if (def == null)
                return "no card " + cardId;
            RealityDirector.NPC.NPCController actor = null;
            foreach (var n in Object.FindObjectsByType<RealityDirector.NPC.NPCController>())
            {
                if (n.Id == actorId)
                    actor = n;
            }

            RealityDirector.Events.Interactable obj = null;
            if (!string.IsNullOrEmpty(def.requiredObjectId))
            {
                foreach (var i in Object.FindObjectsByType<RealityDirector.Events.Interactable>())
                {
                    if (i.Id == def.requiredObjectId)
                        obj = i;
                }
            }

            Vector2? point = float.IsNaN(x) ? (Vector2?)null : new Vector2(x, y);
            executor.Play(def, obj, actor, point);
            return "played " + def.displayName;
        }

        // Серия снимков с интервалом — видно, как карта разыгрывается во времени.
        public static void Burst(string prefix, int count, float interval)
        {
            int done = 0;
            double next = UnityEditor.EditorApplication.timeSinceStartup;
            UnityEditor.EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!Application.isPlaying || done >= count)
                {
                    UnityEditor.EditorApplication.update -= tick;
                    return;
                }

                if (UnityEditor.EditorApplication.timeSinceStartup < next)
                    return;
                next = UnityEditor.EditorApplication.timeSinceStartup + interval;
                Shot(prefix + "_" + done, Screen.width, Screen.height);
                done++;
            };
            UnityEditor.EditorApplication.update += tick;
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

        // Навести мышь на объект UI (подсказки карт): pointerEnter по имени объекта; exit — увести.
        public static string Hover(string name, bool exit = false)
        {
            foreach (var rect in Object.FindObjectsByType<RectTransform>())
            {
                if (!rect.gameObject.activeInHierarchy || rect.name != name)
                    continue;
                var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                if (exit)
                    UnityEngine.EventSystems.ExecuteEvents.Execute(rect.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
                else
                    UnityEngine.EventSystems.ExecuteEvents.Execute(rect.gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                return "hovered " + PathOf(rect);
            }

            return "not found: " + name;
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
