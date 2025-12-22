using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Items/Weapon")]
public class WeaponDa : InGameItem
{
    public bool Automatic;
    public float FireRate = 10f;
    public int MaxReserveAmmo = 90;
    public int BulletsPerMagazine = 30;
    public float ReloadTime = 1.6f;
    public float HitDistance = 30f;
    public LayerMask EnvironmentMask;
    public GameObject[] DecalPrefabs;
    public Texture2D DecalTextureFallback;
    public float DecalSurfaceOffset = 0.0015f;
    public float DecalSize = 0.12f;
    public bool RandomDecalRotation = true;
    public bool DebugSpawnBullet = true;

    private void OnValidate() => Category = ItemCategory.Weapons;
}
