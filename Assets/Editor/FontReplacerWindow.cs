using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Birdie.Editor
{
    /// <summary>
    /// Editor window that replaces the font asset on all TextMeshPro components
    /// in the active scene and/or all prefabs in the project.
    /// </summary>
    internal sealed class FontReplacerWindow : EditorWindow
    {
        private TMP_FontAsset m_targetFont;
        private bool m_includeScene = true;
        private bool m_includePrefabs = true;
        private bool m_previewOnly = true;

        [MenuItem("Birdie/Font Replacer")]
        private static void OpenWindow()
        {
            var window = GetWindow<FontReplacerWindow>("Font Replacer");
            window.minSize = new Vector2(340f, 200f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("TMP Font Replacer", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            m_targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "New Font Asset",
                m_targetFont,
                typeof(TMP_FontAsset),
                false);

            EditorGUILayout.Space(4f);
            m_includeScene = EditorGUILayout.Toggle("Active Scene", m_includeScene);
            m_includePrefabs = EditorGUILayout.Toggle("Project Prefabs", m_includePrefabs);
            EditorGUILayout.Space(4f);
            m_previewOnly = EditorGUILayout.Toggle("Preview Only (dry run)", m_previewOnly);

            EditorGUILayout.Space(8f);

            bool canReplace = m_targetFont != null && (m_includeScene || m_includePrefabs);
            using (new EditorGUI.DisabledScope(!canReplace))
            {
                string buttonLabel = m_previewOnly ? "Preview Replacements" : "Replace All Fonts";
                if (GUILayout.Button(buttonLabel, GUILayout.Height(32f)))
                {
                    RunReplacement();
                }
            }

            if (!canReplace)
            {
                EditorGUILayout.HelpBox("Assign a font asset and select at least one target.", MessageType.Info);
            }
        }

        private void RunReplacement()
        {
            int total = 0;

            if (m_includeScene)
            {
                total += ReplaceInScene();
            }

            if (m_includePrefabs)
            {
                total += ReplaceInPrefabs();
            }

            string action = m_previewOnly ? "Would replace" : "Replaced";
            UnityEngine.Debug.Log($"[{nameof(FontReplacerWindow)}] {action} font on {total} TMP component(s) in total.");
        }

        private int ReplaceInScene()
        {
            TMP_Text[] components = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;

            foreach (TMP_Text component in components)
            {
                if (component.font == m_targetFont)
                {
                    continue;
                }

                if (m_previewOnly)
                {
                    UnityEngine.Debug.Log($"[{nameof(FontReplacerWindow)}] [Scene] Would replace on '{component.gameObject.name}' ({component.GetType().Name})");
                }
                else
                {
                    Undo.RecordObject(component, "Replace TMP Font");
                    component.font = m_targetFont;
                    EditorUtility.SetDirty(component);
                }

                count++;
            }

            if (!m_previewOnly && count > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            return count;
        }

        private int ReplaceInPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                count += ReplaceInPrefab(path);
            }

            if (!m_previewOnly && count > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return count;
        }

        private int ReplaceInPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return 0;
            }

            TMP_Text[] components = prefab.GetComponentsInChildren<TMP_Text>(true);
            int count = 0;

            foreach (TMP_Text component in components)
            {
                if (component.font == m_targetFont)
                {
                    continue;
                }

                if (m_previewOnly)
                {
                    UnityEngine.Debug.Log($"[{nameof(FontReplacerWindow)}] [Prefab] Would replace on '{component.gameObject.name}' in {prefabPath}");
                }
                else
                {
                    component.font = m_targetFont;
                    EditorUtility.SetDirty(component);
                }

                count++;
            }

            return count;
        }
    }
}
