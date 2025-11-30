using System.Collections.Generic;
using System.Threading.Tasks;
using Route24.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class EndDayUI : MonoBehaviour, ISceneInitializable
    {
        [Header("Scene References")]
        [SerializeField] private CanvasGroup _dayCanvas;

        [SerializeField] private Transform _incomeHolder;
        [SerializeField] private Transform _expenseHolder;

        [SerializeField] private TextMeshProUGUI _dayText;

        [Header("Totals")]
        [SerializeField] private TextMeshProUGUI _incomeTotalText;
        [SerializeField] private TextMeshProUGUI _expenseTotalText;
        [SerializeField] private TextMeshProUGUI _balanceText;

        [Header("Asset References")] 
        [SerializeField] private TransactionElement _transactionPrefab;
        [SerializeField] private UpgradeShopUI _shopUI;

        // Separate UI pools
        private readonly List<TransactionElement> _incomeElements = new();
        private readonly List<TransactionElement> _expenseElements = new();

        private GameManager _gameManager;
        private CurrencyManager _currencyManager;
        private EventHub _eventHub;

        public void SceneInitialize()
        {
            Debug.Log("[EndDayUI] SceneInitialize " + GetInstanceID());
            
            _gameManager = ServiceLocator.Get<GameManager>();
            _currencyManager = ServiceLocator.Get<CurrencyManager>();
            _eventHub = ServiceLocator.Get<EventHub>();

            _eventHub.Subscribe<DayEndedEvent>(OnDayEnded);
        }

        void Update()
        {
            if (_dayCanvas != null && _dayCanvas.interactable && Input.GetKeyDown(KeyCode.Space))
            {
                OpenUpgradeShop();
            }
        }

        private void OnDestroy()
        {
            _eventHub.Unsubscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayEnded(DayEndedEvent dayEndedEvent)
        {
            StartCoroutine(DayEndeCoroutine(dayEndedEvent));
        }

        private IEnumerator DayEndeCoroutine(DayEndedEvent dayEndedEvent)
        {
            while (_shipManager.HasActiveShip) // wait for ship to exit
                yield return null;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            
            _dayCanvas.alpha = 1;
            _dayCanvas.interactable = true;
            _dayCanvas.blocksRaycasts = true;

            ShowTransactions();
            
            _dayText.text = $"Day {dayEndedEvent.Day}";
        }
        
        public void OpenUpgradeShop()
        {
            _dayCanvas.alpha = 0;
            _dayCanvas.interactable = false;
            _dayCanvas.blocksRaycasts = false;
            
            _shopUI.Show();
        }

        public void OnNextDayButtonClicked()
        {
            Debug.Log("[EndDayUI] OnNextDayButtonClicked");
            
            Cursor.visible = false;
            
            _dayCanvas.alpha = 0;
            _dayCanvas.interactable = false;
            _dayCanvas.blocksRaycasts = false;

            _currencyManager.ResetSavings(GetBalance());
            HideAllTransactions();

            _gameManager.StartNewDay();
        }

        // -----------------------------------------------------------
        // -------------------   TRANSACTIONS   -----------------------
        // -----------------------------------------------------------

        private void ShowTransactions()
        {
            var all = new List<TransactionOption>();

            // Add today's real transactions
            all.AddRange(Transactions.ConvertToUIList(_currencyManager.GetTransactions()));

            // Add recurring expenses
            all.AddRange(_currencyManager.GetRegularExpenses.ConvertToOptionsList());

            // Add optional expenses
            all.AddRange(_currencyManager.GetExpenseOptions);

            // Split into income + expense groups
            var income = new List<TransactionOption>();
            var expenses = new List<TransactionOption>();

            foreach (var t in all)
            {
                if (t.Transaction.Type == ETransaction.income)
                    income.Add(t);
                else
                    expenses.Add(t);
            }

            SetIncomeUI(income);
            SetExpenseUI(expenses);
            UpdateTotals();
        }

        private void SetIncomeUI(List<TransactionOption> list)
        {
            EnsureUIListSize(list.Count, _incomeElements, _incomeHolder);

            for (int i = 0; i < list.Count; i++)
                AssignTransactionToElement(_incomeElements[i], list[i]);
        }

        private void SetExpenseUI(List<TransactionOption> list)
        {
            EnsureUIListSize(list.Count, _expenseElements, _expenseHolder);

            for (int i = 0; i < list.Count; i++)
                AssignTransactionToElement(_expenseElements[i], list[i]);
        }

        private void EnsureUIListSize(int count, List<TransactionElement> pool, Transform parent)
        {
            while (pool.Count < count)
            {
                var e = Instantiate(_transactionPrefab, parent);
                pool.Add(e);
            }

            // Activate needed
            for (int i = 0; i < pool.Count; i++)
                pool[i].gameObject.SetActive(i < count);
        }

        private void AssignTransactionToElement(TransactionElement element, TransactionOption option)
        {
            element.ShowTransaction(option.Transaction);
        }

        private void HideAllTransactions()
        {
            foreach (var e in _incomeElements) e.Hide();
            foreach (var e in _expenseElements) e.Hide();
        }

        // -----------------------------------------------------------
        // -----------------------   TOTALS   -------------------------
        // -----------------------------------------------------------

        private int GetIncomeTotal()
        {
            int total = 0;
            foreach (var e in _incomeElements)
                total += e.GetCurrencyAffect();
            return total;
        }

        private int GetExpenseTotal()
        {
            int total = 0;
            foreach (var e in _expenseElements)
                total += e.GetCurrencyAffect();
            return total;
        }

        private int GetBalance() => GetIncomeTotal() + GetExpenseTotal();

        private void UpdateTotals()
        {
            int income = GetIncomeTotal();
            int expense = GetExpenseTotal();
            int balance = income + expense;

            _incomeTotalText.text = $"${income}";
            _expenseTotalText.text = $"${expense}";
            _balanceText.text = $"${balance}";
        }
    }
}
