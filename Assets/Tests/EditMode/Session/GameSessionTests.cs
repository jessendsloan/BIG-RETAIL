using System;
using BigRetail.Core.Session;
using NUnit.Framework;
using UnityEditor;

namespace BigRetail.Session.Tests
{
    public sealed class GameSessionTests
    {
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
        public void FrankRoadsideOpeningAdvancesThroughItsTwoBeats()
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
                session.FrankRoadsideOpening.IsComplete,
                Is.True);
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
    }
}
