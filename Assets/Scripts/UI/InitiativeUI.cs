using System.Collections.Generic;
using UnityEngine;

public class InitiativeUI : MonoBehaviour
{
    public TurnManager turnManager;
    public Transform slotsContainer;
    public GameObject initiativeSlotPrefab;

    private readonly List<InitiativeSlot> slots = new List<InitiativeSlot>();

    void Awake()
    {
        if (turnManager == null)
            turnManager = GetComponent<TurnManager>();
    }

    void OnEnable()
    {
        Subscribe();
    }

    void Start()
    {
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (turnManager == null)
            return;

        turnManager.OnInitiativeCreated += RebuildSlots;
        turnManager.OnTurnStarted += HandleTurnStarted;
    }

    private void Unsubscribe()
    {
        if (turnManager == null)
            return;

        turnManager.OnInitiativeCreated -= RebuildSlots;
        turnManager.OnTurnStarted -= HandleTurnStarted;
    }

    private void HandleTurnStarted(UnitStats unit)
    {
        Refresh();
    }

    public void RebuildSlots()
    {
        ClearSlots();
        Refresh();
    }

    public void Refresh()
    {
        if (turnManager == null || slotsContainer == null || initiativeSlotPrefab == null)
            return;

        List<UnitStats> visualOrder = GetVisualOrder();

        if (slots.Count != visualOrder.Count)
            RecreateSlots(visualOrder.Count);

        for (int i = 0; i < visualOrder.Count; i++)
        {
            if (i >= slots.Count)
                return;

            if (slots[i] == null)
                continue;

            slots[i].Setup(visualOrder[i], i == 0);
        }
    }

    private List<UnitStats> GetVisualOrder()
    {
        List<UnitStats> visualOrder = new List<UnitStats>();

        if (turnManager == null || turnManager.initiativeOrder == null)
            return visualOrder;

        List<UnitStats> initiativeOrder = turnManager.initiativeOrder;
        int count = initiativeOrder.Count;

        if (count == 0)
            return visualOrder;

        int startIndex = turnManager.currentTurnIndex;

        if (startIndex < 0 || startIndex >= count)
            startIndex = 0;

        for (int i = 0; i < count; i++)
        {
            int index = (startIndex + i) % count;
            visualOrder.Add(initiativeOrder[index]);
        }

        return visualOrder;
    }

    private void RecreateSlots(int count)
    {
        ClearSlots();

        for (int i = 0; i < count; i++)
        {
            GameObject slotObject = Instantiate(initiativeSlotPrefab, slotsContainer);
            InitiativeSlot slot = slotObject.GetComponent<InitiativeSlot>();

            if (slot == null)
            {
                Debug.LogWarning("initiativeSlotPrefab nao possui InitiativeSlot.");
                Destroy(slotObject);
                continue;
            }

            slots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        foreach (InitiativeSlot slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slots.Clear();
    }
}
