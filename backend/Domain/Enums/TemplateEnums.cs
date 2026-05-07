namespace PigeonRacing.Domain.Enums;

public enum TemplateCategory
{
    RaceResults      = 1,
    BestLoft         = 2,
    AcePigeon        = 3,
    SuperAcePigeon   = 4,
    Certificate      = 5
}

public enum TemplateStyle
{
    Classic          = 1,   // clean tables, serif typography
    Modern           = 2,   // bold sans-serif, coloured headers
    Elegant          = 3,   // gold/navy, decorative borders
    Minimal          = 4,   // ultra-clean whitespace
    Sporty           = 5,   // bold, high-contrast, athletic
    Heritage         = 6,   // vintage/retro feel
    Corporate        = 7,   // professional, muted palette
    Vibrant          = 8,   // full-colour background washes
    Dark             = 9,   // dark background, light text
    Branded          = 10   // uses club primary/secondary colours
}

public enum TemplatePaperSize
{
    A4Portrait       = 1,
    A4Landscape      = 2,
    A3Portrait       = 3,
    A3Landscape      = 4,
    LetterPortrait   = 5,
    LetterLandscape  = 6
}

public enum TemplateColourScheme
{
    Light            = 1,
    Dark             = 2,
    Branded          = 3,   // uses club colours from {{club.primaryColour}}
    Gold             = 4,
    Navy             = 5,
    Crimson          = 6,
    Forest           = 7,
    Monochrome       = 8
}

public enum PrintJobStatus
{
    Pending          = 1,
    Rendering        = 2,
    Complete         = 3,
    Failed           = 4
}
