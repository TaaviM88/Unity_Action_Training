using System;

public static class GameEvents
{
    // --- Combat / health ---
    public static event Action<DamageEvent> DamageDealt;
    public static event Action<DeathEvent> EntityDied;

    // --- Waves / scoring ---
    public static event Action<WaveEvent> WaveStarted;
    public static event Action<WaveEvent> WaveCompleted;
    public static event Action<ScoreEvent> ScoreChanged;

    public static void Raise(DamageEvent e) => DamageDealt?.Invoke(e);
    public static void Raise(DeathEvent e) => EntityDied?.Invoke(e);
    public static void Raise(WaveEvent e, bool completed = false)
    {
        if (completed) WaveCompleted?.Invoke(e);
        else WaveStarted?.Invoke(e);
    }
    public static void Raise(ScoreEvent e) => ScoreChanged?.Invoke(e);
}

public readonly struct DamageEvent
{
    public readonly int Amount;
    public readonly string SourceId;
    public readonly string TargetId;

    public DamageEvent(int amount, string sourceId, string targetId)
    {
        Amount = amount;
        SourceId = sourceId;
        TargetId = targetId;
    }
}

public readonly struct DeathEvent
{
    public readonly string EntityId;
    public readonly string KillerId;

    public DeathEvent(string entityId, string killerId)
    {
        EntityId = entityId;
        KillerId = killerId;
    }
}

public readonly struct WaveEvent
{
    public readonly int WaveIndex;
    public WaveEvent(int waveIndex) => WaveIndex = waveIndex;
}

public readonly struct ScoreEvent
{
    public readonly int NewScore;
    public ScoreEvent(int newScore) => NewScore = newScore;
}