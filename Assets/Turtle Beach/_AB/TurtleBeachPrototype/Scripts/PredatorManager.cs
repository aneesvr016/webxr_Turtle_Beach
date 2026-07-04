using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AB.TurtleBeach
{
    public class PredatorManager : MonoBehaviour
    {
        [SerializeField] List<PredatorObstacle> predatorCollection;

        public static PredatorManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;
        }

        public void ClearCurrentTarget(TurtleController turtle)
        {
            var predators = predatorCollection.Where(x => x.GetCurrentTarget() == turtle);
            
            foreach (var predator in predators)
            {
                predator.ClearCurrentTarget();
                predator.SetPredatorState(PredatorState.Idle);
            }
        }
    }
}
