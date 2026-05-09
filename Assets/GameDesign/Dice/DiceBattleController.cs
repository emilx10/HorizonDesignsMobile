using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(D6DiceRoller))]
[RequireComponent(typeof(D6DiceDamage))]
[RequireComponent(typeof(DicePlayerProgress))]
[RequireComponent(typeof(DicePlayerHealth))]
public sealed class DiceBattleController : MonoBehaviour
{
    [SerializeField] private D6DiceRoller diceRoller;
    [SerializeField] private D6DiceDamage diceDamage;
    [SerializeField] private DicePlayerProgress playerProgress;
    [SerializeField] private DicePlayerHealth playerHealth;
    [SerializeField] private DiceEnemyAI enemy;
    [SerializeField] private DiceEnemyAI enemyPrefab;
    [SerializeField] private Vector3 enemySpawnPosition = new(0f, 0f, 2.5f);

    private void Awake()
    {
        if (diceRoller == null)
        {
            diceRoller = GetComponent<D6DiceRoller>();
        }

        if (diceDamage == null)
        {
            diceDamage = GetComponent<D6DiceDamage>();
        }

        if (playerProgress == null)
        {
            playerProgress = GetComponent<DicePlayerProgress>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<DicePlayerHealth>();
        }

        EnsureEnemy();
    }

    private void OnEnable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished += HandleRollFinished;
        }

        SubscribeEnemy();
    }

    private void OnDisable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished -= HandleRollFinished;
        }

        if (enemy != null)
        {
            enemy.Defeated -= HandleEnemyDefeated;
        }
    }

    private void HandleRollFinished(int _)
    {
        EnsureEnemy();

        if (enemy == null || diceDamage == null)
        {
            return;
        }

        enemy.TakeDamage(diceDamage.DiceDamage);

        if (!enemy.IsDefeated && playerHealth != null)
        {
            enemy.SpinThenAct(ApplyEnemyDamage);
        }
    }

    private void ApplyEnemyDamage()
    {
        if (enemy == null || enemy.IsDefeated || playerHealth == null)
        {
            return;
        }

        playerHealth.TakeDamage(enemy.Damage);
    }

    private void HandleEnemyDefeated(DiceEnemyAI defeatedEnemy, int experienceReward)
    {
        if (playerProgress != null)
        {
            playerProgress.AddExperience(experienceReward);
        }
    }

    private void EnsureEnemy()
    {
        if (enemy != null)
        {
            return;
        }

        enemy = enemyPrefab != null
            ? Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity)
            : FindFirstObjectByType<DiceEnemyAI>();

        SubscribeEnemy();
    }

    private void SubscribeEnemy()
    {
        if (enemy != null)
        {
            enemy.Defeated -= HandleEnemyDefeated;
            enemy.Defeated += HandleEnemyDefeated;
        }
    }
}
