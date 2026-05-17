using UnityEngine;
using System;


public enum GameMode
{
    Play,
    Build,
    Dialog,
    CharacterEditor,
    Pause,
}


public class GameModeManager : MonoBehaviour
{
    public event Action<GameMode> OnModeChanged;
    [field: SerializeField] public GameMode CurrentMode { get; private set; }

    public void SetMode(GameMode mode)
    {
        CurrentMode = mode;
        OnModeChanged?.Invoke(mode);
    }

}
