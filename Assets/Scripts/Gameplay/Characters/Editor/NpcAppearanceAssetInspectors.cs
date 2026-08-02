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
                "The central appearance catalog for Population Studio. " +
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
            EditorGUILayout.HelpBox(
                "A population definition. Customer definitions should " +
                "contain customer clothing; Employee definitions should " +
                "contain approved uniforms. Weight controls how often an " +
                "allowed option appears during random generation.",
                MessageType.Info);

            DrawDefaultInspector();

            NpcPopulationDefinition definition =
                (NpcPopulationDefinition)target;

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
                "A body silhouette changes the proportions and spacing " +
                "of the same shared skeleton. Duplicate a starter body " +
                "before editing. Keep all 18 part-shape entries.",
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
                "colors the badge. Each Part Style may optionally swap " +
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
                "later without changing the recipe system.",
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
                "This is the final person recipe: one body, one skin " +
                "palette, one outfit, and one hair set. It references " +
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
