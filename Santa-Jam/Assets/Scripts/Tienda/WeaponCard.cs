using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponCard : MonoBehaviour, IPointerEnterHandler
{
    private TiendaBotones _shop;
    private int _index;

    public void Initialize(TiendaBotones shop, int index)
    {
        _shop = shop;
        _index = index;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_shop == null) return;
        _shop.OnCardHovered(_index);
    }
}
