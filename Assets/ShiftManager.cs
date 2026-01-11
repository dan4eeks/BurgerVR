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
    [SerializeField] private int targetShifts = 3;
    [SerializeField] private float shiftDurationSeconds = 7f * 60f; // 7 minutes
    [SerializeField] private float betweenShiftPauseSeconds = 3f;

    [SerializeField] private ShiftIntroScreen introScreen;
    [SerializeField] private GameOverScreen gameOverScreen;

    [Header("Fail Conditions")]
    [Tooltip("Сколько 'дымовых beep' за смену считаем провалом (паника-цепочка).")]
    [SerializeField] private int smokeBeepsToFail = 1;


    [System.Serializable]
    public struct DaySettings
    {
        public int day;
        public int targetCustomers;
        public int recipeIngredientsTotal;
        public float happyTime;
        public float neutralTime;
        public float angryTime;
        public float pattyCookTime;
        public float spawnInterval;
        public float ingredientSpawnInterval;
        public int ingredientMaxAlive;
    }

    [SerializeField] private DaySettings[] days = new DaySettings[]
    {
        new DaySettings{ day=1, targetCustomers=3,  recipeIngredientsTotal=3, happyTime=60, neutralTime=60, angryTime=40, pattyCookTime=22, spawnInterval=25, ingredientSpawnInterval=20, ingredientMaxAlive=3 },
        new DaySettings{ day=2, targetCustomers=6,  recipeIngredientsTotal=4, happyTime=43, neutralTime=43, angryTime=25, pattyCookTime=18, spawnInterval=19, ingredientSpawnInterval=14, ingredientMaxAlive=4 },
        new DaySettings{ day=3, targetCustomers=10, recipeIngredientsTotal=6, happyTime=25, neutralTime=25, angryTime=10, pattyCookTime=14, spawnInterval=13, ingredientSpawnInterval=9,  ingredientMaxAlive=5 },
    };

    private DaySettings GetDay(int day)
    {
        // если вышли за массив — берём последний день
        for (int i = 0; i < days.Length; i++)
            if (days[i].day == day) return days[i];

        return days[days.Length - 1];
    }

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
        if (!shiftRunning || isTransitioning) return;

        // (опционально) если хочешь, чтобы таймер всё равно отображался — оставь.
        // Но завершение смены по таймеру убираем, чтобы не конфликтовало с целью N клиентов.

        shiftTimer += Time.deltaTime;

        if (shiftText != null)
            shiftText.text = $"День {currentShift}/{targetShifts} • Цель: {clientsServedThisShift}/{currentTargetClients}";
    }


    private IEnumerator EndShiftSuccessRoutine()
    {
        shiftRunning = false;
        isTransitioning = true;

        customerManager?.SetSpawningEnabled(false);

        if (winnerScreen != null)
            yield return winnerScreen.PlayShiftCleared(currentShift, targetShifts);
        else
            yield return new WaitForSeconds(2f);

        if (currentShift >= targetShifts)
        {
            if (winnerScreen != null)
                yield return winnerScreen.PlayFinalVictory();

            StartDay = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }

        StartDay = currentShift + 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

        // если клиент null — всё равно можно считать это провалом (но лучше, чтобы он не был null после фикса выше)
        if (mood == CustomerMood.Angry)
        {
            if (customer == null || !customer.alwaysAngry)
            {
                FailRun("Клиент недоволен");
                return;
            }
        }

        if (mood != CustomerMood.Angry)
        {
            clientsServedThisShift++;
            if (clientsServedThisShift >= currentTargetClients)
            {
                isTransitioning = true;
                StartCoroutine(EndShiftSuccessRoutine());
            }
        }
    }

    private IEnumerator StartDayIntroAndStartShift()
    {
        DaySettings s = GetDay(currentShift);

        // На время интро — не спавним
        customerManager?.SetSpawningEnabled(false);

        if (introScreen != null)
            yield return introScreen.Play(currentShift, s.targetCustomers);

        // 1) Тайминги и размер рецепта
        orderManager.ApplyDaySettings(s.happyTime, s.neutralTime, s.angryTime, s.recipeIngredientsTotal);

        // 2) Спавн клиентов: ровно N + интервал
        customerManager.ConfigureShiftSpawning(s.targetCustomers, s.spawnInterval);

        // 3) Жарка котлеты
        PattyCookable.CookTimeSeconds = s.pattyCookTime;

        // 4) Спавн ингредиентов (ускорение по дням)
        ApplyIngredientSpawners(s.ingredientSpawnInterval, s.ingredientMaxAlive);

        // Запуск смены с правильной целью
        StartShiftInternal(s.targetCustomers);
    }

    private void ApplyIngredientSpawners(float interval, int maxAlive)
    {
        interval = Mathf.Max(0.1f, interval);
        maxAlive = Mathf.Max(1, maxAlive); // КЛЮЧЕВО: минимум 1

        var spawners = FindObjectsOfType<IngredientSpawner>(true);
        foreach (var sp in spawners)
            sp.ApplyDaySettings(interval, maxAlive);
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
