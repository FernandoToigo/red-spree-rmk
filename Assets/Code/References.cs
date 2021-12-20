﻿using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class References : MonoBehaviour
{
    public PixelPerfectCamera Camera;
    public PlayerComponent Player;
    public Transform GunNozzle;
    public Transform ZombieSpawn;
    public EnemyComponent[] Zombies;
    public BulletComponent[] Bullets;
    public Image[] UiBullets;
    public TMP_Text BulletsText;
}