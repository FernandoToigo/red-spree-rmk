using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class GameTests
{
    [SetUp]
    public void Initialize()
    {
        const string gameScenePath = "Assets\\Scenes\\Gameplay.unity";
        const string definitionsPath = "Assets\\Definitions\\Definitions.asset";
        EditorSceneManager.OpenScene(gameScenePath);
        var references = Object.FindObjectOfType<References>();
        var definitions = AssetDatabase.LoadAssetAtPath<Definitions>(definitionsPath);
        UnityEngine.Random.InitState(123);
        Game.Initialize(references, definitions);
    }

    private Game.Report RunFrame(Game.Input input)
    {
        var deltaSeconds = Time.fixedDeltaTime;
        return Game.Update(input, new FrameTime
        {
            DeltaSeconds = deltaSeconds,
        });
    }

    private void RunFramesUntil(Game.Input input, Func<Game.Report, bool> stopFunction)
    {
        while (true)
        {
            var report = RunFrame(input);

            if (stopFunction(report))
            {
                return;
            }

            if (Game.State.SecondsPassed > 100f)
            {
                Assert.Fail("Test took too long to complete.");
            }
        }
    }

    private static void AssertPlayerIsAlive()
    {
        Assert.IsTrue(!Game.State.IsDead, "Player has died.");
    }

    [Test]
    public void ShootAtZombie_ZombiesGetKilled()
    {
        for (var i = 0; i < 5; i++)
        {
            RunFramesUntil(new Game.Input(), report =>
            {
                AssertPlayerIsAlive();
                return report.SpawnedZombies > 0;
            });
            RunFrame(new Game.Input
            {
                FireStraight = true
            });
            AssertPlayerIsAlive();
        }
        
        RunFramesUntil(new Game.Input(), _ =>
        {
            AssertPlayerIsAlive();
            return Game.State.EnemiesKilled >= 5;
        });
    }
}