using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace PRC.RenderingService.Models;

/// <summary>
/// Certificate types supported by the file-based renderer.
/// Maps 1:1 to the HTML template files in <c>wwwroot/templates/</c>.
/// </summary>
public enum CertType
{
    Race = 1,
    Ace = 2,
    SuperAce = 3,
    BestLoft = 4
}

/// <summary>
/// Request body for the four cert render endpoints. The <see cref="Data"/>
/// payload is forwarded verbatim into the template via <c>window.CERT_DATA</c>;
/// the schema of each <c>Data</c> shape lives next to its HTML template, not in
/// .NET, so we accept it as <see cref="JsonElement"/> and pass it through.
/// </summary>
public class CertRenderRequest
{
    /// <summary>
    /// Design code, e.g. "R1".."R10", "AR-R1".."AR-R3", and "L"-suffixed
    /// variants for landscape ("R1L", "AR-R1L", etc.).
    /// </summary>
    [Required] public string DesignId { get; set; } = "R1";

    /// <summary>"en" or "ar" — Arabic designs (AR-*) auto-force lang=ar.</summary>
    [Required] public string Language { get; set; } = "en";

    /// <summary>Full JSON payload matching the per-cert spec.</summary>
    [Required] public JsonElement Data { get; set; }
}
