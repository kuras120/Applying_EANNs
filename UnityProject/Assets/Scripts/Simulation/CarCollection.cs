using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{
    public class CarCollection : MonoBehaviour
    {
        public CarCollection()
        {
            Cars = new List<RaceCar>();
        }
        public CarController CarController;
        public CarController FirstbestCar { get; set; }
        public CarController SecondBestCar { get; set; }
        public Checkpoint[] Checkpoints { get; set; }
        public Vector3 StartPosition { get; set; }
        public Quaternion StartRotation { get; set; }

        public float TrackLength { get; set; }
        public List<RaceCar> Cars { get; set; }
    }
}