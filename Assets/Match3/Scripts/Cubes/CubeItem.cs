using System;
using MatchGame.Scripts.Services;
using DG.Tweening;
using UnityEngine;

namespace MatchGame.Scripts.Cubes
{
    public abstract class CubeItem : MonoBehaviour, IPoolable
    {
        private bool _isObstacle;

        public bool IsObstacle
        {
            get => _isObstacle;
            set => _isObstacle = value;
        }

        private int _health;

        public int Health
        {
            get => _health;
            set => _health = value;
        }


        public void TakeDamage(Action onDied = null, int damage = 1)
        {
            _health -= damage;

            if (_health <= 0)
            {
                PoolingSystem.Instance.Destroy(PoolInstanceId, gameObject);
                onDied?.Invoke();
            }
        }

        public abstract string Id { get; }


        public void OnGetFromPool()
        {
        }

        public void OnReturnToPool()
        {
        }

        public abstract string PoolInstanceId { get; set; }
    }
}