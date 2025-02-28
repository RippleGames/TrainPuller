using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class MatchArea : GridBase
    {
        [Header("Flags")] 
        [SerializeField] private bool isTaken;

        public void HandleNewGoal()
        {
            if (!HasStickman()) return;
            if (!GameplayManager.instance.GetCurrentBus()) return;
            var currentBus = GameplayManager.instance.GetCurrentBus();
            if (stickman.GetColor() != currentBus.GetColor()) return;

            if (stickman.GetIsReserved())
            {
                if (currentBus.GetReservedCount() == 0) return;
                currentBus.DecreaseReservedCount();
                GoToBus(currentBus);
            }
            else
            {
                if (currentBus.GetReservedCount() > 0)
                {
                    if (currentBus.IsLastSeat()) return;
                    GoToBus(currentBus);
                }
                else
                {
                    GoToBus(currentBus);
                }
            }
        }

        private void GoToBus(GoalScript currentBus)
        {
            if (currentBus.GetComingStickmanCount() >= 3) return;
            currentBus.AddComingStickman(1);
            stickman.GoToBus(null);
            SetTaken(false);
            MatchAreaManager.instance.RemoveMatchArea(this);
        }


        public bool HasStickman()
        {
            return stickman;
        }

        public void AddStickman(Stickman man)
        {
            stickman = man;
        }

        public bool IsTaken()
        {
            return isTaken;
        }

        public void SetTaken(bool flag)
        {
            isTaken = flag;
        }
    }
}