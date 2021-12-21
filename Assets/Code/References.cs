using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class References : MonoBehaviour
{
    public Camera Camera;
    public PixelPerfectCamera PixelPerfectCamera;
    public PlayerComponent Player;
    public Transform GunNozzle;
    public Transform ZombieSpawn;
    public EnemyComponent[] Zombies;
    public BulletComponent[] Bullets;
    public TMP_Text BulletsText;
    public RectTransform CollectedBulletsParent;
    public TMP_Text[] CollectedBulletVisuals;
}