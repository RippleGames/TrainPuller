using System;
using TMPro;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class TimeManager : MonoBehaviour
    {        
        
        public static TimeManager instance;

        [Header("Cached References")]
        private TextMeshProUGUI timerTMP;
        
        [Header("Parameters")]
        [SerializeField] private float levelTime;
        [SerializeField] private bool isTimerActive;

        private void Awake()
        {
            if (!instance)
            {
                instance = this;
            }
        }

        private void Update()
        {
            HandleTimer();
        }

        private void HandleTimer()
        {
            if (!isTimerActive) return;
            levelTime -= Time.deltaTime;
            timerTMP.text = TimeSpan.FromSeconds((int)levelTime).ToString(@"m\:ss");
            
            if (levelTime <= 10f)
            {
                UIManager.instance.StartBlinkTimer();
            }

            if (levelTime > 0f) return;
            PauseTimer();
            GameplayManager.instance.LoseGame(true);
        }

        public void SetTimer(int time)
        {
            levelTime = time;
        }

        public void StartTimer()
        {
            isTimerActive = true;
        }

        public void PauseTimer()
        {
            isTimerActive = false;
        }

        public bool GetIsTimerActive()
        {
            return isTimerActive;
        }

        public void SetTimerTMP(TextMeshProUGUI timer,TextMeshProUGUI startScreenTimerTMP)
        {
            timerTMP = timer;
            startScreenTimerTMP.text = "Level Time: " + TimeSpan.FromSeconds((int)levelTime).ToString(@"m\:ss");
            timerTMP.text = TimeSpan.FromSeconds((int)levelTime).ToString(@"m\:ss");

            UIManager.instance.OpenTimer();
        }

        public float GetTimeLeft()
        {
            return levelTime;
        }
    }
}