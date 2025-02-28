using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Cached References")] 
        private Camera _mainCam;
        [AudioClipName] public string popSound;

        [Header("Parameters")] 
        public LayerMask stickmanLayer;

        private void Start()
        {
            AssignMainCam();
        }

        private void AssignMainCam()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (!ShouldProcessInput()) return;

            if (Input.GetMouseButtonDown(0))
            {
                ProcessRaycastInteraction();
            }
        }

        private bool ShouldProcessInput()
        {
            return LevelManager.instance.isGamePlayable && !LevelManager.instance.isLevelFailed;
        }

        void ProcessRaycastInteraction()
        {
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            if (!TryRayCast(ray, out var hitInfo, stickmanLayer)) return;
            if (!hitInfo.transform || !hitInfo.transform.CompareTag("Stickman")) return;
            TrySelectStickman(hitInfo);
        }

        private bool TryRayCast(Ray ray, out RaycastHit hitInfo, LayerMask layer)
        {
            return Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layer);
        }

        private void TrySelectStickman(RaycastHit hitInfo)
        {
            if (!hitInfo.transform.TryGetComponent(out Stickman stickman)) return;

            if (!stickman.GetBelongedGrid()) return;
            if (stickman.GetIsMoving() && stickman.GetBelongedGrid() as MatchArea) return;

            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(popSound);

            }

            if (!stickman.GetHasPath() && stickman.GetBelongedGrid().GetYAxis() != 0)
            {
                stickman.WrongSelection();
                if (VibrationManager.instance)
                {
                    VibrationManager.instance.Medium();
                }

                return;
            }

            var currentGoal = GameplayManager.instance.GetCurrentBus();
            var path = stickman.GetBelongedGrid().GetClosestPath();

            if (VibrationManager.instance)
            {
                VibrationManager.instance.Light();
            }

            if (stickman.GetColor() == currentGoal.GetColor() &&
                currentGoal.GetComingStickmanCount() + 1 <= 3)
            {
                if (stickman.GetIsReserved())
                {
                    if (currentGoal.GetReservedCount() != 0)
                    {
                        currentGoal.DecreaseReservedCount();
                        GoToGoal(currentGoal, stickman, path);
                    }
                    else
                    {
                        GoToMatchArea(stickman, path);
                    }
                }
                else
                {
                    if (currentGoal.GetReservedCount() > 0)
                    {
                        if (!currentGoal.IsLastSeat())
                        {
                            GoToGoal(currentGoal, stickman, path);
                        }
                        else
                        {
                            GoToMatchArea(stickman, path);
                        }
                    }
                    else
                    {
                        GoToGoal(currentGoal, stickman, path);
                    }
                }
            }
            else
            {
                GoToMatchArea(stickman, path);
            }
        }

        private void GoToGoal(GoalScript currentGoal, Stickman stickman, List<GridBase> path)
        {
            currentGoal.AddComingStickman(1);
            stickman.DisableInteraction();
            stickman.GoToBus(path);
            GridManager.instance.RecalculatePaths();
        }

        private void GoToMatchArea(Stickman stickman, List<GridBase> path)
        {
            var availableMatchArea = MatchAreaManager.instance.GetEmptyArea();
            if (!availableMatchArea) return;
            stickman.DisableInteraction();
            stickman.GoToMatchArea(availableMatchArea, availableMatchArea.transform, path);
            GridManager.instance.RecalculatePaths();
        }
    }
}