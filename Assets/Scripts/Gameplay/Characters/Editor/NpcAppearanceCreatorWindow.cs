using BigRetail.Characters.Rigging;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    public sealed class NpcAppearanceCreatorWindow : EditorWindow
    {
        private const string MenuPath =
            "Big Retail/Characters/Appearance Creator/Open Creator";

        private string personName = "New Person";
        private NpcBodySilhouette bodySilhouette;
        private NpcSkinPalette skinPalette;
        private NpcOutfitSet outfitSet;
        private NpcHairSet hairSet;
        private NpcAppearanceProfile selectedProfile;
        private NpcCutoutRig previewRig;
        private Vector2 scrollPosition;


        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<NpcAppearanceCreatorWindow>(
                "Person Creator");
        }


        private void OnEnable()
        {
            TryUseSelection();
        }


        private void OnSelectionChange()
        {
            TryUseSelection();
            Repaint();
        }


        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Big Retail Person Creator",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Compose one person from four independent choices. " +
                "The resulting profile reuses the shared rig and " +
                "animations.",
                MessageType.Info);

            DrawStarterLibrarySection();
            EditorGUILayout.Space(8f);
            DrawRecipeSection();
            EditorGUILayout.Space(8f);
            DrawPreviewSection();

            EditorGUILayout.EndScrollView();
        }


        private void DrawStarterLibrarySection()
        {
            EditorGUILayout.LabelField(
                "Starter Library",
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "Create / Refresh Starter Appearance Library"))
            {
                NpcAppearanceStarterFactory
                    .CreateOrUpdateStarterLibrary();
                TryUseSelection();
            }

            EditorGUILayout.LabelField(
                "This safely captures Rowan, creates starter choices, " +
                "and builds a second profile for comparison.",
                EditorStyles.wordWrappedMiniLabel);
        }


        private void DrawRecipeSection()
        {
            EditorGUILayout.LabelField(
                "Appearance Recipe",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            selectedProfile =
                (NpcAppearanceProfile)EditorGUILayout.ObjectField(
                    "Saved Profile",
                    selectedProfile,
                    typeof(NpcAppearanceProfile),
                    false);

            if (EditorGUI.EndChangeCheck()
                && selectedProfile != null)
            {
                LoadRecipe(selectedProfile);
            }

            personName = EditorGUILayout.TextField(
                "Person Name",
                personName);

            bodySilhouette =
                (NpcBodySilhouette)EditorGUILayout.ObjectField(
                    "Body Silhouette",
                    bodySilhouette,
                    typeof(NpcBodySilhouette),
                    false);

            skinPalette =
                (NpcSkinPalette)EditorGUILayout.ObjectField(
                    "Skin Palette",
                    skinPalette,
                    typeof(NpcSkinPalette),
                    false);

            outfitSet =
                (NpcOutfitSet)EditorGUILayout.ObjectField(
                    "Outfit Set",
                    outfitSet,
                    typeof(NpcOutfitSet),
                    false);

            hairSet =
                (NpcHairSet)EditorGUILayout.ObjectField(
                    "Hair Set",
                    hairSet,
                    typeof(NpcHairSet),
                    false);

            using (new EditorGUI.DisabledScope(
                       !HasCompleteRecipe()))
            {
                if (GUILayout.Button("Save New Appearance Profile"))
                {
                    SaveNewProfile();
                }

                using (new EditorGUI.DisabledScope(
                           selectedProfile == null))
                {
                    if (GUILayout.Button(
                            "Update Selected Profile"))
                    {
                        UpdateSelectedProfile();
                    }
                }
            }

            DrawRecipeValidation();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Make New Choices",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.HelpBox(
                "Duplicate a working choice, rename the copy, then " +
                "edit its colors, shapes, or optional sprites in the " +
                "Inspector. This keeps all required body-part rules " +
                "intact.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(
                       bodySilhouette == null))
            {
                if (GUILayout.Button("Duplicate Body"))
                {
                    bodySilhouette = DuplicateAsset(
                        bodySilhouette,
                        "Body");
                }
            }

            using (new EditorGUI.DisabledScope(
                       skinPalette == null))
            {
                if (GUILayout.Button("Duplicate Skin"))
                {
                    skinPalette = DuplicateAsset(
                        skinPalette,
                        "Skin");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(outfitSet == null))
            {
                if (GUILayout.Button("Duplicate Outfit"))
                {
                    outfitSet = DuplicateAsset(
                        outfitSet,
                        "Outfit");
                }
            }

            using (new EditorGUI.DisabledScope(hairSet == null))
            {
                if (GUILayout.Button("Duplicate Hair"))
                {
                    hairSet = DuplicateAsset(
                        hairSet,
                        "Hair");
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField(
                "Live Rig Preview",
                EditorStyles.boldLabel);

            previewRig =
                (NpcCutoutRig)EditorGUILayout.ObjectField(
                    "Rig",
                    previewRig,
                    typeof(NpcCutoutRig),
                    true);

            using (new EditorGUI.DisabledScope(
                       previewRig == null
                       || selectedProfile == null))
            {
                if (GUILayout.Button(
                        "Apply Saved Profile To Rig"))
                {
                    ApplyProfileToRig();
                }
            }

            using (new EditorGUI.DisabledScope(previewRig == null))
            {
                EditorGUILayout.LabelField("Facing");
                EditorGUILayout.BeginHorizontal();

                DrawFacingButton("SE", NpcFacing.SouthEast);
                DrawFacingButton("SW", NpcFacing.SouthWest);
                DrawFacingButton("NE", NpcFacing.NorthEast);
                DrawFacingButton("NW", NpcFacing.NorthWest);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox(
                "Open a character prefab or select a scene instance, " +
                "then use the four facing buttons to inspect the recipe. " +
                "The Animator and movement components are not replaced.",
                MessageType.None);
        }


        private void DrawFacingButton(
            string label,
            NpcFacing facing)
        {
            if (!GUILayout.Button(label))
            {
                return;
            }

            Undo.RecordObject(
                previewRig,
                "Preview NPC Facing");

            previewRig.SetFacing(facing);
            MarkRigDirty(previewRig);
        }


        private void ApplyProfileToRig()
        {
            Undo.RecordObject(
                previewRig,
                "Apply NPC Appearance Profile");

            previewRig.SetAppearanceProfile(selectedProfile);
            MarkRigDirty(previewRig);
            SceneView.RepaintAll();
        }


        private void UpdateSelectedProfile()
        {
            Undo.RecordObject(
                selectedProfile,
                "Update NPC Appearance Profile");

            selectedProfile.Configure(
                personName,
                bodySilhouette,
                skinPalette,
                outfitSet,
                hairSet);

            EditorUtility.SetDirty(selectedProfile);
            AssetDatabase.SaveAssets();

            if (previewRig != null)
            {
                ApplyProfileToRig();
            }
        }


        private void SaveNewProfile()
        {
            string defaultName =
                string.IsNullOrWhiteSpace(personName)
                    ? "PersonAppearance"
                    : personName.Replace(" ", string.Empty)
                        + "Appearance";

            string assetPath =
                EditorUtility.SaveFilePanelInProject(
                    "Save Person Appearance",
                    defaultName,
                    "asset",
                    "Choose where to save the appearance profile.",
                    "Assets/Art/Characters/Appearance/Profiles");

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            NpcAppearanceProfile profile =
                CreateInstance<NpcAppearanceProfile>();

            profile.Configure(
                personName,
                bodySilhouette,
                skinPalette,
                outfitSet,
                hairSet);

            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();

            selectedProfile = profile;
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }


        private void LoadRecipe(
            NpcAppearanceProfile profile)
        {
            personName = profile.DisplayName;
            bodySilhouette = profile.BodySilhouette;
            skinPalette = profile.SkinPalette;
            outfitSet = profile.OutfitSet;
            hairSet = profile.HairSet;
        }


        private bool HasCompleteRecipe()
        {
            return bodySilhouette != null
                && skinPalette != null
                && outfitSet != null
                && hairSet != null;
        }


        private void DrawRecipeValidation()
        {
            if (!HasCompleteRecipe())
            {
                EditorGUILayout.HelpBox(
                    "Choose one item in all four recipe slots.",
                    MessageType.Warning);
                return;
            }

            if (!bodySilhouette.TryValidate(out string reason)
                || !outfitSet.TryValidate(out reason)
                || !hairSet.TryValidate(out reason))
            {
                EditorGUILayout.HelpBox(
                    reason,
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "Complete recipe: body + skin + outfit + hair.",
                MessageType.None);
        }


        private void TryUseSelection()
        {
            if (Selection.activeObject
                is NpcAppearanceProfile profile)
            {
                selectedProfile = profile;
                LoadRecipe(profile);
                return;
            }

            if (Selection.activeGameObject != null)
            {
                NpcCutoutRig selectedRig =
                    Selection.activeGameObject
                        .GetComponentInParent<NpcCutoutRig>();

                if (selectedRig != null)
                {
                    previewRig = selectedRig;

                    if (selectedRig.AppearanceProfile != null)
                    {
                        selectedProfile =
                            selectedRig.AppearanceProfile;
                        LoadRecipe(selectedProfile);
                    }
                }
            }
        }


        private static void MarkRigDirty(
            NpcCutoutRig rig)
        {
            EditorUtility.SetDirty(rig);

            if (PrefabUtility.IsPartOfPrefabInstance(rig))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    rig);
            }
        }


        private static T DuplicateAsset<T>(
            T source,
            string kind)
            where T : ScriptableObject
        {
            if (source == null)
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string directory = string.IsNullOrWhiteSpace(sourcePath)
                ? "Assets/Art/Characters/Appearance"
                : System.IO.Path.GetDirectoryName(sourcePath)
                    ?.Replace('\\', '/');

            string defaultName = source.name + " Copy";
            string destination = EditorUtility.SaveFilePanelInProject(
                $"Duplicate {kind}",
                defaultName,
                "asset",
                $"Create a new {kind.ToLowerInvariant()} choice " +
                "from this working template.",
                directory);

            if (string.IsNullOrWhiteSpace(destination))
            {
                return source;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destination))
            {
                Debug.LogError(
                    $"Could not duplicate '{sourcePath}' to " +
                    $"'{destination}'.");
                return source;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            T copy = AssetDatabase.LoadAssetAtPath<T>(destination);
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
            return copy;
        }
    }
}
