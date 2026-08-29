using System;
using BigRetail.Core.Session;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BigRetail.Session.Tests
{
    public sealed class GameSessionTests
    {
        private const string StartScreenScenePath =
            "Assets/Scenes/StartScreen.unity";

        private const string FrankRoadsideScenePath =
            "Assets/Scenes/FrankRoadside.unity";

        [TearDown]
        public void TearDown()
        {
            DevelopmentSessionBootstrap.ClearRequest();
        }

        [Test]
        public void CampaignSessionIdentifiesAsCampaign()
        {
            var session = new GameSession(GameMode.Campaign);

            Assert.That(session.Mode, Is.EqualTo(GameMode.Campaign));
            Assert.That(session.IsCampaign, Is.True);
            Assert.That(session.IsSandbox, Is.False);
        }

        [Test]
        public void SandboxSessionIdentifiesAsSandbox()
        {
            var session = new GameSession(GameMode.Sandbox);

            Assert.That(session.Mode, Is.EqualTo(GameMode.Sandbox));
            Assert.That(session.IsCampaign, Is.False);
            Assert.That(session.IsSandbox, Is.True);
        }

        [Test]
        public void StartScreenRoutesCampaignToFrankRoadside()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    StartScreenScenePath,
                    OpenSceneMode.Single);
                GameSessionHost sessionHost =
                    FindSceneComponent<GameSessionHost>(scene);

                Assert.That(sessionHost, Is.Not.Null);
                Assert.That(
                    sessionHost.GetStartingSceneName(GameMode.Campaign),
                    Is.EqualTo("FrankRoadside"));
                Assert.That(
                    sessionHost.GetStartingSceneName(GameMode.Sandbox),
                    Is.EqualTo("Gameplay"));
                Assert.That(
                    IsEnabledBuildScene(FrankRoadsideScenePath),
                    Is.True,
                    "Frank Roadside must be enabled in Build Settings before "
                    + "the Campaign button can load it.");
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(
                        previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }

        [Test]
        public void CampaignOpeningAdvancesThroughItsThreeBeats()
        {
            var session = new GameSession(GameMode.Campaign);

            Assert.That(
                session.CampaignOpening.CurrentBeat,
                Is.EqualTo(CampaignOpeningBeat.Opportunity));

            session.CampaignOpening.Advance();
            Assert.That(
                session.CampaignOpening.CurrentBeat,
                Is.EqualTo(CampaignOpeningBeat.Financing));

            session.CampaignOpening.Advance();
            Assert.That(
                session.CampaignOpening.CurrentBeat,
                Is.EqualTo(CampaignOpeningBeat.FirstAssignment));

            session.CampaignOpening.Advance();
            Assert.That(session.CampaignOpening.IsComplete, Is.True);
        }

        [Test]
        public void CampaignOpeningCanBeSkipped()
        {
            var session = new GameSession(GameMode.Campaign);

            session.CampaignOpening.Skip();

            Assert.That(session.CampaignOpening.IsComplete, Is.True);
            Assert.That(
                session.CampaignOpening.CurrentBeat,
                Is.EqualTo(CampaignOpeningBeat.Complete));
        }

        [Test]
        public void FrankRoadsideOpeningAdvancesThroughItsThreeBeats()
        {
            var session = new GameSession(GameMode.Campaign);

            Assert.That(
                session.FrankRoadsideOpening.CurrentBeat,
                Is.EqualTo(FrankRoadsideOpeningBeat.WakeUp));

            session.FrankRoadsideOpening.Advance();
            Assert.That(
                session.FrankRoadsideOpening.CurrentBeat,
                Is.EqualTo(FrankRoadsideOpeningBeat.CoverTheStore));

            session.FrankRoadsideOpening.Advance();
            Assert.That(
                session.FrankRoadsideOpening.CurrentBeat,
                Is.EqualTo(
                    FrankRoadsideOpeningBeat.MoveReceivingToStockroom));

            session.FrankRoadsideOpening.Advance();
            Assert.That(session.FrankRoadsideOpening.IsComplete, Is.True);
        }

        [Test]
        public void FrankRoadsideOpeningCanBeSkipped()
        {
            var session = new GameSession(GameMode.Campaign);

            session.FrankRoadsideOpening.Skip();

            Assert.That(session.FrankRoadsideOpening.IsComplete, Is.True);
            Assert.That(
                session.FrankRoadsideOpening.CurrentBeat,
                Is.EqualTo(FrankRoadsideOpeningBeat.Complete));
        }

        [Test]
        public void DevelopmentQuickStartArmsRequestedMode()
        {
            DevelopmentSessionBootstrap.Arm(GameMode.Campaign);

            Assert.That(
                EditorPrefs.GetBool(
                    DevelopmentSessionBootstrap.ArmedEditorPreference),
                Is.True);
            Assert.That(
                EditorPrefs.GetInt(
                    DevelopmentSessionBootstrap.ModeEditorPreference),
                Is.EqualTo((int)GameMode.Campaign));
            Assert.That(
                EditorPrefs.GetBool(
                    DevelopmentSessionBootstrap
                        .WorkshopEditorPreference),
                Is.False);
        }


        [Test]
        public void MapWorkshopArmsSandboxWithWorkshopFlag()
        {
            DevelopmentSessionBootstrap.ArmMapWorkshop();

            Assert.That(
                EditorPrefs.GetBool(
                    DevelopmentSessionBootstrap.ArmedEditorPreference),
                Is.True);
            Assert.That(
                EditorPrefs.GetInt(
                    DevelopmentSessionBootstrap.ModeEditorPreference),
                Is.EqualTo((int)GameMode.Sandbox));
            Assert.That(
                EditorPrefs.GetBool(
                    DevelopmentSessionBootstrap
                        .WorkshopEditorPreference),
                Is.True);
        }

        [Test]
        public void DevelopmentQuickStartRejectsUnknownMode()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => DevelopmentSessionBootstrap.Arm((GameMode)999));
        }

        private static bool IsEnabledBuildScene(string scenePath)
        {
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;

            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].enabled
                    && scenes[index].path == scenePath)
                {
                    return true;
                }
            }

            return false;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int index = 0; index < roots.Length; index++)
            {
                T component =
                    roots[index].GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
