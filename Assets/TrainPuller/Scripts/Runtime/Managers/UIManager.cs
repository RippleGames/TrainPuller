using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainPuller.Scripts.Runtime.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;

        [Header("Cached References")] [SerializeField]
        private Image screenTransitionImage;

        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject loseScreen;
        [SerializeField] private GameObject levelCompleteConfetti;
        [SerializeField] private GameObject youWinObject;
        [SerializeField] private GameObject tutorialParent;
        [SerializeField] private GameObject tutorialHand;

        [Header("Timer References")] [SerializeField]
        private GameObject timerParent;

        [SerializeField] private TextMeshProUGUI timerTMP;
        private bool isTimerBlinking;

        [Header("Start Screen References")] [SerializeField]
        private TextMeshProUGUI startScreenLevelTMP;

        [SerializeField] private TextMeshProUGUI startScreenTimerTMP;

        [Header("Lose Screen References")] [SerializeField]
        private TextMeshProUGUI loseTitleTMP;

        [Header("Level Text References")] [SerializeField]
        private GameObject levelTextParent;

        [SerializeField] private TextMeshProUGUI levelTMP;

        [Header("Settings References")] [SerializeField]
        private GameObject settingsPanel;

        [SerializeField] private GameObject settingsButton;
        [SerializeField] private Slider audioSlider;
        [SerializeField] private Slider vibrationSlider;

        [Header("Debug Settings")] 
        [SerializeField] private bool isDebug;
        private int _debugClickCounter;
        [SerializeField] private GameObject debugPanel;
        private void Awake()
        {
            InitializeSingleton();
        }

        private void Start()
        {
            if (LevelManager.instance.isTutorialOn)
            {
                OpenTutorial();
            }
        }

        private void InitializeSingleton()
        {
            if (instance) return;
            instance = this;
        }

        public void OpenTransition(Action callback)
        {
            CloseTimer();
            CloseLevelText();
            // DisableSettingsButton();
            var color = screenTransitionImage.color;
            screenTransitionImage.DOColor(new Color(color.r, color.g, color.b, 1f), 0.5f).OnComplete(() =>
            {
                callback?.Invoke();
            });
        }

        public void CloseTransition(Action callback)
        {
            var color = screenTransitionImage.color;
            screenTransitionImage.DOColor(new Color(color.r, color.g, color.b, 0f), 0.5f).OnComplete(() =>
            {
                callback?.Invoke();
            });
        }

        public void CloseLoadingScreen()
        {
            loadingScreen.SetActive(false);
        }

        public void OpenStartScreen()
        {
            LevelManager.instance.isGamePlayable = false;
            startScreen.transform.parent.gameObject.SetActive(true);
            startScreen.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        public void CloseStartScreen()
        {
            startScreen.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                startScreen.transform.parent.gameObject.SetActive(false);
                LevelManager.instance.isGamePlayable = true;
                HandleTimer();
            });
        }


        private void HandleTimer()
        {
            if (TimeManager.instance.GetIsTimerActive()) return;
            TimeManager.instance.StartTimer();
            StopBlinkTimer();
        }

        public void OpenLoseScreen()
        {
            loseScreen.transform.parent.gameObject.SetActive(true);
            loseScreen.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        private void CloseLoseScreen(Action callBack)
        {
            loseScreen.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
            {
                loseScreen.transform.parent.gameObject.SetActive(false);
                callBack?.Invoke();
            });
        }

        public void RestartButton()
        {
            CloseLoseScreen(() => { LevelManager.instance.RestartLevel(); });
        }

        public void LevelCompleteEvents()
        {
            levelCompleteConfetti.SetActive(true);
            youWinObject.SetActive(true);
            if (LevelManager.instance.isTutorialOn)
            {
                CloseTutorial();
                LevelManager.instance.isTutorialOn = false;
            }
        }

        public void OpenTimer()
        {
            timerParent.SetActive(true);
            timerParent.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack).OnComplete(StartBlinkTimer);
        }

        private void CloseTimer()
        {
            return;
            timerParent.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
            {
                isTimerBlinking = false;
                StopCoroutine(BlinkTimer());
                timerParent.SetActive(false);
            });
        }

        public TextMeshProUGUI GetTimerTMP()
        {
            return timerTMP;
        }

        public void OpenLevelText()
        {
            levelTextParent.SetActive(true);
        }

        private void CloseLevelText()
        {
            //levelTextParent.SetActive(false);
        }

        public TextMeshProUGUI GetLevelTMP()
        {
            return levelTMP;
        }

        public void EnableSettingsButton()
        {
            settingsButton.SetActive(true);
        }

        private void DisableSettingsButton()
        {
            settingsButton.SetActive(false);
        }

        public void OpenSettingsMenu()
        {
            settingsPanel.transform.parent.gameObject.SetActive(true);
            settingsPanel.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
            TimeManager.instance.PauseTimer();
            LevelManager.instance.isGamePlayable = false;
        }

        public void CloseSettingsMenu()
        {
            settingsPanel.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
            {
                settingsPanel.transform.parent.gameObject.SetActive(false);
                TimeManager.instance.StartTimer();

                LevelManager.instance.isGamePlayable = true;
            });
        }

        public void ToggleAudio()
        {
            GameplayManager.instance.ToggleAudio();
        }

        public void ToggleVibration()
        {
            GameplayManager.instance.ToggleVibration();
        }

        public void HandleSwitches(bool isAudioOn, bool isVibrationOn)
        {
            audioSlider.value = isAudioOn ? 1 : 0;
            vibrationSlider.value = isVibrationOn ? 1 : 0;
        }

        private IEnumerator BlinkTimer()
        {
            yield return new WaitForSeconds(1f);
            if (TimeManager.instance.GetIsTimerActive() && TimeManager.instance.GetTimeLeft() > 10f)
            {
                StopBlinkTimer();
                yield break;
            }

            timerTMP.DOColor(Color.red, 0.15f).OnComplete(() =>
            {
                timerTMP.DOColor(Color.white, 0.15f).SetDelay(0.15f);
            });


            StartCoroutine(BlinkTimer());
        }

        private void StopBlinkTimer()
        {
            StopCoroutine(BlinkTimer());
            isTimerBlinking = false;
            DOTween.Kill(timerTMP);
            timerTMP.color = Color.white;
        }

        public void StartBlinkTimer()
        {
            if (isTimerBlinking) return;
            isTimerBlinking = true;
            StartCoroutine(BlinkTimer());
        }

        public TextMeshProUGUI GetStartLevelTMP()
        {
            return startScreenLevelTMP;
        }

        public TextMeshProUGUI GetStartLevelTimeTMP()
        {
            return startScreenTimerTMP;
        }

        public void SetTimeLost()
        {
            loseTitleTMP.text = "Out of Time";
        }

        public void TryEnableDebug()
        {
            if (!isDebug) return;
            _debugClickCounter++;
            if(_debugClickCounter >= 3)
            {
                OpenDebugMenu();
            }
        }

        private void OpenDebugMenu()
        {
            debugPanel.SetActive(true);
        }

        public void CloseDebugMenu()
        {
            _debugClickCounter = 0;
            debugPanel.SetActive(false);
        }

        public void OpenTutorial()
        {
            tutorialParent.SetActive(true);
        }

        public void CloseTutorialHand()
        {
            tutorialHand.SetActive(false);
        }

        public void CloseTutorial()
        {
            
        }
    }
}