using TMPro;
using UnityEngine;

public class WorldHPText : MonoBehaviour
{
    public TMP_Text hpText;
    public Vector3 offset = new Vector3(0, 1.4f, 0);

    private UnitStats unitStats;
    private bool loggedMissingReference = false;

    void Awake()
    {
        // Procura automaticamente o UnitStats da unidade pai
        unitStats = GetComponentInParent<UnitStats>();

        // Se o TMP_Text não estiver configurado, tenta pegar deste objeto
        if (hpText == null)
        {
            hpText = GetComponent<TMP_Text>();
        }

        if (unitStats == null)
        {
            Debug.LogWarning(
                "WorldHPText nao encontrou UnitStats no objeto pai."
            );
        }

        if (hpText == null)
        {
            Debug.LogWarning(
                "WorldHPText nao encontrou TMP_Text."
            );
        }
    }

    void Update()
    {
        if (unitStats == null || hpText == null)
        {
            if (!loggedMissingReference)
            {
                Debug.LogWarning(
                    "WorldHPText nao pode atualizar porque falta uma referencia."
                );

                loggedMissingReference = true;
            }

            return;
        }

        if (unitStats.isDowned)
        {
            hpText.text = "Desmaiado";
        }
        else
        {
            hpText.text = unitStats.currentHP + "/" + unitStats.maxHP;
        }

        // Como HPText agora é filho da unidade,
        // usamos posição local.
        transform.localPosition = offset;

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}