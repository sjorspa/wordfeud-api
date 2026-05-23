using FluentValidation;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Validators;

/// <summary>
/// Validator for CreateGameRequest.
/// </summary>
public class CreateGameRequestValidator : AbstractValidator<CreateGameRequest>
{
    /// <summary>
    /// Creates a new CreateGameRequestValidator.
    /// </summary>
    public CreateGameRequestValidator()
    {
        RuleFor(x => x.PlayerName)
            .NotEmpty().WithMessage("Player name is required.")
            .MaximumLength(50).WithMessage("Player name must not exceed 50 characters.");
    }
}

/// <summary>
/// Validator for JoinGameRequest.
/// </summary>
public class JoinGameRequestValidator : AbstractValidator<JoinGameRequest>
{
    /// <summary>
    /// Creates a new JoinGameRequestValidator.
    /// </summary>
    public JoinGameRequestValidator()
    {
        RuleFor(x => x.PlayerName)
            .NotEmpty().WithMessage("Player name is required.")
            .MaximumLength(50).WithMessage("Player name must not exceed 50 characters.");
    }
}

/// <summary>
/// Validator for PlaceTilesRequest.
/// </summary>
public class PlaceTilesRequestValidator : AbstractValidator<PlaceTilesRequest>
{
    /// <summary>
    /// Creates a new PlaceTilesRequestValidator.
    /// </summary>
    public PlaceTilesRequestValidator()
    {
        RuleFor(x => x.Tiles)
            .NotEmpty().WithMessage("At least one tile must be placed.")
            .Must(tiles => tiles.Count <= 7).WithMessage("Cannot place more than 7 tiles at once.");

        RuleFor(x => x.StartRow)
            .InclusiveBetween(0, 14).WithMessage("StartRow must be between 0 and 14.");

        RuleFor(x => x.StartColumn)
            .InclusiveBetween(0, 14).WithMessage("StartColumn must be between 0 and 14.");

        RuleFor(x => x.Direction)
            .Must(d => d == 0 || d == 1).WithMessage("Direction must be 0 (horizontal) or 1 (vertical).");
    }
}

/// <summary>
/// Validator for SwapTilesRequest.
/// </summary>
public class SwapTilesRequestValidator : AbstractValidator<SwapTilesRequest>
{
    /// <summary>
    /// Creates a new SwapTilesRequestValidator.
    /// </summary>
    public SwapTilesRequestValidator()
    {
        RuleFor(x => x.TileIds)
            .NotEmpty().WithMessage("At least one tile must be swapped.")
            .Must(ids => ids.Count <= 7).WithMessage("Cannot swap more than 7 tiles at once.");
    }
}
