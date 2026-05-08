namespace PRC.RenderingService.Data;

/// <summary>
/// Contains all 160 seeded print templates as self-contained HTML strings.
/// Variables use {{path}} syntax, resolved by TemplateRenderer before delivery.
/// </summary>
public static partial class TemplateLibrary
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SHARED CSS FRAGMENTS
    // ═══════════════════════════════════════════════════════════════════════════

    private const string PrintBase = @"
        @page { margin: 12mm; }
        @media print { body { -webkit-print-color-adjust: exact; print-color-adjust: exact; } }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 10pt; color: #1a1a1a; background: #fff; }
        table { width: 100%; border-collapse: collapse; }
        td, th { padding: 5px 8px; }
    ";

    private const string SeriffBase = @"
        @page { margin: 15mm; }
        @media print { body { -webkit-print-color-adjust: exact; print-color-adjust: exact; } }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: 'Georgia', 'Times New Roman', serif; font-size: 10.5pt; color: #1a1a1a; background: #fff; }
        table { width: 100%; border-collapse: collapse; }
        td, th { padding: 5px 8px; }
    ";

    // ═══════════════════════════════════════════════════════════════════════════
    //  RACE RESULTS — 50 TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════════

    // RR-01 Classic Navy Table
    public const string RR01 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
.header { background: #1E3A5F; color: #fff; padding: 14px 20px; display: flex; justify-content: space-between; align-items: center; }
.header h1 { font-size: 18pt; font-weight: 700; letter-spacing: 1px; }
.header .sub { font-size: 9pt; opacity: .8; }
.logo { width: 60px; height: 60px; object-fit: contain; background: rgba(255,255,255,.15); border-radius: 4px; padding: 4px; }
.meta { display: flex; gap: 24px; padding: 10px 20px; background: #F0F4F8; border-bottom: 2px solid #1E3A5F; font-size: 9pt; }
.meta-item { display: flex; flex-direction: column; }
.meta-item strong { font-size: 10pt; color: #1E3A5F; }
thead th { background: #1E3A5F; color: #fff; font-size: 9pt; text-align: left; padding: 7px 8px; }
thead th:first-child { width: 50px; text-align: center; }
tbody tr:nth-child(even) { background: #F5F8FC; }
tbody tr:nth-child(1) td { background: #FFF8E1; font-weight: 700; }
tbody tr:nth-child(2) td { background: #F5F5F5; }
tbody tr:nth-child(3) td { background: #FFF3E0; }
td { font-size: 9pt; border-bottom: 1px solid #E8EDF2; }
td:first-child { text-align: center; font-weight: 700; color: #1E3A5F; font-size: 11pt; }
.footer { padding: 8px 20px; font-size: 8pt; color: #888; display: flex; justify-content: space-between; margin-top: 8px; }
</style></head><body>
<div class='header'>
  <div><h1>{{race.name}}</h1><div class='sub'>{{club.name}} &bull; Season {{season}}</div></div>
  <img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
</div>
<div class='meta'>
  <div class='meta-item'><span>Release Point</span><strong>{{race.releaseLocation}}</strong></div>
  <div class='meta-item'><span>Date</span><strong>{{race.date}}</strong></div>
  <div class='meta-item'><span>Release Time</span><strong>{{race.releaseTime}}</strong></div>
  <div class='meta-item'><span>Distance</span><strong>{{race.distance}} km</strong></div>
  <div class='meta-item'><span>Entries</span><strong>{{race.totalEntries}}</strong></div>
  <div class='meta-item'><span>Wind</span><strong>{{race.wind}}</strong></div>
</div>
<table>
  <thead><tr>
    <th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th>
    <th>Arrival</th><th>Distance (km)</th><th>Velocity (m/min)</th><th>Category</th>
  </tr></thead>
  <tbody>{{#each results}}<tr>
    <td>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td>
    <td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td>{{velocityMperMin}}</td><td>{{categoryName}}</td>
  </tr>{{/each}}</tbody>
</table>
<div class='footer'><span>Printed {{printDate}}</span><span>{{club.name}} &mdash; Official Race Results</span></div>
</body></html>";

    // RR-02 Gold & Black Landscape
    public const string RR02 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
@page { size: A4 landscape; margin: 10mm; }
.header { background: #1a1a1a; color: #C9A84C; padding: 12px 24px; display: flex; justify-content: space-between; align-items: center; border-bottom: 3px solid #C9A84C; }
.header h1 { font-size: 22pt; font-weight: 900; letter-spacing: 2px; text-transform: uppercase; }
.header .details { font-size: 9pt; color: #aaa; text-align: right; }
.ribbon { background: #C9A84C; color: #1a1a1a; display: flex; gap: 40px; padding: 8px 24px; font-size: 9pt; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; }
thead th { background: #C9A84C; color: #1a1a1a; font-weight: 700; font-size: 8.5pt; text-transform: uppercase; letter-spacing: .5px; padding: 7px 8px; }
tbody tr:nth-child(even) { background: #F9F7F0; }
td { font-size: 8.5pt; border-bottom: 1px solid #E8E0CC; }
td:first-child { font-weight: 900; font-size: 13pt; color: #C9A84C; text-align: center; width: 40px; }
.top1 td { background: #FFF8E1 !important; }
.footer { margin-top: 10px; font-size: 8pt; color: #888; text-align: center; }
</style></head><body>
<div class='header'>
  <div><h1>{{race.name}}</h1></div>
  <div class='details'>{{club.name}}<br>{{race.date}} &bull; {{race.releaseLocation}}<br>Entries: {{race.totalEntries}}</div>
</div>
<div class='ribbon'>
  <span>Distance: {{race.distance}} km</span>
  <span>Wind: {{race.wind}}</span>
  <span>Temp: {{race.temperature}}</span>
  <span>Season {{season}}</span>
</div>
<table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Arrival</th><th>Distance</th><th>Velocity (m/min)</th><th>km/h</th><th>Cat.</th></tr></thead>
  <tbody>{{#each results}}<tr class='{{#if isFirst}}top1{{/if}}'>
    <td>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYear}}</td>
    <td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td><strong>{{velocityMperMin}}</strong></td><td>{{velocityKmH}}</td><td>{{categoryName}}</td>
  </tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} Official Results &bull; Printed {{printDate}}</div>
</body></html>";

    // RR-03 Green Federation Style
    public const string RR03 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
.header { display: flex; align-items: center; gap: 16px; border-bottom: 4px double #2D6A4F; padding-bottom: 12px; margin-bottom: 12px; }
.logo { width: 70px; height: 70px; object-fit: contain; }
.header-text h1 { font-size: 20pt; color: #2D6A4F; font-weight: 800; }
.header-text .sub { font-size: 10pt; color: #555; }
.info-grid { display: grid; grid-template-columns: repeat(6,1fr); gap: 8px; background: #F0F7F4; border: 1px solid #B7DFCC; border-radius: 4px; padding: 10px; margin-bottom: 12px; }
.info-item { font-size: 8.5pt; } .info-item span { display: block; color: #2D6A4F; font-weight: 700; font-size: 9pt; }
thead th { background: #2D6A4F; color: #fff; font-size: 9pt; }
tbody tr:nth-child(odd) { background: #F7FBF9; }
tbody tr:hover { background: #E8F4EE; }
td { font-size: 9pt; border-bottom: 1px solid #D4EAE0; }
.rank { font-size: 12pt; font-weight: 900; color: #2D6A4F; text-align: center; }
.vel { font-weight: 700; color: #1B4332; }
.footer { margin-top: 10px; font-size: 8pt; color: #777; border-top: 1px solid #D4EAE0; padding-top: 6px; display: flex; justify-content: space-between; }
</style></head><body>
<div class='header'>
  <img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'"">
  <div class='header-text'><h1>{{race.name}} — Race Results</h1><div class='sub'>{{club.name}} &bull; {{race.date}}</div></div>
</div>
<div class='info-grid'>
  <div class='info-item'><span>Release</span>{{race.releaseLocation}}</div>
  <div class='info-item'><span>Date</span>{{race.date}}</div>
  <div class='info-item'><span>Time</span>{{race.releaseTime}}</div>
  <div class='info-item'><span>Distance</span>{{race.distance}} km</div>
  <div class='info-item'><span>Entries</span>{{race.totalEntries}}</div>
  <div class='info-item'><span>Wind</span>{{race.wind}}</div>
</div>
<table>
  <thead><tr><th style='width:42px'>#</th><th>Ring Number</th><th>Fancier</th><th>Pigeon</th><th>Arrival Time</th><th>Distance (km)</th><th>Velocity m/min</th><th>Velocity km/h</th><th>Category</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank'>{{rank}}</td><td>{{ringNumber}}</td><td>{{fancierName}}</td><td>{{pigeonName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'><span>Official Results — {{club.name}}</span><span>Generated {{printDate}}</span></div>
</body></html>";

    // RR-04 Crimson Compact
    public const string RR04 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
.header { background: #C1121F; color: #fff; padding: 12px 18px; }
.header h1 { font-size: 17pt; font-weight: 900; text-transform: uppercase; letter-spacing: 1px; }
.header .sub { font-size: 9pt; opacity: .85; }
.meta-bar { display: flex; flex-wrap: wrap; gap: 0; background: #F5F5F5; border-bottom: 2px solid #C1121F; }
.meta-bar div { padding: 6px 14px; font-size: 8.5pt; border-right: 1px solid #DDD; }
.meta-bar div strong { display: block; color: #C1121F; font-size: 9pt; }
thead th { background: #2B2D42; color: #fff; font-size: 8.5pt; padding: 6px 8px; text-align: left; }
tbody tr:nth-child(even) { background: #FBF0F0; }
td { font-size: 8.5pt; border-bottom: 1px solid #EBDADA; padding: 5px 8px; }
.rank-cell { font-weight: 900; font-size: 12pt; color: #C1121F; text-align: center; width: 36px; }
.vel-cell { font-weight: 700; }
.gold { background: #FFF8E1 !important; } .silver { background: #F5F5F5 !important; } .bronze { background: #FFF3E0 !important; }
.footer { font-size: 8pt; color: #999; text-align: right; padding: 6px 0; }
</style></head><body>
<div class='header'><h1>{{race.name}}</h1><div class='sub'>{{club.name}} &bull; Official Race Results</div></div>
<div class='meta-bar'>
  <div><strong>Location</strong>{{race.releaseLocation}}</div>
  <div><strong>Date</strong>{{race.date}}</div>
  <div><strong>Release</strong>{{race.releaseTime}}</div>
  <div><strong>Distance</strong>{{race.distance}} km</div>
  <div><strong>Entries</strong>{{race.totalEntries}}</div>
  <div><strong>Wind</strong>{{race.wind}}</div>
  <div><strong>Season</strong>{{season}}</div>
</div>
<table>
  <thead><tr><th style='width:36px'>#</th><th>Ring #</th><th>Pigeon</th><th>Sex</th><th>Fancier</th><th>Arrival</th><th>Dist km</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
  <tbody>{{#each results}}<tr class='{{#if (eq rank 1)}}gold{{else}}{{#if (eq rank 2)}}silver{{else}}{{#if (eq rank 3)}}bronze{{/if}}{{/if}}{{/if}}'>
    <td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td>
  </tr>{{/each}}</tbody>
</table>
<div class='footer'>Printed {{printDate}} &bull; {{club.name}}</div>
</body></html>";

    // RR-05 Ivory & Gold Classic (serif)
    public const string RR05 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + SeriffBase + @"
body { background: #FFFEF8; }
.page-border { border: 3px double #C9A84C; padding: 16px; min-height: 95vh; }
.title-block { text-align: center; border-bottom: 2px solid #C9A84C; padding-bottom: 14px; margin-bottom: 14px; }
.title-block h1 { font-size: 22pt; color: #1E3A5F; letter-spacing: 2px; }
.title-block .club { font-size: 13pt; color: #C9A84C; font-style: italic; margin-top: 4px; }
.title-block .meta { font-size: 9.5pt; color: #666; margin-top: 6px; letter-spacing: 1px; text-transform: uppercase; }
.ornament { text-align: center; font-size: 18pt; color: #C9A84C; line-height: 1; margin: 4px 0; }
thead th { background: #1E3A5F; color: #C9A84C; font-family: Georgia, serif; font-size: 9pt; text-transform: uppercase; letter-spacing: 1px; border: 1px solid #C9A84C; }
tbody tr:nth-child(even) { background: #FBF7EC; }
td { border: 1px solid #E8DFC8; font-size: 9.5pt; }
.rank-cell { color: #C9A84C; font-weight: 700; font-size: 12pt; text-align: center; width: 44px; }
.vel-cell { font-weight: 700; color: #1E3A5F; }
.footer { text-align: center; font-size: 8.5pt; color: #999; margin-top: 12px; font-style: italic; }
</style></head><body>
<div class='page-border'>
<div class='ornament'>&#8730; &mdash; &#8730;</div>
<div class='title-block'>
  <h1>{{race.name}}</h1>
  <div class='club'>{{club.name}}</div>
  <div class='meta'>Official Race Results &bull; {{race.date}} &bull; Season {{season}}</div>
</div>
<div class='ornament'>&#10022; &#10022; &#10022;</div>
<table style='margin-top:10px'>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon Name</th><th>Fancier</th><th>Arrival Time</th><th>Distance km</th><th>Velocity m/min</th><th>Category</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &mdash; {{race.releaseLocation}} &mdash; {{race.totalEntries}} entries &mdash; Printed {{printDate}}</div>
</div></body></html>";

    // RR-06 Dark Mode Landscape
    public const string RR06 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
@page { size: A4 landscape; margin: 10mm; }
body { background: #0D1B2A; color: #E8F0FE; }
.header { background: linear-gradient(135deg, #0D1B2A 0%, #1E3A5F 100%); padding: 14px 20px; display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #1E90FF; }
.header h1 { font-size: 20pt; font-weight: 900; color: #1E90FF; letter-spacing: 1px; }
.header .sub { color: #8FA8C8; font-size: 9pt; }
.meta-bar { background: #132030; display: flex; gap: 32px; padding: 8px 20px; border-bottom: 1px solid #1E3A5F; font-size: 8.5pt; color: #8FA8C8; }
.meta-bar strong { color: #1E90FF; display: block; }
thead th { background: #1E3A5F; color: #1E90FF; font-size: 8.5pt; border-bottom: 2px solid #1E90FF; padding: 7px 8px; }
tbody tr { border-bottom: 1px solid #1A2940; }
tbody tr:nth-child(even) { background: #0F1E2E; }
tbody tr:nth-child(1) td { color: #FFD740; }
td { font-size: 8.5pt; color: #C8D8F0; padding: 6px 8px; }
.rank-cell { color: #1E90FF; font-weight: 900; font-size: 13pt; text-align: center; width: 40px; }
.vel-cell { color: #00E676; font-weight: 700; }
.footer { color: #4A6080; font-size: 8pt; text-align: center; padding: 8px; }
</style></head><body>
<div class='header'>
  <div><h1>{{race.name}}</h1><div class='sub'>{{club.name}} &bull; Season {{season}}</div></div>
  <div class='sub' style='text-align:right'>{{race.releaseLocation}}<br>{{race.date}}</div>
</div>
<div class='meta-bar'>
  <div><strong>Distance</strong>{{race.distance}} km</div>
  <div><strong>Wind</strong>{{race.wind}}</div>
  <div><strong>Temp</strong>{{race.temperature}}</div>
  <div><strong>Entries</strong>{{race.totalEntries}}</div>
  <div><strong>Release</strong>{{race.releaseTime}}</div>
</div>
<table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Fancier</th><th>Arrival</th><th>Distance km</th><th>Velocity m/min</th><th>km/h</th><th>Category</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} Official Results &bull; Printed {{printDate}}</div>
</body></html>";

    // RR-07 Minimal Whitespace
    public const string RR07 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #111; }
.header { padding: 20px 0 14px; border-bottom: 1px solid #111; }
.header h1 { font-size: 24pt; font-weight: 900; letter-spacing: -1px; }
.header .sub { font-size: 10pt; color: #666; margin-top: 2px; }
.meta { display: flex; gap: 32px; padding: 10px 0; border-bottom: 1px solid #DDD; margin-bottom: 12px; font-size: 8.5pt; color: #666; }
.meta strong { display: block; color: #111; font-size: 9pt; }
thead th { font-size: 8pt; text-transform: uppercase; letter-spacing: 1px; color: #666; font-weight: 600; border-bottom: 2px solid #111; padding: 5px 6px; text-align: left; }
tbody tr { border-bottom: 1px solid #EEE; }
td { font-size: 9.5pt; padding: 6px 6px; }
.rank-cell { font-size: 11pt; font-weight: 900; text-align: center; width: 36px; }
.vel-cell { font-weight: 700; }
.top3 td:first-child::before { content: ''; }
tr:nth-child(1) .rank-cell { color: #D4A017; }
tr:nth-child(2) .rank-cell { color: #888; }
tr:nth-child(3) .rank-cell { color: #CD7F32; }
.footer { font-size: 8pt; color: #AAA; margin-top: 12px; }
</style></head><body>
<div class='header'><h1>{{race.name}}</h1><div class='sub'>{{club.name}} &bull; {{race.date}}</div></div>
<div class='meta'>
  <div><strong>Release</strong>{{race.releaseLocation}}</div>
  <div><strong>Time</strong>{{race.releaseTime}}</div>
  <div><strong>Distance</strong>{{race.distance}} km</div>
  <div><strong>Entries</strong>{{race.totalEntries}}</div>
  <div><strong>Season</strong>{{season}}</div>
</div>
<table>
  <thead><tr><th>#</th><th>Ring</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Dist</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &mdash; Printed {{printDate}}</div>
</body></html>";

    // RR-08 Sporty Bold Stripes
    public const string RR08 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
@page { size: A4 landscape; margin: 8mm; }
body { font-family: 'Arial Black', Impact, sans-serif; }
.banner { background: repeating-linear-gradient(45deg, #E63946 0 12px, #1a1a1a 12px 24px); height: 10px; }
.header { background: #E63946; color: #fff; padding: 10px 20px; display: flex; justify-content: space-between; align-items: flex-end; }
.header h1 { font-size: 24pt; font-weight: 900; letter-spacing: 1px; text-transform: uppercase; }
.header .info { text-align: right; font-size: 9pt; font-family: Arial, sans-serif; }
.meta-strip { background: #1a1a1a; color: #E63946; display: flex; gap: 30px; padding: 6px 20px; font-size: 9pt; text-transform: uppercase; letter-spacing: 1px; font-family: Arial, sans-serif; }
thead th { background: #E63946; color: #fff; font-size: 8.5pt; text-transform: uppercase; letter-spacing: .5px; padding: 6px 7px; text-align: left; }
tbody tr:nth-child(odd) { background: #F9F9F9; }
tbody tr:nth-child(1) { background: #FFF8E1; }
td { font-size: 8.5pt; font-family: Arial, sans-serif; padding: 5px 7px; border-bottom: 1px solid #EEE; }
.rank-cell { font-size: 14pt; font-weight: 900; color: #E63946; text-align: center; width: 36px; }
.vel-cell { color: #1a1a1a; font-weight: 700; }
.banner2 { background: repeating-linear-gradient(45deg, #E63946 0 6px, #1a1a1a 6px 12px); height: 6px; margin-top: 8px; }
</style></head><body>
<div class='banner'></div>
<div class='header'>
  <h1>{{race.name}}</h1>
  <div class='info'>{{club.name}}<br>{{race.date}} &bull; {{race.releaseLocation}}</div>
</div>
<div class='meta-strip'>
  <span>Dist: {{race.distance}} km</span>
  <span>Entries: {{race.totalEntries}}</span>
  <span>Wind: {{race.wind}}</span>
  <span>Season {{season}}</span>
</div>
<table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Arrival</th><th>Distance</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYear}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='banner2'></div>
</body></html>";

    // RR-09 Royal Purple
    public const string RR09 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
.header { background: #4A0E8F; color: #fff; padding: 14px 20px; border-bottom: 3px solid #D4A017; }
.header h1 { font-size: 19pt; font-weight: 800; }
.header .sub { font-size: 9pt; opacity: .8; }
.info-row { display: flex; gap: 24px; background: #F5F0FF; padding: 8px 20px; border-bottom: 1px solid #C4A8E8; font-size: 9pt; }
.info-row div { display: flex; flex-direction: column; }
.info-row strong { color: #4A0E8F; font-size: 9.5pt; }
thead th { background: #4A0E8F; color: #D4A017; font-size: 9pt; padding: 7px 8px; text-align: left; }
tbody tr:nth-child(even) { background: #FBF8FF; }
td { font-size: 9pt; border-bottom: 1px solid #E8E0F5; }
.rank-cell { color: #4A0E8F; font-weight: 900; font-size: 13pt; text-align: center; width: 44px; }
.vel-cell { color: #4A0E8F; font-weight: 700; }
.footer { font-size: 8pt; color: #888; margin-top: 8px; text-align: center; }
</style></head><body>
<div class='header'><h1>{{race.name}}</h1><div class='sub'>{{club.name}} &bull; Season {{season}}</div></div>
<div class='info-row'>
  <div><strong>Release</strong>{{race.releaseLocation}}</div>
  <div><strong>Date</strong>{{race.date}}</div>
  <div><strong>Time</strong>{{race.releaseTime}}</div>
  <div><strong>Distance</strong>{{race.distance}} km</div>
  <div><strong>Entries</strong>{{race.totalEntries}}</div>
  <div><strong>Wind</strong>{{race.wind}}</div>
</div>
<table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Distance km</th><th>Velocity m/min</th><th>km/h</th><th>Category</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} Official Results &bull; {{printDate}}</div>
</body></html>";

    // RR-10 Teal & White Modern
    public const string RR10 = @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
" + PrintBase + @"
body { font-family: 'Segoe UI', sans-serif; }
.top-bar { height: 6px; background: linear-gradient(90deg, #00B4D8, #0077B6); }
.header { padding: 14px 20px; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #E0F4FA; }
.header h1 { font-size: 18pt; color: #0077B6; font-weight: 800; }
.header .right { font-size: 9pt; color: #666; text-align: right; }
.meta { display: grid; grid-template-columns: repeat(7,1fr); gap: 0; border: 1px solid #B2EBF2; border-radius: 4px; overflow: hidden; margin: 10px 20px; }
.meta-item { padding: 7px 10px; font-size: 8.5pt; border-right: 1px solid #B2EBF2; }
.meta-item:last-child { border-right: none; }
.meta-item strong { display: block; color: #0077B6; font-size: 9pt; }
thead th { background: #0077B6; color: #fff; font-size: 8.5pt; padding: 7px 8px; text-align: left; }
tbody tr:nth-child(even) { background: #F0FAFD; }
tbody tr:nth-child(1) td { background: #E0F8FF; font-weight: 600; }
td { font-size: 9pt; border-bottom: 1px solid #E0F4FA; padding: 5px 8px; }
.rank-cell { color: #0077B6; font-weight: 900; font-size: 13pt; text-align: center; width: 40px; }
.vel-cell { color: #006494; font-weight: 700; }
.footer { padding: 6px 20px; font-size: 8pt; color: #999; display: flex; justify-content: space-between; }
</style></head><body>
<div class='top-bar'></div>
<div class='header'>
  <div><h1>{{race.name}}</h1><div style='font-size:9pt;color:#666'>{{club.name}} &bull; Official Race Results</div></div>
  <div class='right'>{{race.date}}<br>Season {{season}}</div>
</div>
<div class='meta'>
  <div class='meta-item'><strong>Location</strong>{{race.releaseLocation}}</div>
  <div class='meta-item'><strong>Date</strong>{{race.date}}</div>
  <div class='meta-item'><strong>Release</strong>{{race.releaseTime}}</div>
  <div class='meta-item'><strong>Distance</strong>{{race.distance}} km</div>
  <div class='meta-item'><strong>Entries</strong>{{race.totalEntries}}</div>
  <div class='meta-item'><strong>Wind</strong>{{race.wind}}</div>
  <div class='meta-item'><strong>Temp</strong>{{race.temperature}}</div>
</div>
<table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Fancier</th><th>Arrival</th><th>Dist km</th><th>m/min</th><th>km/h</th><th>Category</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank-cell'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel-cell'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'><span>{{club.name}} &mdash; {{race.releaseLocation}}</span><span>Printed {{printDate}}</span></div>
</body></html>";

    // RR-11 to RR-25: additional race result templates defined as static field arrays
    // (styles: Olive, Slate, Rose, Amber, Two-column, Category-grouped, Top10 large, etc.)

    public static readonly (string Id, string Name, string Style, string Html)[] RaceResultTemplatesExtra = new[]
    {
        ("RR-11","Olive Federation","Heritage", BuildVariant("Olive & Cream","#4A5240","#D4C89A","#FAFAF4")),
        ("RR-12","Slate Corporate","Corporate", BuildVariant("Slate Blue","#475569","#94A3B8","#F8FAFC")),
        ("RR-13","Rose & White","Vibrant", BuildVariant("Rose","#BE123C","#FDA4AF","#FFF1F2")),
        ("RR-14","Amber Harvest","Classic", BuildVariant("Amber","#92400E","#FCD34D","#FFFBEB")),
        ("RR-15","Forest Green Deep","Classic", BuildVariant("Forest","#14532D","#86EFAC","#F0FDF4")),
        ("RR-16","Ocean Blue","Modern", BuildVariant("Ocean","#1E40AF","#93C5FD","#EFF6FF")),
        ("RR-17","Sunset Orange","Sporty", BuildVariant("Sunset","#C2410C","#FB923C","#FFF7ED")),
        ("RR-18","Classic Burgundy","Heritage", BuildVariant("Burgundy","#7F1D1D","#F9A8A8","#FFF5F5")),
        ("RR-19","Midnight Teal","Dark", BuildVariant("Midnight Teal","#134E4A","#5EEAD4","#F0FDFA")),
        ("RR-20","Soft Lavender","Elegant", BuildVariant("Lavender","#4C1D95","#C4B5FD","#F5F3FF")),
        ("RR-21","Copper & Cream","Elegant", BuildVariant("Copper","#92400E","#D97706","#FEFCE8")),
        ("RR-22","Ice Blue","Minimal", BuildVariant("Ice","#1E3A8A","#BAE6FD","#F0F9FF")),
        ("RR-23","Carbon & Lime","Sporty", BuildVariant("Carbon","#1C1917","#A3E635","#1C1917")),
        ("RR-24","Dusty Rose","Classic", BuildVariant("Dusty Rose","#831843","#FBCFE8","#FDF2F8")),
        ("RR-25","Electric Blue","Modern", BuildVariant("Electric","#1D4ED8","#38BDF8","#F0F9FF")),
    };

    private static string BuildVariant(string label, string primary, string accent, string bg) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
{PrintBase}
body {{ background: {bg}; }}
.header {{ background: {primary}; color: #fff; padding: 13px 20px; }}
.header h1 {{ font-size: 18pt; font-weight: 800; }}
.header .sub {{ font-size: 9pt; opacity: .8; }}
.meta {{ display:flex; gap:20px; background: {accent}22; padding: 8px 20px; border-bottom: 2px solid {accent}; font-size: 9pt; }}
.meta strong {{ color: {primary}; display:block; }}
thead th {{ background: {primary}; color: {accent}; font-size: 9pt; padding: 7px 8px; text-align: left; }}
tbody tr:nth-child(even) {{ background: {accent}11; }}
td {{ font-size: 9pt; border-bottom: 1px solid {accent}44; padding: 5px 8px; }}
.rank-cell {{ font-size: 13pt; font-weight: 900; color: {primary}; text-align:center; width:44px; }}
.vel-cell {{ font-weight: 700; color: {primary}; }}
.footer {{ font-size: 8pt; color: #999; text-align:center; padding: 6px; }}
</style></head><body>
<div class='header'><h1>{{{{race.name}}}}</h1><div class='sub'>{{{{club.name}}}} &bull; {label} &bull; Season {{{{season}}}}</div></div>
<div class='meta'>
  <div><strong>Release</strong>{{{{race.releaseLocation}}}}</div>
  <div><strong>Date</strong>{{{{race.date}}}}</div>
  <div><strong>Distance</strong>{{{{race.distance}}}} km</div>
  <div><strong>Entries</strong>{{{{race.totalEntries}}}}</div>
  <div><strong>Wind</strong>{{{{race.wind}}}}</div>
</div>
<table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Dist km</th><th>m/min</th><th>km/h</th><th>Category</th></tr></thead>
  <tbody>{{{{#each results}}}}<tr><td class='rank-cell'>{{{{rank}}}}</td><td>{{{{ringNumber}}}}</td><td>{{{{pigeonName}}}}</td><td>{{{{fancierName}}}}</td><td>{{{{arrivalTime}}}}</td><td>{{{{distanceKm}}}}</td><td class='vel-cell'>{{{{velocityMperMin}}}}</td><td>{{{{velocityKmH}}}}</td><td>{{{{categoryName}}}}</td></tr>{{{{/each}}}}</tbody>
</table>
<div class='footer'>{{{{club.name}}}} &mdash; Printed {{{{printDate}}}}</div>
</body></html>";
}
