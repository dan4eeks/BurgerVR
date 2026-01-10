using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShiftManager : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private WinnerScreen winnerScreen;

    [Header("Optional UI (can be null)")]
    [SerializeField] private TMP_Text shiftText;
    [SerializeField] private TMP_Text statusText;

    [Header("Shift Rules")]
    [SerializeField] private int targetShifts = 7;
    [SerializeField] private float shiftDurationSeconds = 7f * 60f; // 7 minutes
    [SerializeField] private float betweenShiftPauseSeconds = 3f;

    [SerializeField] private ShiftIntroScreen introScreen;
    [SerializeField] private GameOverScreen gameOverScreen;

    [Header("Fail Conditions")]
    [Tooltip("Сколько 'дымовых beep' за смену считаем провалом (паника-цепочка).")]
    [SerializeField] private int smokeBeepsToFail = 1;

    [SerializeField] private int baseClients = 3;
    [SerializeField] private int clientsStepPerDay = 1;

    private int currentTargetClients = 0;
    private int clientsServedThisShift = 0;
    public static int StartDay = 1;

    private void StartShiftInternal(int targetClients)
    {
        currentTargetClients = targetClients;
        clientsServedThisShift = 0;

        shiftTimer = 0f;
        smokeBeepsThisShift = 0;

        shiftRunning = true;

        isTransitioning = false;

        customerManager?.SetSpawningEnabled(true);
    }


    private int GetTargetClientsForDay(int day)
    {
        return baseClients + (day - 1) * clientsStepPerDay;
    }


    private int currentShift = 1;
    private float shiftTimer = 0f;
    private bool shiftRunning = false;
    private bool isTransitioning = false;

    private int smokeBeepsThisShift = 0;

    private void Awake()
    {
        if (customerManager == null) customerManager = FindObjectOfType<CustomerManager>();
        if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
    }

    private void OnEnable()
    {
        PattyCookable.OnSmokeAlarmBeepGlobal += OnSmokeBeep;

        if (orderManager != null)
            orderManager.OnOrderEvaluated += OnOrderEvaluated;
    }

    private void OnDisable()
    {
        PattyCookable.OnSmokeAlarmBeepGlobal -= OnSmokeBeep;

        if (orderManager != null)
            orderManager.OnOrderEvaluated -= OnOrderEvaluated;
    }

    private void Start()
    {
        currentShift = StartDay;
        StartCoroutine(StartDayIntroAndStartShift());
    }


    private void Update()
    {
        shiftTimer += Time.deltaTime;
        float left = Mathf.Max(0f, shiftDurationSeconds - shiftTimer);

        // Обновление UI
        if (shiftText != null)
            shiftText.text = $"Смена {currentShift}/{targetShifts} • Осталось {FormatTime(left)}";

        // Проверка конца смены
        if (shiftTimer >= shiftDurationSeconds)
        {
            StartCoroutine(EndShiftSuccessRoutine());
        }
    }

    private IEnumerator EndShiftSuccessRoutine()
    {
        shiftRunning = false;

        if (customerManager != null)
            customerManager.SetSpawningEnabled(false);

        // Экран “Смена пройдена”
        if (winnerScreen != null)
            yield return winnerScreen.PlayShiftCleared(currentShift, targetShifts);
        else
            yield return new WaitForSeconds(2f);

        // Если это была последняя смена — финальная победа и рестарт
        if (currentShift >= targetShifts)
        {
            // финальная победа
            if (winnerScreen != null)
                yield return winnerScreen.PlayFinalVictory();

            // полный рестарт игры
            StartDay = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }
        else
        {
            // обычная победа смены
            if (winnerScreen != null)
                yield return winnerScreen.PlayShiftCleared(currentShift, targetShifts);

            // следующий день
            StartDay = currentShift + 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }

        // Иначе — следующий день
        currentShift++;
        isTransitioning = false;
        yield return StartDayIntroAndStartShift();
    }


    private void OnSmokeBeep()
    {
        if (!shiftRunning) return;

        smokeBeepsThisShift++;

        if (statusText != null)
            statusText.text = $"ТРЕВОГА! ({smokeBeepsThisShift}/{smokeBeepsToFail})";

        if (smokeBeepsThisShift >= smokeBeepsToFail)
        {
            FailRun("Паника из-за дыма");
        }
    }

    private void StartShift(int shiftIndex)
    {
        currentShift = shiftIndex;
        shiftTimer = 0f;
        smokeBeepsThisShift = 0;

        shiftRunning = true;

        // Открываем спавн клиентов
        if (customerManager != null)
            customerManager.SetSpawningEnabled(true);

        if (statusText != null)
            statusText.text = "Смена началась";

        // Можно (опционально) стартануть заказ только когда клиент у кассы, но это у тебя уже завязано на кнопку.
        // Здесь не трогаем.
    }

    private void EndShiftSuccess()
    {
        shiftRunning = false;

        // Закрываем спавн (чтобы новые не приходили)
        if (customerManager != null)
            customerManager.SetSpawningEnabled(false);

        if (statusText != null)
            statusText.text = "Смена пережита ?";

        // Победа в режиме
        if (currentShift >= targetShifts)
        {
            if (statusText != null)
                statusText.text = $"ПОБЕДА ?? Вы выдержали {targetShifts} смен!";
            return;
        }

        // Старт следующей смены через паузу
        Invoke(nameof(StartNextShift), betweenShiftPauseSeconds);
    }

    private void StartNextShift()
    {
        StartShift(currentShift + 1);
    }

    private void FailRun(string reason)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(FailRoutine(reason));
    }

    private IEnumerator FailRoutine(string reason)
    {
        shiftRunning = false;

        if (customerManager != null)
            customerManager.SetSpawningEnabled(false);

        // Показываем Game Over
        if (gameOverScreen != null)
            yield return gameOverScreen.Play(reason);
        else
            yield return new WaitForSeconds(2f);

        // ВАЖНО: полностью сбросить мир (горящие котлеты, клиент у кассы, паника и т.п.)
        StartDay = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnOrderEvaluated(Customer customer, CustomerMood mood)
    {
        if (!shiftRunning || isTransitioning) return;

        // Game Over: клиент недоволен (кроме alwaysAngry)
        if (mood == CustomerMood.Angry && customer != null && !customer.alwaysAngry)
        {
            FailRun("Клиент недоволен");
            return;
        }

        // Считаем клиента как "обслуженного", если заказ НЕ angry
        // (neutral/happy засчитываем)
        if (mood != CustomerMood.Angry)
        {
            clientsServedThisShift++;

            // Если достигли цели — заканчиваем смену
            if (clientsServedThisShift >= currentTargetClients)
            {
                isTransitioning = true;
                StartCoroutine(EndShiftSuccessRoutine());
            }
        }
    }

    private IEnumerator StartDayIntroAndStartShift()
    {
        int targetClients = GetTargetClientsForDay(currentShift);

        // На время интро — не спавним
        if (customerManager != null)
            customerManager.SetSpawningEnabled(false);

        if (introScreen != null)
            yield return introScreen.Play(currentShift, targetClients);

        StartShiftInternal(targetClients);
    }

    private void RestartFromBeginning()
    {
        StartShift(1);
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        int m = s / 60;
        int r = s % 60;
        return $"{m:00}:{r:00}";
    }
}
