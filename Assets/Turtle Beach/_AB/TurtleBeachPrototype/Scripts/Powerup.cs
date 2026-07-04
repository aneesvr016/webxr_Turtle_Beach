using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AB.TurtleBeach
{
    public class Powerup : MonoBehaviour, IObstacle
    {
        [SerializeField] string PowerupID;
        [SerializeField] GameObject powerupVisual;

        float _respawnTime = 5f;
        float _respawnCounter = 0f;

        bool _isVisible = true;

        public void DoCollision(TurtleController controller)
        {
            controller.EnableSpeedBoost();
            if (powerupVisual != null) powerupVisual.SetActive(false);
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        void Update()
        {
            if(powerupVisual != null && !powerupVisual.activeSelf)
            {
                _respawnCounter += Time.deltaTime;
                if(_respawnCounter >= _respawnTime)
                {
                    powerupVisual.SetActive(true);
                    _respawnCounter = 0f;
                }
            }
        }
    }
}