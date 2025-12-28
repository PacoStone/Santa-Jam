#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static InGameItem;

[CustomEditor(typeof(InGameItem), true)]
public class InGameItemEditor : Editor
{
    // ===== Common =====
    SerializedProperty itemName;
    SerializedProperty icon;
    SerializedProperty description;
    SerializedProperty category;
    SerializedProperty previewAnchorPosition;
    SerializedProperty handsToHold;

    // ===== Weapon =====
    SerializedProperty weaponClass;
    SerializedProperty automatic;
    SerializedProperty fireRate;
    SerializedProperty maxReserveAmmo;
    SerializedProperty bulletsPerMagazine;
    SerializedProperty reloadTime;
    SerializedProperty hitDistance;
    SerializedProperty environmentMask;
    SerializedProperty decalPrefabs;
    SerializedProperty decalTextureFallback;
    SerializedProperty decalSurfaceOffset;
    SerializedProperty decalSize;
    SerializedProperty randomDecalRotation;
    SerializedProperty debugSpawnBullet;

    // ===== Armour =====
    SerializedProperty totalArmourPoints;
    SerializedProperty armourLossPerHit;
    SerializedProperty damageReductionPercent;

    private void OnEnable()
    {
        // Common
        itemName = serializedObject.FindProperty("ItemName");
        icon = serializedObject.FindProperty("Icon");
        description = serializedObject.FindProperty("Description");
        category = serializedObject.FindProperty("Category");
        previewAnchorPosition = serializedObject.FindProperty("PreviewAnchorPosition");
        handsToHold = serializedObject.FindProperty("HandsToHold");

        // Weapon
        weaponClass = serializedObject.FindProperty("Class");
        automatic = serializedObject.FindProperty("Automatic");
        fireRate = serializedObject.FindProperty("FireRate");
        maxReserveAmmo = serializedObject.FindProperty("MaxReserveAmmo");
        bulletsPerMagazine = serializedObject.FindProperty("BulletsPerMagazine");
        reloadTime = serializedObject.FindProperty("ReloadTime");
        hitDistance = serializedObject.FindProperty("HitDistance");
        environmentMask = serializedObject.FindProperty("EnvironmentMask");
        decalPrefabs = serializedObject.FindProperty("DecalPrefabs");
        decalTextureFallback = serializedObject.FindProperty("DecalTextureFallback");
        decalSurfaceOffset = serializedObject.FindProperty("DecalSurfaceOffset");
        decalSize = serializedObject.FindProperty("DecalSize");
        randomDecalRotation = serializedObject.FindProperty("RandomDecalRotation");
        debugSpawnBullet = serializedObject.FindProperty("DebugSpawnBullet");

        // Armour
        totalArmourPoints = serializedObject.FindProperty("TotalArmourPoints");
        armourLossPerHit = serializedObject.FindProperty("ArmourLossPerHit");
        damageReductionPercent = serializedObject.FindProperty("DamageReductionPercent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawGroupBox("UI", () =>
        {
            EditorGUILayout.PropertyField(itemName);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(description);
        });

        DrawGroupBox("General", () =>
        {
            EditorGUILayout.PropertyField(category);
            EditorGUILayout.PropertyField(previewAnchorPosition);
            EditorGUILayout.PropertyField(handsToHold);
        });

        ItemCategory cat = (ItemCategory)category.enumValueIndex;

        switch (cat)
        {
            case ItemCategory.Weapons:
                DrawWeaponSection();
                break;

            case ItemCategory.Armour:
                DrawArmourSection();
                break;

            default:
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(
                    "Esta categoría aún no tiene una sección personalizada.",
                    MessageType.Info);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ========================= WEAPON =========================

    private void DrawWeaponSection()
    {
        if (!(target is WeaponDa))
        {
            EditorGUILayout.HelpBox(
                "Category = Weapons, pero este asset no es WeaponDa.",
                MessageType.Warning);
            return;
        }

        //DrawGroupBox("Weapon - Class", () =>
        //{
        //    EditorGUILayout.PropertyField(weaponClass);
        //});

        DrawGroupBox("Weapon - Fire", () =>
        {
            EditorGUILayout.PropertyField(automatic);
            EditorGUILayout.PropertyField(fireRate);
        });

        DrawGroupBox("Weapon - Ammo", () =>
        {
            EditorGUILayout.PropertyField(maxReserveAmmo);
            EditorGUILayout.PropertyField(bulletsPerMagazine);
            EditorGUILayout.PropertyField(reloadTime);
        });

        DrawGroupBox("Weapon - Hit / Decal", () =>
        {
            EditorGUILayout.PropertyField(hitDistance);
            EditorGUILayout.PropertyField(environmentMask);
        });

        DrawGroupBox("Weapon - Decals", () =>
        {
            EditorGUILayout.PropertyField(decalPrefabs, true);
            EditorGUILayout.PropertyField(decalTextureFallback);
            EditorGUILayout.PropertyField(decalSurfaceOffset);
            EditorGUILayout.PropertyField(decalSize);
            EditorGUILayout.PropertyField(randomDecalRotation);
        });

        DrawGroupBox("Weapon - Debug", () =>
        {
            EditorGUILayout.PropertyField(debugSpawnBullet);
        });
    }

    // ========================= ARMOUR =========================

    private void DrawArmourSection()
    {
        if (!(target is ArmourDa))
        {
            EditorGUILayout.HelpBox(
                "Category = Armour, pero este asset no es ArmourDa.",
                MessageType.Warning);
            return;
        }

        DrawGroupBox("Armour - Stats", () =>
        {
            EditorGUILayout.PropertyField(totalArmourPoints);
            EditorGUILayout.PropertyField(armourLossPerHit);
            EditorGUILayout.PropertyField(damageReductionPercent);
        });
    }

    // ========================= UTILS =========================

    private void DrawGroupBox(string title, System.Action content)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
        content.Invoke();
        EditorGUILayout.EndVertical();
    }
}
#endif
