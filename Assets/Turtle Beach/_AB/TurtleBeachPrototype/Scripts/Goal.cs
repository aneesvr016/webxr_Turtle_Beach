using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AB.TurtleBeach
{
    public class Goal : MonoBehaviour
    {
        public static Goal Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;
        }
    }
}
