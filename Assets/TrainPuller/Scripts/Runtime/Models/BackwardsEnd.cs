using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class BackwardsEnd : MonoBehaviour
    {
        [SerializeField] private TrainMovement trainMovement;

        public void SetTrainMovement(TrainMovement train)
        {
            trainMovement = train;
        }

        public TrainMovement GetTrainMovement()
        {
            return trainMovement;
        }
    }
}
