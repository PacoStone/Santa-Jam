using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private WeaponRuntime weaponRuntime;
    [SerializeField] private TMP_Text ammoText;

    private void Reset()
    {
        ammoText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (ammoText == null)
            return;

        if (weaponRuntime == null || weaponRuntime.weaponData == null)
        {
            ammoText.text = "0 / 0";
            return;
        }

        int mag = Mathf.Max(0, weaponRuntime.currentMagazine);
        int reserve = Mathf.Max(0, weaponRuntime.currentReserveAmmo);

        ammoText.text = $"{mag} / {reserve}";
    }
}
