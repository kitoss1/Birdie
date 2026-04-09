using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Editor
{
    /// <summary>
    /// Converts all SpriteRenderer components in the selected GameObject's hierarchy to UI Images.
    /// The root GameObject must be under a Canvas before running this tool.
    /// Note: inspector references to converted GameObjects (e.g. food level visuals) must be re-assigned after conversion.
    /// </summary>
    public static class SpriteRendererToImageConverter
    {
        [MenuItem("Birdie/Convert SpriteRenderers to Images", validate = false)]
        private static void Convert()
        {
            GameObject root = Selection.activeGameObject;

            if (root == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Select a GameObject in the Hierarchy first.", "OK");
                return;
            }

            if (root.GetComponentInParent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog(
                    "No Canvas Found",
                    "The selected GameObject must be placed under a Canvas before converting.",
                    "OK");
                return;
            }

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

            if (renderers.Length == 0)
            {
                EditorUtility.DisplayDialog("Nothing to Convert", "No SpriteRenderer components found.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Convert SpriteRenderers to Images",
                $"This will convert {renderers.Length} SpriteRenderer(s) to UI Images.\n\nInspector references to the converted GameObjects will need to be re-assigned afterward.\n\nContinue?",
                "Convert", "Cancel");

            if (!confirmed)
            {
                return;
            }

            int count = 0;

            foreach (SpriteRenderer sr in renderers)
            {
                ConvertSpriteRenderer(sr);
                count++;
            }

            EditorUtility.SetDirty(root);
            UnityEngine.Debug.Log($"[{nameof(SpriteRendererToImageConverter)}] Converted {count} SpriteRenderer(s) to Image.");
        }

        private static void ConvertSpriteRenderer(SpriteRenderer sr)
        {
            GameObject oldGo = sr.gameObject;
            Transform parent = oldGo.transform.parent;
            int siblingIndex = oldGo.transform.GetSiblingIndex();

            Sprite sprite = sr.sprite;
            Color color = sr.color;
            string goName = oldGo.name;
            Vector3 localPos = oldGo.transform.localPosition;
            Vector3 localScale = oldGo.transform.localScale;
            Quaternion localRot = oldGo.transform.localRotation;

            // Collect children to re-parent
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < oldGo.transform.childCount; i++)
            {
                children.Add(oldGo.transform.GetChild(i));
            }

            // Create replacement with RectTransform (Image requires RectTransform)
            GameObject newGo = new GameObject(goName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(newGo, "Convert SpriteRenderer to Image");
            newGo.transform.SetParent(parent, worldPositionStays: false);
            newGo.transform.SetSiblingIndex(siblingIndex);
            newGo.transform.localPosition = localPos;
            newGo.transform.localScale = localScale;
            newGo.transform.localRotation = localRot;

            Image image = newGo.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;

            if (sprite != null)
            {
                RectTransform rt = newGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(
                    sprite.rect.width / sprite.pixelsPerUnit,
                    sprite.rect.height / sprite.pixelsPerUnit);
            }

            // Move children to the new GameObject
            foreach (Transform child in children)
            {
                Undo.SetTransformParent(child, newGo.transform, "Convert SpriteRenderer to Image");
            }

            Undo.DestroyObjectImmediate(oldGo);
        }

        [MenuItem("Birdie/Convert SpriteRenderers to Images", validate = true)]
        private static bool ValidateConvert()
        {
            return Selection.activeGameObject != null;
        }
    }
}
