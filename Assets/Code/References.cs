﻿using TMPro;
using UnityEngine;
using UnityEngine.U2D;

public class References : MonoBehaviour
{
    public Camera Camera;
    public PixelPerfectCamera PixelPerfectCamera;
    public PlayerComponent Player;
    public Transform StraightGunNozzle;
    public Transform DiagonalGunNozzle;
    public Transform ZombieSpawn;
    public Transform MinVultureSpawn;
    public Transform MaxVultureSpawn;
    public EnemyComponent[] Zombies;
    public EnemyComponent[] Vultures;
    public BulletComponent[] Bullets;
    public TMP_Text BulletsText;
    public RectTransform CollectedBulletsParent;
    public TMP_Text[] CollectedBulletVisuals;
}