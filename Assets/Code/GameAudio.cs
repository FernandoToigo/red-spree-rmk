    using System.Collections.Generic;
    using UnityEngine;

    public static class GameAudio
    {
        private static References _references;
        private static State _state;
        
        public static void Initialize(References references)
        {
            _references = references;
            _state.BulletQueue = new Queue<AudioSource>(references.BulletAudioSources);
            _state.EnemyDeathQueue = new Queue<AudioSource>(references.EnemyDeathAudioSources);
        }
        
        public static void Update(Game.Report gameReport)
        {
            TryPlayBulletAudio(gameReport);
            TryPlayEnemyDeathAudio(gameReport);
        }

        private static void TryPlayBulletAudio(Game.Report gameReport)
        {
            if (!gameReport.FiredBullet)
            {
                return;
            }
            
            var audioSource = _state.BulletQueue.Dequeue();
            audioSource.Play();
            _state.BulletQueue.Enqueue(audioSource);
        }

        private static void TryPlayEnemyDeathAudio(Game.Report gameReport)
        {
            if (gameReport.KilledZombies <= 0 && gameReport.KilledVultures <= 0)
            {
                return;
            }
            
            var audioSource = _state.EnemyDeathQueue.Dequeue();
            audioSource.Play();
            _state.EnemyDeathQueue.Enqueue(audioSource);
        }
        
        private struct State
        {
            public Queue<AudioSource> BulletQueue;
            public Queue<AudioSource> EnemyDeathQueue;
        }
    }