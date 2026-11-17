namespace RacingGame.Gameplay.Import;

/// <summary>
/// Metallic-roughness parameters of one family of surfaces of the race world, with the name of the family (the name tells
/// apart two families that share their values, such as the glass of the buildings and the glass of the car). Starting
/// values, tuned by eye afterwards: no metallic-roughness texture is authored, each material carries scalars.
/// </summary>
public readonly record struct RacingGamePbrFamily(string Name, float Metallic, float Roughness, float ClearCoat = 0f, float ClearCoatRoughness = 0.1f);
