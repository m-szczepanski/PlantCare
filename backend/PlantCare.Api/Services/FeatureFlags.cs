namespace PlantCare.Api.Services;

/// <summary>Env-driven feature flags (see .env.example).</summary>
public sealed record FeatureFlags(bool CareTipsEnabled);
