namespace DeadDawn.Core
{
    public enum GameState
    {
        Scavenge,   // Gathering resources, fortifying shelter
        Warning,    // Horde approaching siren/pulse countdown
        Horde,      // Zombie wave actively attacking
        Dawn,       // Wave survived, pick perks / evaluate
        GameOver    // Player or Shelter Core destroyed
    }

    public struct GameStateChangedEvent
    {
        public GameState PreviousState;
        public GameState NewState;
        public int DayNumber;

        public GameStateChangedEvent(GameState prev, GameState next, int day)
        {
            PreviousState = prev;
            NewState = next;
            DayNumber = day;
        }
    }

    public struct PlayerDamagedEvent
    {
        public float CurrentHealth;
        public float MaxHealth;

        public PlayerDamagedEvent(float current, float max)
        {
            CurrentHealth = current;
            MaxHealth = max;
        }
    }

    public struct PlayerAmmoChangedEvent
    {
        public int CurrentAmmo;
        public int MaxReserveAmmo;

        public PlayerAmmoChangedEvent(int current, int maxReserve)
        {
            CurrentAmmo = current;
            MaxReserveAmmo = maxReserve;
        }
    }

    public struct ScreenShakeEvent
    {
        public float Intensity;
        public float Duration;

        public ScreenShakeEvent(float intensity, float duration)
        {
            Intensity = intensity;
            Duration = duration;
        }
    }
}
