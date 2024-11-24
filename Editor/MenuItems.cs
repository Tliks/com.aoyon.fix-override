using System.Linq;
using UnityEngine;
using UnityEditor;

namespace com.aoyon.fix_override
{
    public static class MenuItems
    {
        private const string FIX_PROJECT_PATH = "Tools/Fix Overrides/Fix All in Project";
        private const string ENABLE_AUTO_PATH = "Tools/Fix Overrides/Enable Auto Fix";
        private const string FIX_PREFAB_PATH = "GameObject/Fix Overrides";
        private const string PREFERENCE_KEY = "com.aoyon.EnableFixOverrides";
        private static bool isEnabled;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.delayCall += () =>
            {
                isEnabled = EditorPrefs.GetBool(PREFERENCE_KEY, false);
                ToggleAutoFix(isEnabled);
            };
        }

        [MenuItem(ENABLE_AUTO_PATH)]
        private static void ToggleAutoFix()
        {
            isEnabled = !isEnabled;
            EditorPrefs.SetBool(PREFERENCE_KEY, isEnabled);
            ToggleAutoFix(isEnabled);
        }

        private static void ToggleAutoFix(bool isEnabled)
        {
            Menu.SetChecked(ENABLE_AUTO_PATH, isEnabled);

            if (isEnabled) {
                ObjectChangeEvents.changesPublished += OnObjectChange;
            }
            else {
                ObjectChangeEvents.changesPublished -= OnObjectChange;
            }
        }

        [MenuItem(FIX_PROJECT_PATH)]
        public static void RunForProject()
        {
            FixPrefabsInProject();
        }

        [MenuItem(FIX_PREFAB_PATH, true)]
        public static bool ValidateRunForPrefab()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(FIX_PREFAB_PATH, false)]
        public static void RunForPrefab()
        {
            FixPrefab(Selection.activeGameObject);
        }

        private static void OnObjectChange(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                {
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var data);
                    var obj = EditorUtility.InstanceIDToObject(data.instanceId);
                    if (obj == null) continue;
                    if (!PrefabUtility.IsPartOfAnyPrefab(obj)) continue;
                    RevertObjectOverride.Apply(obj);
                }
            }
        }

        private static void FixPrefab(GameObject prefab)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(prefab)) return;

            var objs = prefab.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject as Object)
                .Concat(prefab.GetComponentsInChildren<Component>(true));

            int totalReverts = 0;
            foreach (var obj in objs)
            {
                totalReverts += RevertObjectOverride.Apply(obj);
            }
            Debug.Log($"Revert {totalReverts} overrides in '{prefab.name}'.");
        }

        private static void FixPrefabsInProject()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    FixPrefab(prefab);
                }
            }
        }
    }
}