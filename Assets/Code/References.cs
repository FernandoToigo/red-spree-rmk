using TMPro;
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
    public TMP_Text KillCountText;
    public RectTransform CollectedBulletsParent;
    public TMP_Text[] CollectedBulletVisuals;
    public TMP_Text UpgradeText;
    public CanvasGroup StartCanvas;
    public UnityButton StartButton;
    public AudioSource MusicAudioSource;
    public AudioSource[] BulletAudioSources;
    public AudioSource[] EnemyDeathAudioSources;
}