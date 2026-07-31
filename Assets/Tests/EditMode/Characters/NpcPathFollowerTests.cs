using BigRetail.Characters.Rigging;
using NUnit.Framework;
using UnityEngine;

namespace BigRetail.Characters.Rigging.Tests
{
    public sealed class NpcPathFollowerTests
    {
        [Test]
        public void Tick_MovesTowardWaypointAndStopsAtEnd()
        {
            GameObject character = new GameObject("NPC Path Test");

            try
            {
                NpcPathFollower follower = character.AddComponent<NpcPathFollower>();
                follower.ConfigurePrototype(2f, 0.01f, 1f);
                follower.SetPath(new[] { new Vector3(1f, 0f, 0f) });

                follower.Tick(0.25f);

                Assert.That(character.transform.position.x, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(follower.IsMoving, Is.True);

                follower.Tick(0.25f);

                Assert.That(character.transform.position, Is.EqualTo(new Vector3(1f, 0f, 0f)));
                Assert.That(follower.IsMoving, Is.False);
                Assert.That(follower.Velocity, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void SetPath_CopiesWaypointsAndCanBeStopped()
        {
            GameObject character = new GameObject("NPC Path Test");

            try
            {
                NpcPathFollower follower = character.AddComponent<NpcPathFollower>();
                Vector3[] path =
                {
                    new Vector3(0f, 0f, 1f),
                    new Vector3(0f, 0f, 2f)
                };

                follower.SetPath(path);
                path[0] = new Vector3(99f, 99f, 99f);
                follower.Tick(0.25f);

                Assert.That(character.transform.position.z, Is.EqualTo(0.3f).Within(0.0001f));

                follower.Stop();
                follower.Tick(1f);

                Assert.That(follower.IsMoving, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(character);
            }
        }

        [TestCase(1f, -1f, NpcFacing.SouthEast)]
        [TestCase(-1f, -1f, NpcFacing.SouthWest)]
        [TestCase(1f, 1f, NpcFacing.NorthEast)]
        [TestCase(-1f, 1f, NpcFacing.NorthWest)]
        public void Tick_UpdatesFacingFromIsometricMapPlane(
            float x,
            float y,
            NpcFacing expectedFacing)
        {
            GameObject character = new GameObject("NPC Facing Test");

            try
            {
                NpcCutoutRig rig = character.AddComponent<NpcCutoutRig>();
                NpcPathFollower follower = character.AddComponent<NpcPathFollower>();
                follower.ConfigurePrototype(1f, 0.01f, 1f);
                follower.SetPath(new[] { new Vector3(x, y, 0f) });

                follower.Tick(0.25f);

                Assert.That(rig.Facing, Is.EqualTo(expectedFacing));
            }
            finally
            {
                Object.DestroyImmediate(character);
            }
        }
    }
}
