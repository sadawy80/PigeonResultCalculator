using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PRC.RenderingService.Models;

/// <summary>
/// Result-table types. Each maps to a single multi-page A4 portrait template
/// with its own JSON shape and its own <c>window.*_DATA</c> globals.
/// </summary>
public enum ResultType
{
    Race = 1,
    Ace = 2,
    SuperAce = 3,
    BestLoft = 4
}

/// <summary>
/// Request body for the result render endpoints (PDF + Excel).
/// <see cref="Data"/> is the same payload the templates expect, passed through verbatim.
/// </summary>
public class ResultRenderRequest
{
    /// <summary>
    /// Design code:
    ///  - Race: "T1".."T20"
    ///  - Ace: "A1".."A10" / "AR-A1".."AR-A3"
    ///  - Super Ace: "SA1".."SA10" / "AR-SA1".."AR-SA3"
    ///  - Best Loft: "L1".."L10" / "AR-L1".."AR-L3"
    /// </summary>
    [Required] public string DesignId { get; set; } = "T1";

    /// <summary>"en"/"ar"; Race also supports fa/es/de/zh.</summary>
    [Required] public string Language { get; set; } = "en";

    /// <summary>Full JSON payload matching the per-type result spec.</summary>
    [Required] public JsonElement Data { get; set; }
}
