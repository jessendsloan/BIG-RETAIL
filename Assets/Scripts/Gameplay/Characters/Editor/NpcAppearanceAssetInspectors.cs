using BigRetail.Characters.Rigging;
using UnityEditor;

namespace BigRetail.Characters.Editor
{
    [CustomEditor(typeof(NpcAppearanceCatalog))]
    public sealed class NpcAppearanceCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "The central appearance catalog for Population Definitions. " +
                "Population definitions control what gameplay may generate; the lists " +
                "register every reusable appearance asset available for " +
                "authoring.",
                MessageType.Info);

            DrawDefaultInspector();

            NpcAppearanceCatalog catalog =
                (NpcAppearanceCatalog)target;

            if (catalog.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Appearance catalog is ready.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }
    }


    [CustomEditor(typeof(NpcPopulationDefinition))]
    public sealed class NpcPopulationDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            NpcPopulationDefinition definition =
                (NpcPopulationDefinition)target;

            if (definition.EnsureGenderAppearancePools())
            {
                EditorUtility.SetDirty(definition);
                serializedObject.Update();
            }

            EditorGUILayout.HelpBox(
                "A population definition controls Men/Women weights and " +
                "separate Body, Skin, Outfit, and Hair pools for one " +
                "gameplay population. " +
                "Use the Population Definitions window for normal editing.",
                MessageType.Info);

            DrawDefaultInspector();

            if (definition.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Population definition is ready for generation.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }
    }


    [CustomEditor(typeof(NpcBodySilhouette))]
    public sealed class NpcBodySilhouetteInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "A body silhouette defines Man or Woman and changes safe " +
                "proportions and spacing on the shared skeleton. Use the " +
                "Appearance Creator for normal editing. Keep all 18 " +
                "part-shape entries.",
                MessageType.Info);

            DrawDefaultInspector();
            DrawValidation((NpcBodySilhouette)target);
        }


        private static void DrawValidation(
            NpcBodySilhouette silhouette)
        {
            if (silhouette.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Body contract complete: 18/18 parts.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }
    }


    [CustomEditor(typeof(NpcSkinPalette))]
    public sealed class NpcSkinPaletteInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "The base color is used by the head, neck, and hands. " +
                "The rig automatically makes the farther side slightly " +
                "darker for readable isometric depth.",
                MessageType.Info);

            DrawDefaultInspector();
        }
    }


    [CustomEditor(typeof(NpcOutfitSet))]
    public sealed class NpcOutfitSetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "An outfit is a coordinated clothing recipe. Primary " +
                "fabric usually colors the shirt and sleeves; secondary " +
                "fabric colors trousers; footwear colors shoes; accent " +
                "colors the badge. Its compatibility determines whether " +
                "it may be generated for men, women, or everyone. Each " +
                "Part Style may optionally swap " +
                "in SouthEast and NorthEast sprites for painted clothing.",
                MessageType.Info);

            DrawDefaultInspector();

            NpcOutfitSet outfit = (NpcOutfitSet)target;

            if (outfit.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Outfit contract complete.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }
    }


    [CustomEditor(typeof(NpcHairSet))]
    public sealed class NpcHairSetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "A hair set owns coordinated rear and front hair pieces. " +
                "Their shapes create simple procedural haircuts; optional " +
                "SouthEast and NorthEast sprites can replace those shapes " +
                "later without changing the recipe system. Compatibility " +
                "controls whether it may be used for men, women, or both.",
                MessageType.Info);

            DrawDefaultInspector();

            NpcHairSet hair = (NpcHairSet)target;

            if (hair.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Hair contract complete.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }
    }


    [CustomEditor(typeof(NpcAppearanceProfile))]
    public sealed class NpcAppearanceProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "This is the final person recipe: Man or Woman, one body, " +
                "one skin palette, one outfit, and one hair set. It references " +
                "shared assets rather than copying art or animations.",
                MessageType.Info);

            DrawDefaultInspector();

            NpcAppearanceProfile profile =
                (NpcAppearanceProfile)target;

            if (profile.TryValidate(out string reason))
            {
                EditorGUILayout.HelpBox(
                    "Appearance recipe complete.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(reason, MessageType.Error);
            }
        }
    }
}
