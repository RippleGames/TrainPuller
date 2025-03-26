using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class RoadBarrierScript : MonoBehaviour
    {
        [SerializeField] private GameObject barrierObject;
        [SerializeField] private LevelData.GridColorType colorType;
        [SerializeField] private float barrierCloseTime;
        [SerializeField] private List<Renderer> barrierRenderers;
        [SerializeField] private GameColors colors;
        private float _closeTimer;
        private bool _isTimerActive;
        private bool _isBarrierOpen;


        private void Update()
        {
            HandleBarrierTime();
        }

        private void HandleBarrierTime()
        {
            if (!_isTimerActive) return;
            _closeTimer += Time.deltaTime;
            if (_closeTimer >= barrierCloseTime)
            {
                _isTimerActive = false;
                CloseBarrier();
                _closeTimer = 0f;
            }
        }

        private void OpenBarrier()
        {
            if (_isBarrierOpen) return;
            _isBarrierOpen = true;
            barrierObject.transform.DOLocalRotate(new Vector3(-90f, 0f, 0f), 0.15f);
        }

        private void CloseBarrier()
        {
            if (!_isBarrierOpen) return;
            _isBarrierOpen = false;
            barrierObject.transform.DOLocalRotate(new Vector3(0, 0f, 0f), 0.15f);
        }

        public void SetColorType(LevelData.GridColorType color)
        {
            colorType = color;
            HandleMaterialSet();
        }

        private void HandleMaterialSet()
        {
            foreach (var barrierRenderer in barrierRenderers)
            {
                var newMaterials = new Material[barrierRenderer.sharedMaterials.Length];
                for (var i = 0; i < newMaterials.Length; i++)
                {
                    newMaterials[i] = colors.activeMaterials[(int)colorType];
                }

                barrierRenderer.sharedMaterials = newMaterials;
            }
        }

        public LevelData.GridColorType GetColor()
        {
            return colorType;
        }

        public bool TryOpenBarrier(LevelData.GridColorType trainColor)
        {
            if (_isBarrierOpen) return true;
            if (trainColor != colorType) return false;
            OpenBarrier();
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("TrainCart"))
            {
                if (!_isTimerActive) return;
                _isTimerActive = false;
                _closeTimer = 0;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("TrainCart"))
            {
                TryCloseBarrier();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("TrainCart"))
            {
                if (!_isTimerActive) return;
                _isTimerActive = false;
                _closeTimer = 0;
            }
        }

        private void TryCloseBarrier()
        {
            if (_isTimerActive) return;
            _closeTimer = 0;
            _isTimerActive = true;
        }

        public bool GetIsOpen()
        {
            return _isBarrierOpen;
        }
    }
}