using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShootingTurf : MonoBehaviour
{
    [Header("Team Settings")]
    [Tooltip("Your team ID (0, 1, 2, ...).")]
    public int      teamId;
    [Tooltip("Material used for your team’s paint decals.")]
    public Material paintMaterial;

    [Header("Ammo Settings")]
    [Tooltip("Max ammo per magazine.")]
    public int   maxAmmo    = 20;
    [Tooltip("Ammo recovered per second.")]
    public float reloadRate = 5f;

    [Header("Fire Settings")]
    [Tooltip("Shots per second when holding fire.")]
    public float fireRate   = 5f;

    [Header("UI")]
    [Tooltip("World-space Image (Filled→Horizontal) for ammo bar.")]
    public Image ammoBarFillImage;

    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab (with Rigidbody & PaintProjectile).")]
    public GameObject paintProjectilePrefab;
    [Tooltip("Muzzle or spawn point.")]
    public Transform shootOrigin;
    [Tooltip("Impulse force to launch projectile).")]
    public float shootForce = 500f;

    // Runtime
    private float currentAmmo;
    private bool  isFiring;
    private float nextFireTime;

    // Input
    private PlayerInput playerInput;
    private InputAction fireAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        fireAction  = playerInput.actions.FindAction("Attack");

        currentAmmo  = maxAmmo;
        isFiring     = false;
        nextFireTime = 0f;

        fireAction.started  += _ => isFiring = true;
        fireAction.canceled += _ => isFiring = false;
    }

    private void OnEnable()  => fireAction.Enable();
    private void OnDisable() => fireAction.Disable();

    private void Update()
    {
        // Auto-reload
        if (currentAmmo < maxAmmo)
        {
            currentAmmo += reloadRate * Time.deltaTime;
            currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
        }

        // Continuous fire when holding button
        if (isFiring && Time.time >= nextFireTime && currentAmmo >= 1f)
        {
            ShootOnce();
            nextFireTime = Time.time + 1f / fireRate;
        }

        // UI update
        if (ammoBarFillImage != null)
            ammoBarFillImage.fillAmount = currentAmmo / maxAmmo;
    }

    private void ShootOnce()
    {
        // 1) Spawn & fire projectile
        var proj = Instantiate(
            paintProjectilePrefab,
            shootOrigin.position,
            shootOrigin.rotation
        );
        if (proj.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(shootOrigin.forward * shootForce);

        // 2) Pass team/color info to the projectile
        if (proj.TryGetComponent<PaintProjectile>(out var painter))
        {
            painter.teamId       = teamId;
            painter.paintMaterial = paintMaterial;
        }

        // 3) Consume ammo
        currentAmmo -= 1f;
    }
}
