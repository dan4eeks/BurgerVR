using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShiftManager : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private OrderManager orderManager;
    [SerializeField] private WinnerScreen winnerScreen;
    [SerializeField] private ShiftIntroScreen introScreen;
    [SerializeField] private GameOverScreen gameOverScreen;

    [Header("Optional UI (can be null)")]
    [SerializeField] private TMP_Text shiftText;
    [SerializeField] private TMP_Text statusText;

    [Header("Shift Rules")]
    [SerializeField] private int targetShifts = 3;

    [Header("Fail Conditions")]
    [Tooltip("—колько 'дымовых beep' за смену считаем провалом (паника-цепочка).")]
    [SerializeField] private int smokeBeepsToFail = 1;

    [System.Serializable]
    public struct DaySettings
    {
        public int day;

        [Header("Customers")]
        public int targetCustomers;
        public float spawnInterval;

        [Header("Recipe")]
        public int recipeIngredientsTotal;

        [Header("Mood by time")]
        public float happyTime;
        public float neutralTime;
        public float angryTime;

        [Header("Cooking")]
        public float pattyCookTime;

        [Header("Ingredients spawning")]
        public float ingredientSpawnInterval;
        public int ingredientMaxAlive;
    }

    [SerializeField] private DaySettings[] days = new DaySettings[]
    {
        new DaySettings{ day=1, targetCustomers=3,  recipeIngredientsTotal=3, happyTime=60, neutralTime=60, angryTime=40, pattyCookTime=22, spawnInterval=25, ingredientSpawnInterval=20, ingredientMaxAlive=3 },
        new DaySettings{ day=2, targetCustomers=6,  recipeIngredientsTotal=4, happyTime=43, neutralTime=43, angryTime=25, pattyCookTime=18, spawnInterval=19, ingredientSpawnInterval=14, ingredientMaxAlive=4 },
        new DaySettings{ day=3, targetCustomers=10, recipeIngredientsTotal=6, happyTime=25, neutralTime=25, angryTime=10, pattyCookTime=14, spawnInterval=13, ingredientSpawnInterval=9,  ingredientMaxAlive=5 },
    };

    // Persist day across scene reload
    public static int StartDay = 1;

    private int currentShift = 1;

    private int currentTargetClients = 0;
    private int clientsServedThisShift = 0;

    private bool shiftRunning = false;
    private bool isTransitioning = false;

    private int smokeBeepsThisShift = 0;

    // защита от двойного засчЄта одного клиента
    private readonly HashSet<Customer> countedCustomers = new HashSet<Customer>();

    private void Awake()
    {
        if (customerManager == null) customerManager = FindObjectOfType<CustomerManager>();
        if (orderManager == null) orderManager = FindObjectOfType<OrderManager>();
    }

    private void OnEnable()
    {
        PattyCookable.OnSmokeAlarmBeepGlobal += OnSmokeBeep;

        if (orderManager != null)
            orderManager.OnCustomerReactionFinished += OnCustomerReactionFinished;
    }

    private void OnDisable()
    {
        PattyCookable.OnSmokeAlarmBeepGlobal -= OnSmokeBeep;

        if (orderManager != null)
            orderManager.OnCustomerReactionFinished += OnCustomerReactionFinished;
    }

    private void Start()
    {
        // защита от мусора в StartDay
        if (StartDay < 1) StartDay = 1;
        if (StartDay > targetShifts) StartDay = 1;

        currentShift = StartDay;
        StartCoroutine(StartDayIntroAndStartShift());
    }

    private void Update()
    {
        if (!shiftRunning || isTransitioning) return;

        if (shiftText != null)
            shiftText.text = $"ƒень {currentShift}/{targetShifts} Х ÷ель: {clientsServedThisShift}/{currentTargetClients}";
    }

    private DaySettings GetDay(int day)
    {
        for (int i = 0; i < days.Length; i++)
            if (days[i].day == day) return days[i];

        return days.Length > 0 ? days[days.Length - 1] : default;
    }

    private IEnumerator StartDayIntroAndStartShift()
    {
        DaySettings s = GetDay(currentShift);

        // Ќа врем€ интро Ч не спавним клиентов
        customerManager?.SetSpawningEnabled(false);

        // »нтро
        if (introScreen != null)
            yield return introScreen.Play(currentShift, s.targetCustomers);

        // ѕримен€ем настройки дн€
        if (orderManager != null)
            orderManager.ApplyDaySettings(s.happyTime, s.neutralTime, s.angryTime, s.recipeIngredientsTotal);

        if (customerManager != null)
            customerManager.ConfigureShiftSpawning(s.targetCustomers, s.spawnInterval);

        PattyCookable.CookTimeSeconds = Mathf.Max(0.1f, s.pattyCookTime);

        ApplyIngredientSpawners(s.ingredientSpawnInterval, s.ingredientMaxAlive);

        // «апуск смены
        StartShiftInternal(s.targetCustomers);

        if (statusText != null)
            statusText.text = "—мена началась";
    }

    private void StartShiftInternal(int targetClients)
    {
        countedCustomers.Clear();

        currentTargetClients = Mathf.Max(1, targetClients);
        clientsServedThisShift = 0;

        smokeBeepsThisShift = 0;

        shiftRunning = true;
        isTransitioning = false;
    }

    private void ApplyIngredientSpawners(float interval, int maxAlive)
    {
        interval = Mathf.Max(0.1f, interval);
        maxAlive = Mathf.Max(1, maxAlive);

        var spawners = FindObjectsOfType<IngredientSpawner>(true);
        foreach (var sp in spawners)
            sp.ApplyDaySettings(interval, maxAlive);
    }

    private void OnSmokeBeep()
    {
        if (!shiftRunning || isTransitioning) return;

        smokeBeepsThisShift++;

        if (statusText != null)
            statusText.text = $"“–≈¬ќ√ј! ({smokeBeepsThisShift}/{smokeBeepsToFail})";

        if (smokeBeepsThisShift >= smokeBeepsToFail)
        {
            // вместо FailRun Ч специальный сценарий
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(FailAfterEvacuationRoutine("ѕаника из-за дыма"));
        }
    }

    private IEnumerator FailAfterEvacuationRoutine(string reason)
    {
        shiftRunning = false;
        isTransitioning = true;

        // стопаем спавн
        customerManager?.SetSpawningEnabled(false);

        // ? ждЄм, пока все клиенты добегут и уничтожатс€
        if (customerManager != null)
            yield return customerManager.WaitForEvacuation(12f);

        // показываем Game Over
        if (gameOverScreen != null)
            yield return gameOverScreen.Play(reason);
        else
            yield return new WaitForSeconds(2f);

        StartDay = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    private void OnCustomerReactionFinished(Customer customer, CustomerMood mood)
    {
        if (!shiftRunning || isTransitioning) return;

        // ? Game Over только после реакции
        if (mood == CustomerMood.Angry)
        {
            // "вечно злых" Ќ≈ считаем причиной game over
            if (customer == null || !customer.alwaysAngry)
            {
                FailRun(" лиент недоволен");
                return;
            }

            // но если alwaysAngry Ч можно засчитать как обслуженного, чтобы смена не зависала
            CountServedOnce(customer);
            TryFinishShift();
            return;
        }

        // ? Happy/Neutral Ч обслужено
        if (customer != null)
            CountServedOnce(customer);

        TryFinishShift();
    }

    private void CountServedOnce(Customer customer)
    {
        if (!countedCustomers.Add(customer)) return;
        clientsServedThisShift++;
    }

    private void TryFinishShift()
    {
        if (clientsServedThisShift >= currentTargetClients)
        {
            isTransitioning = true;
            StartCoroutine(EndShiftSuccessRoutine());
        }
    }

    private IEnumerator EndShiftSuccessRoutine()
    {
        shiftRunning = false;
        isTransitioning = true;

        customerManager?.SetSpawningEnabled(false);

        if (winnerScreen != null)
            yield return winnerScreen.PlayShiftCleared(currentShift, targetShifts);
        else
            yield return new WaitForSeconds(1.5f);

        // ‘инальна€ победа
        if (currentShift >= targetShifts)
        {
            if (winnerScreen != null)
                yield return winnerScreen.PlayFinalVictory();

            StartDay = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield break;
        }

        // —ледующий день
        StartDay = currentShift + 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void FailRun(string reason)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(FailRoutine(reason));
    }

    private IEnumerator FailRoutine(string reason)
    {
        shiftRunning = false;
        isTransitioning = true;

        customerManager?.SetSpawningEnabled(false);

        if (gameOverScreen != null)
            yield return gameOverScreen.Play(reason);
        else
            yield return new WaitForSeconds(2f);

        StartDay = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
