using System;
using System.Collections.Generic;
using System.Linq;
using CupkekGames.Character.Editor;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CupkekGames.Character.Tests
{
  public class FaceControllerTests
  {
    private readonly List<Object> _created = new();
    private GameObject _root;
    private FaceController _face;

    [SetUp]
    public void SetUp()
    {
      _root = Track(new GameObject("Character"));
    }

    [TearDown]
    public void TearDown()
    {
      foreach (Object created in _created)
      {
        if (created != null) Object.DestroyImmediate(created);
      }

      _created.Clear();
    }

    // ── Rig ──

    [Test]
    public void OneChannelDrivesEveryMeshThatHasIt_WhateverTheShapeOrder()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "jawOpen", "eyeBlinkLeft", "eyeBlinkRight", "mouthSmileLeft");
      SkinnedMeshRenderer teeth = AddMesh("Teeth", "mouthSmileLeft", "jawOpen");
      CreateFace();

      _face.Snap(Expression(("jawOpen", 40f)));

      Assert.AreEqual(40f, Weight(face, "jawOpen"), 0.001f);
      Assert.AreEqual(40f, Weight(teeth, "jawOpen"), 0.001f);
      Assert.AreEqual(0f, Weight(teeth, "mouthSmileLeft"), 0.001f);
    }

    [Test]
    public void MidShapesAreNotChannels_AndOrphansAreReported()
    {
      AddMesh("Face", "eyeBlinkLeft", "eyeBlinkLeft_mid", "browDown_mid");
      CreateFace();

      Assert.IsTrue(_face.HasChannel("eyeBlinkLeft"));
      Assert.IsFalse(_face.HasChannel("eyeBlinkLeft_mid"));
      Assert.IsTrue(_face.Rig.HasMid("eyeBlinkLeft"));
      CollectionAssert.AreEqual(new[] { "browDown_mid" }, _face.Rig.OrphanMidKeys);
    }

    // ── Expression layer ──

    [Test]
    public void ChannelsAnExpressionDoesNotList_AreZero()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "mouthSmileLeft", "browDownLeft");
      CreateFace();

      _face.Snap(Expression(("mouthSmileLeft", 80f)));
      _face.Snap(Expression(("browDownLeft", 50f)));

      Assert.AreEqual(0f, Weight(face, "mouthSmileLeft"), 0.001f);
      Assert.AreEqual(50f, Weight(face, "browDownLeft"), 0.001f);
    }

    [Test]
    public void ChannelsNothingTouches_KeepTheirValue()
    {
      SkinnedMeshRenderer body = AddMesh("Body", "morph_Male");
      AddMesh("Face", "mouthSmileLeft");
      body.SetBlendShapeWeight(0, 30f);
      CreateFace();

      _face.Snap(Expression(("mouthSmileLeft", 80f)));

      Assert.AreEqual(30f, Weight(body, "morph_Male"), 0.001f);
    }

    [Test]
    public void Crossfade_IsHalfwayAtTheMidpoint()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "jawOpen");
      CreateFace();
      _face.Snap(Expression());

      _face.PlayAt(Expression(("jawOpen", 100f)), 0f, 1f, 10f);
      _face.Evaluate(10.5f);
      Assert.AreEqual(50f, Weight(face, "jawOpen"), 0.001f);

      _face.Evaluate(11f);
      Assert.AreEqual(100f, Weight(face, "jawOpen"), 0.001f);
    }

    [Test]
    public void ANewFade_StartsFromWhereTheLastOneGot()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "jawOpen");
      CreateFace();
      _face.Snap(Expression());

      _face.PlayAt(Expression(("jawOpen", 100f)), 0f, 1f, 0f);
      _face.Evaluate(0.5f);
      _face.PlayAt(Expression(), 0f, 1f, 0.5f);
      _face.Evaluate(0.5f);

      Assert.AreEqual(50f, Weight(face, "jawOpen"), 0.001f);
    }

    [Test]
    public void ATimedExpression_ReturnsToRest()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "jawOpen", "mouthSmileLeft");
      CreateFace();
      _face.SetRest(Expression(("mouthSmileLeft", 10f)));
      _face.ApplyNow();

      _face.PlayAt(Expression(("jawOpen", 60f)), 2f, 0f, 5f);
      _face.Evaluate(6f);
      Assert.AreEqual(60f, Weight(face, "jawOpen"), 0.001f);
      Assert.AreEqual(0f, Weight(face, "mouthSmileLeft"), 0.001f);

      _face.Evaluate(7.01f);
      Assert.AreEqual(0f, Weight(face, "jawOpen"), 0.001f);
      Assert.AreEqual(10f, Weight(face, "mouthSmileLeft"), 0.001f);
    }

    [Test]
    public void SetRest_FadesAResting_Face()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "mouthFrownLeft");
      CreateFace();
      _face.SetRest(Expression());
      _face.ApplyNow();

      _face.SetRest(Expression(("mouthFrownLeft", 40f)));
      _face.ApplyNow();

      Assert.AreEqual(40f, Weight(face, "mouthFrownLeft"), 0.001f);
    }

    [Test]
    public void PlayingNothing_Throws()
    {
      AddMesh("Face", "jawOpen");
      CreateFace();

      Assert.Throws<ArgumentNullException>(() => _face.Play(null));
    }

    // ── Blink and mid ──

    [Test]
    public void Blink_ClosesTheEyesOnAFaceThatAllowsIt()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "eyeBlinkLeft", "eyeBlinkRight");
      CreateFace();
      _face.Snap(Expression(("eyeBlinkLeft", 20f)));

      _face.Blink01 = 0.5f;
      _face.ApplyNow();

      Assert.AreEqual(60f, Weight(face, "eyeBlinkLeft"), 0.001f);
      Assert.AreEqual(50f, Weight(face, "eyeBlinkRight"), 0.001f);
    }

    [Test]
    public void Blink_IsSuppressedByAFaceThatSaysSo()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "eyeBlinkLeft", "eyeBlinkRight");
      CreateFace();
      _face.Snap(Expression(FaceBlinkPolicy.Suppress, ("eyeBlinkLeft", 30f)));

      _face.Blink01 = 1f;
      _face.ApplyNow();

      Assert.AreEqual(30f, Weight(face, "eyeBlinkLeft"), 0.001f);
      Assert.AreEqual(0f, Weight(face, "eyeBlinkRight"), 0.001f);
    }

    [TestCase(0f, 0f)]
    [TestCase(25f, 50f)]
    [TestCase(50f, 100f)]
    [TestCase(100f, 0f)]
    public void Mid_FollowsTheTriangle(float weight, float expectedMid)
    {
      SkinnedMeshRenderer face = AddMesh("Face", "eyeBlinkLeft", "eyeBlinkLeft_mid");
      CreateFace();

      _face.Snap(Expression(("eyeBlinkLeft", weight)));

      Assert.AreEqual(expectedMid, Weight(face, "eyeBlinkLeft_mid"), 0.001f);
    }

    [Test]
    public void Mid_FollowsTheBlinkedWeight()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "eyeBlinkLeft", "eyeBlinkLeft_mid");
      CreateFace();
      _face.Snap(Expression());

      _face.Blink01 = 0.5f;
      _face.ApplyNow();

      Assert.AreEqual(100f, Weight(face, "eyeBlinkLeft_mid"), 0.001f);
    }

    // ── Validator ──

    [Test]
    public void Validator_FlagsAMeshNamedLikeABone()
    {
      SkinnedMeshRenderer face = AddMesh("Head", "eyeBlinkLeft", "eyeBlinkRight");
      Transform bone = Track(new GameObject("Head")).transform;
      face.bones = new[] { bone };
      CreateFace();

      Assert.IsTrue(Errors().Any(message => message.Contains("shares its name with a bone")));
    }

    [Test]
    public void Validator_FlagsAShapeExportedNonZero()
    {
      SkinnedMeshRenderer face = AddMesh("Face", "eyeBlinkLeft", "eyeBlinkRight", "mouthShrugLower");
      face.SetBlendShapeWeight(2, 41.7f);
      CreateFace();

      Assert.IsTrue(Errors().Any(message => message.Contains("'mouthShrugLower' rests at 41.7")));
    }

    [Test]
    public void Validator_FlagsAMissingBlinkChannelAndAnOrphanMid()
    {
      AddMesh("Face", "eyeBlinkLeft", "jawOpen_mid");
      CreateFace();

      List<string> errors = Errors();
      Assert.IsTrue(errors.Any(message => message.Contains("Blink channel 'eyeBlinkRight'")));
      Assert.IsTrue(errors.Any(message => message.Contains("'jawOpen_mid' has no base shape")));
    }

    [Test]
    public void Validator_WarnsWhenAnExpressionMovesChannelsTheFaceLacks()
    {
      AddMesh("Face", "eyeBlinkLeft", "eyeBlinkRight");
      CreateFace();

      List<FaceRigIssue> issues = FaceRigValidator.Check(_root, new[] { Expression(("eyeHappyLeft", 100f)) });

      Assert.IsTrue(issues.Any(issue =>
        issue.Severity == FaceRigIssueSeverity.Warning && issue.Message.Contains("eyeHappyLeft")));
    }

    // ── Helpers ──

    private T Track<T>(T created) where T : Object
    {
      _created.Add(created);
      return created;
    }

    private void CreateFace()
    {
      _face = _root.AddComponent<FaceController>();
    }

    private SkinnedMeshRenderer AddMesh(string name, params string[] shapes)
    {
      var go = new GameObject(name);
      go.transform.SetParent(_root.transform);

      var mesh = Track(new Mesh
      {
        vertices = new[] { Vector3.zero, Vector3.up, Vector3.right },
        triangles = new[] { 0, 1, 2 },
      });
      var deltas = new Vector3[3];
      foreach (string shape in shapes) mesh.AddBlendShapeFrame(shape, 100f, deltas, null, null);

      var renderer = go.AddComponent<SkinnedMeshRenderer>();
      renderer.sharedMesh = mesh;
      return renderer;
    }

    private FaceExpressionSO Expression(params (string name, float weight)[] weights) =>
      Expression(FaceBlinkPolicy.Allow, weights);

    private FaceExpressionSO Expression(FaceBlinkPolicy blink, params (string name, float weight)[] weights)
    {
      var expression = Track(ScriptableObject.CreateInstance<FaceExpressionSO>());
      expression.Configure(weights.Select(w => new FaceWeight(w.name, w.weight)), blink, 0f);
      return expression;
    }

    private static float Weight(SkinnedMeshRenderer renderer, string shape) =>
      renderer.GetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(shape));

    private List<string> Errors() =>
      FaceRigValidator.Check(_root)
        .Where(issue => issue.Severity == FaceRigIssueSeverity.Error)
        .Select(issue => issue.Message)
        .ToList();
  }
}
