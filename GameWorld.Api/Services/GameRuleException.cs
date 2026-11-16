namespace GameWorld.Api.Services;

/// <summary>A player action rejected by a known game rule, safe to display to the player.</summary>
public sealed class GameRuleException(string message) : Exception(message);
