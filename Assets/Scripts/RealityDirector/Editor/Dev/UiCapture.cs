using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RealityDirector.EditorTools
{
    // Снимок всего UI в Play (все overlay-канвасы по порядку отрисовки) — для проверки экранов без окна Game.
    public static class UiCapture
    {
        public static void Capture(string path, int width = 1600, int height = 900)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("CaptureCam");
                cam = go.AddComponent<Camera>();
                cam.orthographic = true;
            }

            var canvases = new List<Canvas>();
            foreach (var c in Object.FindObjectsByType<Canvas>())
            {
                if (c.isRootCanvas && c.isActiveAndEnabled && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    canvases.Add(c);
            }

            // Больший sortingOrder — ближе к камере.
            foreach (var c in canvases)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = Mathf.Max(cam.nearClipPlane + 0.05f, 9f - c.sortingOrder * 0.01f);
            }

            // Раскладка под размер снимка: так можно проверить другие пропорции экрана.
            var rt = new RenderTexture(width, height, 24);
            var old = cam.targetTexture;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.enabled = false;
                    scaler.enabled = true;
                }
            }

            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            cam.targetTexture = old;
            RenderTexture.active = null;
            foreach (var c in canvases)
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
