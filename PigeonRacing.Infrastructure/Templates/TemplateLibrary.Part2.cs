namespace PigeonRacing.Infrastructure.Templates;

public static partial class TemplateLibrary
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  RACE RESULTS — templates 26-50 (bulk colour variants + special layouts)
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Style, string Html)[] RaceResultTemplates26To50 = new[]
    {
        ("RR-26","Ruby Red Landscape","Sporty",      BuildRRLandscape("Ruby","#9B1C1C","#FCA5A5","#FFF5F5")),
        ("RR-27","Jade Corporate","Corporate",        BuildRRLandscape("Jade","#065F46","#6EE7B7","#ECFDF5")),
        ("RR-28","Cobalt Landscape","Modern",         BuildRRLandscape("Cobalt","#1E3A8A","#93C5FD","#EFF6FF")),
        ("RR-29","Terracotta Warm","Heritage",        BuildRRPortrait("Terracotta","#7C2D12","#FED7AA","#FFFBF7")),
        ("RR-30","Chartreuse Bold","Sporty",          BuildRRPortrait("Chartreuse","#365314","#BEF264","#F7FEE7")),
        ("RR-31","Steel & Silver","Corporate",        BuildRRPortrait("Steel","#374151","#D1D5DB","#F9FAFB")),
        ("RR-32","Maroon Heritage","Heritage",        BuildRRPortrait("Maroon","#4C1130","#F9A8D4","#FDF2F8")),
        ("RR-33","Sky Blue Summer","Vibrant",         BuildRRPortrait("Sky","#075985","#BAE6FD","#F0F9FF")),
        ("RR-34","Pumpkin Harvest","Classic",         BuildRRPortrait("Pumpkin","#9A3412","#FDBA74","#FFF7ED")),
        ("RR-35","Pine Forest","Classic",             BuildRRPortrait("Pine","#14532D","#4ADE80","#F0FDF4")),
        ("RR-36","Platinum Light","Minimal",          BuildRRPortrait("Platinum","#1F2937","#E5E7EB","#FFFFFF")),
        ("RR-37","Bright Aqua","Vibrant",             BuildRRPortrait("Aqua","#0E7490","#67E8F9","#ECFEFF")),
        ("RR-38","Classic Walnut","Heritage",         BuildRRPortrait("Walnut","#78350F","#D97706","#FEFCE8")),
        ("RR-39","Sapphire Blue","Elegant",           BuildRRPortrait("Sapphire","#1E3A8A","#93C5FD","#EFF6FF")),
        ("RR-40","Warm Taupe","Minimal",              BuildRRPortrait("Taupe","#44403C","#D6D3D1","#FAFAF9")),
        ("RR-41","Electric Violet","Modern",          BuildRRPortrait("Violet","#5B21B6","#C4B5FD","#F5F3FF")),
        ("RR-42","Rose Gold","Elegant",               BuildRRPortrait("Rose Gold","#9F1239","#FCA5A1","#FFF1F2")),
        ("RR-43","Deep Indigo","Dark",                BuildRRPortrait("Indigo","#312E81","#A5B4FC","#EEF2FF")),
        ("RR-44","Moss & Cream","Heritage",           BuildRRPortrait("Moss","#3F6212","#BEF264","#F7FEE7")),
        ("RR-45","Coral Reef","Vibrant",              BuildRRPortrait("Coral","#BE123C","#FCA5A5","#FFF1F2")),
        ("RR-46","Branded Club A4 Portrait","Branded",BuildRRBranded(false)),
        ("RR-47","Branded Club A4 Landscape","Branded",BuildRRBranded(true)),
        ("RR-48","Top 10 Podium Portrait","Modern",   BuildRRTop10()),
        ("RR-49","Multi-Category Grouped","Corporate", BuildRRCategoryGrouped()),
        ("RR-50","Ultra Compact Dense","Minimal",     BuildRRCompact()),
    };

    private static string BuildRRPortrait(string label, string primary, string accent, string bg) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page {{margin:12mm}} @media print {{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}} body{{font-family:'Segoe UI',Arial,sans-serif;font-size:9.5pt;background:{bg};color:#111}}
.h{{background:{primary};color:#fff;padding:12px 18px}}.h h1{{font-size:17pt;font-weight:800}}.h .s{{font-size:9pt;opacity:.8}}
.m{{display:flex;gap:16px;background:{accent}22;padding:7px 18px;border-bottom:2px solid {accent};font-size:8.5pt}}
.m strong{{color:{primary};display:block}}
thead th{{background:{primary};color:{accent};font-size:8.5pt;padding:6px 8px;text-align:left}}
tbody tr:nth-child(even){{background:{accent}11}} td{{font-size:8.5pt;border-bottom:1px solid {accent}44;padding:5px 8px}}
.r{{font-size:12pt;font-weight:900;color:{primary};text-align:center;width:40px}}.v{{font-weight:700;color:{primary}}}
.f{{font-size:8pt;color:#999;text-align:center;padding:6px}}
</style></head><body>
<div class='h'><h1>{{{{race.name}}}}</h1><div class='s'>{{{{club.name}}}} &bull; {label}</div></div>
<div class='m'><div><strong>Location</strong>{{{{race.releaseLocation}}}}</div><div><strong>Date</strong>{{{{race.date}}}}</div><div><strong>Distance</strong>{{{{race.distance}}}} km</div><div><strong>Entries</strong>{{{{race.totalEntries}}}}</div><div><strong>Wind</strong>{{{{race.wind}}}}</div><div><strong>Season</strong>{{{{season}}}}</div></div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Dist</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
<tbody>{{{{#each results}}}}<tr><td class='r'>{{{{rank}}}}</td><td>{{{{ringNumber}}}}</td><td>{{{{pigeonName}}}}</td><td>{{{{fancierName}}}}</td><td>{{{{arrivalTime}}}}</td><td>{{{{distanceKm}}}}</td><td class='v'>{{{{velocityMperMin}}}}</td><td>{{{{velocityKmH}}}}</td><td>{{{{categoryName}}}}</td></tr>{{{{/each}}}}</tbody>
</table><div class='f'>{{{{club.name}}}} &mdash; Printed {{{{printDate}}}}</div></body></html>";

    private static string BuildRRLandscape(string label, string primary, string accent, string bg) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page {{size:A4 landscape;margin:10mm}} @media print {{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}} body{{font-family:'Segoe UI',Arial,sans-serif;font-size:9pt;background:{bg};color:#111}}
.h{{background:{primary};color:#fff;padding:10px 20px;display:flex;justify-content:space-between;align-items:center}}
.h h1{{font-size:20pt;font-weight:900}}.h .s{{font-size:9pt;opacity:.8;text-align:right}}
.m{{display:flex;gap:24px;background:{accent}22;padding:7px 20px;border-bottom:2px solid {accent};font-size:8.5pt}}
.m strong{{color:{primary};display:block}}
thead th{{background:{primary};color:{accent};font-size:8pt;padding:6px 7px;text-align:left}}
tbody tr:nth-child(even){{background:{accent}11}} td{{font-size:8pt;border-bottom:1px solid {accent}33;padding:4px 7px}}
.r{{font-size:12pt;font-weight:900;color:{primary};text-align:center;width:36px}}.v{{font-weight:700}}
.f{{font-size:8pt;color:#999;text-align:center;padding:5px}}
</style></head><body>
<div class='h'><h1>{{{{race.name}}}}</h1><div class='s'>{{{{club.name}}}}<br>{{{{race.date}}}} &bull; {label}</div></div>
<div class='m'><div><strong>Release</strong>{{{{race.releaseLocation}}}}</div><div><strong>Date</strong>{{{{race.date}}}}</div><div><strong>Time</strong>{{{{race.releaseTime}}}}</div><div><strong>Distance</strong>{{{{race.distance}}}} km</div><div><strong>Entries</strong>{{{{race.totalEntries}}}}</div><div><strong>Wind</strong>{{{{race.wind}}}}</div><div><strong>Temp</strong>{{{{race.temperature}}}}</div><div><strong>Season</strong>{{{{season}}}}</div></div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Arrival</th><th>Dist km</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
<tbody>{{{{#each results}}}}<tr><td class='r'>{{{{rank}}}}</td><td>{{{{ringNumber}}}}</td><td>{{{{pigeonName}}}}</td><td>{{{{pigeonSex}}}}</td><td>{{{{pigeonYear}}}}</td><td>{{{{fancierName}}}}</td><td>{{{{arrivalTime}}}}</td><td>{{{{distanceKm}}}}</td><td class='v'>{{{{velocityMperMin}}}}</td><td>{{{{velocityKmH}}}}</td><td>{{{{categoryName}}}}</td></tr>{{{{/each}}}}</tbody>
</table><div class='f'>{{{{club.name}}}} &mdash; Printed {{{{printDate}}}}</div></body></html>";

    private static string BuildRRBranded(bool landscape) => @"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page {" + (landscape ? "size:A4 landscape;" : "") + @"margin:12mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Segoe UI',Arial,sans-serif;font-size:9.5pt;color:#111}
.h{background:{{club.primaryColour}};color:#fff;padding:14px 20px;display:flex;justify-content:space-between;align-items:center}
.h h1{font-size:18pt;font-weight:800}.h .s{font-size:9pt;opacity:.8}
.logo{width:56px;height:56px;object-fit:contain;background:rgba(255,255,255,.15);border-radius:4px;padding:4px}
.m{display:flex;gap:20px;background:{{club.secondaryColour}}22;padding:8px 20px;border-bottom:2px solid {{club.secondaryColour}};font-size:9pt}
.m strong{color:{{club.primaryColour}};display:block}
thead th{background:{{club.primaryColour}};color:{{club.secondaryColour}};font-size:9pt;padding:7px 8px;text-align:left}
tbody tr:nth-child(even){background:{{club.secondaryColour}}11} td{font-size:9pt;border-bottom:1px solid {{club.secondaryColour}}44;padding:5px 8px}
.r{font-size:13pt;font-weight:900;color:{{club.primaryColour}};text-align:center;width:44px}.v{font-weight:700;color:{{club.primaryColour}}}
.f{font-size:8pt;color:#999;text-align:center;padding:6px}
</style></head><body>
<div class='h'><div><h1>{{race.name}}</h1><div class='s'>{{club.name}} &bull; Branded</div></div><img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'""></div>
<div class='m'><div><strong>Location</strong>{{race.releaseLocation}}</div><div><strong>Date</strong>{{race.date}}</div><div><strong>Distance</strong>{{race.distance}} km</div><div><strong>Entries</strong>{{race.totalEntries}}</div><div><strong>Wind</strong>{{race.wind}}</div><div><strong>Season</strong>{{season}}</div></div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Dist km</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
<tbody>{{#each results}}<tr><td class='r'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='v'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table><div class='f'>{{club.name}} Official Results &bull; Printed {{printDate}}</div></body></html>";

    private static string BuildRRTop10() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:14mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Segoe UI',Arial,sans-serif;color:#111}
.header{text-align:center;padding:18px;border-bottom:4px solid #1E3A5F}
.header h1{font-size:24pt;color:#1E3A5F;font-weight:900;letter-spacing:1px}
.header .sub{font-size:11pt;color:#C9A84C;font-weight:600}
.header .meta{font-size:9pt;color:#666;margin-top:4px}
.podium{display:flex;justify-content:center;gap:12px;margin:16px 0;align-items:flex-end}
.pod{text-align:center;padding:8px 12px;border-radius:4px;min-width:120px}
.pod-1{background:#FFF8E1;border:2px solid #D4A017;order:2}
.pod-2{background:#F5F5F5;border:2px solid #888;order:1}
.pod-3{background:#FFF3E0;border:2px solid #CD7F32;order:3}
.pod .medal{font-size:24pt;display:block}
.pod .ring{font-size:9pt;font-weight:700;color:#333}
.pod .name{font-size:9pt;color:#555}
.pod .vel{font-size:12pt;font-weight:900;color:#1E3A5F}
.rest{margin-top:12px}
thead th{background:#1E3A5F;color:#C9A84C;font-size:9pt;padding:7px 10px;text-align:left}
tbody tr:nth-child(even){background:#F5F8FC}
td{font-size:10pt;border-bottom:1px solid #E8EDF2;padding:6px 10px}
.rank{font-size:13pt;font-weight:900;color:#1E3A5F;text-align:center;width:48px}
.footer{text-align:center;font-size:8pt;color:#999;margin-top:12px}
</style></head><body>
<div class='header'>
  <h1>{{race.name}}</h1>
  <div class='sub'>{{club.name}} — Top 10</div>
  <div class='meta'>{{race.releaseLocation}} &bull; {{race.date}} &bull; {{race.distance}} km &bull; {{race.totalEntries}} entries</div>
</div>
{{#if results.[0]}}
<div class='podium'>
  <div class='pod pod-2'><span class='medal'>🥈</span><div class='ring'>{{results.[1].ringNumber}}</div><div class='name'>{{results.[1].fancierName}}</div><div class='vel'>{{results.[1].velocityMperMin}}</div><div style='font-size:8pt;color:#777'>m/min</div></div>
  <div class='pod pod-1'><span class='medal'>🥇</span><div class='ring'>{{results.[0].ringNumber}}</div><div class='name'>{{results.[0].fancierName}}</div><div class='vel'>{{results.[0].velocityMperMin}}</div><div style='font-size:8pt;color:#777'>m/min</div></div>
  <div class='pod pod-3'><span class='medal'>🥉</span><div class='ring'>{{results.[2].ringNumber}}</div><div class='name'>{{results.[2].fancierName}}</div><div class='vel'>{{results.[2].velocityMperMin}}</div><div style='font-size:8pt;color:#777'>m/min</div></div>
</div>
{{/if}}
<div class='rest'><table>
  <thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Distance km</th><th>m/min</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td style='font-weight:700'>{{velocityMperMin}}</td></tr>{{/each}}</tbody>
</table></div>
<div class='footer'>{{club.name}} &bull; {{printDate}}</div></body></html>";

    private static string BuildRRCategoryGrouped() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:12mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Segoe UI',Arial,sans-serif;font-size:9.5pt;color:#111}
.header{background:#1E3A5F;color:#fff;padding:13px 20px}.header h1{font-size:18pt;font-weight:800}.header .sub{font-size:9pt;opacity:.8}
.race-meta{display:flex;gap:20px;background:#F0F4F8;padding:8px 20px;border-bottom:2px solid #1E3A5F;font-size:9pt}
.race-meta strong{color:#1E3A5F;display:block}
.cat-header{background:#E8EDF5;color:#1E3A5F;font-weight:800;font-size:11pt;padding:8px 12px;margin-top:12px;border-left:4px solid #1E3A5F}
thead th{background:#4A6FA5;color:#fff;font-size:8.5pt;padding:6px 8px;text-align:left}
tbody tr:nth-child(even){background:#F5F8FC} td{font-size:8.5pt;border-bottom:1px solid #E0E8F0;padding:5px 8px}
.rank{font-size:12pt;font-weight:900;color:#1E3A5F;text-align:center;width:40px}.vel{font-weight:700}
.footer{font-size:8pt;color:#999;text-align:center;padding:6px;margin-top:8px}
</style></head><body>
<div class='header'><h1>{{race.name}}</h1><div class='sub'>{{club.name}} &bull; Results by Category</div></div>
<div class='race-meta'><div><strong>Location</strong>{{race.releaseLocation}}</div><div><strong>Date</strong>{{race.date}}</div><div><strong>Distance</strong>{{race.distance}} km</div><div><strong>Entries</strong>{{race.totalEntries}}</div><div><strong>Wind</strong>{{race.wind}}</div></div>
{{#each categories}}
<div class='cat-header'>{{name}} ({{count}} entries)</div>
<table><thead><tr><th>Cat#</th><th>Club#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Dist</th><th>m/min</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{categoryRank}}</td><td style='color:#666'>{{clubRank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='vel'>{{velocityMperMin}}</td></tr>{{/each}}</tbody>
</table>{{/each}}
<div class='footer'>{{club.name}} &bull; Printed {{printDate}}</div></body></html>";

    private static string BuildRRCompact() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:8mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:Arial,sans-serif;font-size:8pt;color:#111}
.h{background:#333;color:#fff;padding:7px 12px;display:flex;justify-content:space-between}
.h h1{font-size:12pt;font-weight:900}.h .s{font-size:8pt;opacity:.8;text-align:right}
.m{display:flex;gap:14px;background:#F5F5F5;padding:5px 12px;font-size:7.5pt;border-bottom:1px solid #CCC}
thead th{background:#555;color:#fff;font-size:7.5pt;padding:4px 6px;text-align:left}
tbody tr:nth-child(even){background:#F8F8F8} td{font-size:7.5pt;border-bottom:1px solid #EEE;padding:3px 6px}
.r{font-size:10pt;font-weight:900;text-align:center;color:#333;width:28px}.v{font-weight:700}
</style></head><body>
<div class='h'><h1>{{race.name}}</h1><div class='s'>{{club.name}}<br>{{race.date}} &bull; {{race.distance}} km &bull; {{race.totalEntries}} entries</div></div>
<div class='m'><span>Release: {{race.releaseLocation}}</span><span>Time: {{race.releaseTime}}</span><span>Wind: {{race.wind}}</span><span>Season {{season}}</span></div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Fancier</th><th>Arrival</th><th>Dist</th><th>m/min</th><th>km/h</th><th>Cat</th></tr></thead>
<tbody>{{#each results}}<tr><td class='r'>{{rank}}</td><td>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{fancierName}}</td><td>{{arrivalTime}}</td><td>{{distanceKm}}</td><td class='v'>{{velocityMperMin}}</td><td>{{velocityKmH}}</td><td>{{categoryName}}</td></tr>{{/each}}</tbody>
</table></body></html>";

    // ═══════════════════════════════════════════════════════════════════════════
    //  BEST LOFT — 20 TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Style, string Html)[] BestLoftTemplates = new[]
    {
        ("BL-01","Best Loft Classic Navy",    "Classic",   BuildBL("Classic Navy",   "#1E3A5F","#C9A84C","#F0F4F8",false)),
        ("BL-02","Best Loft Gold Landscape",  "Elegant",   BuildBL("Gold",           "#1a1a1a","#C9A84C","#FAFAF0",true)),
        ("BL-03","Best Loft Green Federation","Classic",   BuildBL("Federation",     "#2D6A4F","#F4A261","#F0F7F4",false)),
        ("BL-04","Best Loft Crimson",         "Sporty",    BuildBL("Crimson",        "#C1121F","#2B2D42","#FFF5F5",false)),
        ("BL-05","Best Loft Ivory & Gold",    "Elegant",   BuildBLSerifBordered()),
        ("BL-06","Best Loft Dark Mode",       "Dark",      BuildBL("Dark",           "#0D1B2A","#1E90FF","#0D1B2A",true)),
        ("BL-07","Best Loft Minimal",         "Minimal",   BuildBLMinimal()),
        ("BL-08","Best Loft Purple Royal",    "Elegant",   BuildBL("Royal Purple",   "#4A0E8F","#D4A017","#F5F0FF",false)),
        ("BL-09","Best Loft Teal Modern",     "Modern",    BuildBL("Teal",           "#0077B6","#00B4D8","#F0F9FF",false)),
        ("BL-10","Best Loft Branded",         "Branded",   BuildBLBranded()),
        ("BL-11","Best Loft Ruby Red",        "Sporty",    BuildBL("Ruby",           "#9B1C1C","#FCA5A5","#FFF5F5",false)),
        ("BL-12","Best Loft Slate Corp",      "Corporate", BuildBL("Slate",          "#475569","#94A3B8","#F8FAFC",false)),
        ("BL-13","Best Loft Ocean Blue",      "Modern",    BuildBL("Ocean",          "#1E40AF","#93C5FD","#EFF6FF",false)),
        ("BL-14","Best Loft Amber Harvest",   "Heritage",  BuildBL("Amber",          "#92400E","#FCD34D","#FFFBEB",false)),
        ("BL-15","Best Loft Forest Deep",     "Heritage",  BuildBL("Forest",         "#14532D","#86EFAC","#F0FDF4",false)),
        ("BL-16","Best Loft Rose Gold",       "Elegant",   BuildBL("Rose Gold",      "#9F1239","#FCA5A1","#FFF1F2",false)),
        ("BL-17","Best Loft Cobalt Wide",     "Modern",    BuildBL("Cobalt",         "#1E3A8A","#93C5FD","#EFF6FF",true)),
        ("BL-18","Best Loft Vintage Brown",   "Heritage",  BuildBL("Vintage",        "#78350F","#D97706","#FEFCE8",false)),
        ("BL-19","Best Loft Carbon Black",    "Sporty",    BuildBL("Carbon",         "#1C1917","#A3E635","#1C1917",true)),
        ("BL-20","Best Loft Lavender",        "Elegant",   BuildBL("Lavender",       "#4C1D95","#C4B5FD","#F5F3FF",false)),
    };

    private static string BuildBL(string label, string primary, string accent, string bg, bool landscape) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page {{{(landscape ? "size:A4 landscape;" : "")}margin:12mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}} body{{font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;background:{bg};color:#111}}
.header{{background:{primary};color:#fff;padding:14px 20px}}
.header h1{{font-size:19pt;font-weight:800}}.header .sub{{font-size:9.5pt;opacity:.8}}
.prog-meta{{display:flex;gap:20px;background:{accent}22;padding:9px 20px;border-bottom:2px solid {accent};font-size:9pt}}
.prog-meta strong{{color:{primary};display:block}}
.season-badge{{background:{accent};color:{primary};font-weight:900;font-size:10pt;padding:3px 12px;border-radius:3px;display:inline-block;margin:8px 20px}}
thead th{{background:{primary};color:{accent};font-size:9pt;padding:7px 9px;text-align:left}}
thead th:first-child{{width:48px;text-align:center}}
tbody tr:nth-child(even){{background:{accent}11}}
tbody tr:nth-child(1) td{{background:{accent}33;font-weight:700}}
tbody tr:nth-child(2) td{{background:{accent}22}}
tbody tr:nth-child(3) td{{background:{accent}11}}
td{{font-size:9.5pt;border-bottom:1px solid {accent}44;padding:6px 9px}}
.rank{{font-size:14pt;font-weight:900;color:{primary};text-align:center;width:48px}}
.score{{font-weight:700;color:{primary}}}
.footer{{font-size:8pt;color:#888;text-align:center;padding:7px;margin-top:8px}}
.breakdown{{font-size:7.5pt;color:#777;display:flex;gap:8px;flex-wrap:wrap}}
</style></head><body>
<div class='header'><h1>🏠 Best Loft Results</h1><div class='sub'>{{{{programme.name}}}} &bull; {{{{club.name}}}} &bull; {label}</div></div>
<div class='prog-meta'>
  <div><strong>Programme</strong>{{{{programme.name}}}}</div>
  <div><strong>Season</strong>{{{{programme.year}}}}</div>
  <div><strong>Scoring</strong>{{{{programme.scoringMethod}}}}</div>
  <div><strong>Races</strong>{{{{programme.raceCount}}}}</div>
  <div><strong>Fanciers</strong>{{{{totalLofts}}}}</div>
</div>
<table><thead><tr><th>#</th><th>Fancier / Loft</th><th>Races Entered</th><th>Pigeons Entered</th><th>Best Velocity</th><th>Avg Velocity</th><th>Total Score</th><th>Avg Score</th></tr></thead>
<tbody>{{{{#each results}}}}<tr>
  <td class='rank'>{{{{loftRank}}}}</td>
  <td><strong>{{{{fancierName}}}}</strong></td>
  <td>{{{{racesEntered}}}}</td>
  <td>{{{{pigeonsEntered}}}}</td>
  <td>{{{{bestSingleVelocityMperMin}}}} m/min</td>
  <td>{{{{averageVelocityMperMin}}}} m/min</td>
  <td class='score'>{{{{totalScore}}}}</td>
  <td>{{{{averageScore}}}}</td>
</tr>{{{{/each}}}}</tbody></table>
<div class='footer'>{{{{club.name}}}} &bull; Best Loft Programme {{{{programme.year}}}} &bull; Printed {{{{printDate}}}}</div>
</body></html>";

    private static string BuildBLSerifBordered() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:15mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Georgia','Times New Roman',serif;font-size:10.5pt;background:#FFFEF8;color:#111}
.border{border:3px double #C9A84C;padding:16px}
.title{text-align:center;border-bottom:2px solid #C9A84C;padding-bottom:14px;margin-bottom:14px}
.title h1{font-size:22pt;color:#1E3A5F;letter-spacing:1px;font-weight:800}
.title .prog{font-size:13pt;color:#C9A84C;font-style:italic;margin-top:4px}
.title .meta{font-size:9.5pt;color:#777;margin-top:6px;letter-spacing:1px;text-transform:uppercase}
.orn{text-align:center;font-size:18pt;color:#C9A84C;margin:6px 0}
thead th{background:#1E3A5F;color:#C9A84C;font-family:Georgia,serif;font-size:9pt;text-transform:uppercase;letter-spacing:.5px;border:1px solid #C9A84C;padding:7px 9px}
tbody tr:nth-child(even){background:#FBF7EC} td{border:1px solid #E8DFC8;font-size:10pt;padding:6px 9px}
.rank{font-size:14pt;font-weight:700;color:#C9A84C;text-align:center;width:48px}
.score{font-weight:700;color:#1E3A5F}
.footer{text-align:center;font-size:8.5pt;color:#999;margin-top:12px;font-style:italic}
</style></head><body><div class='border'>
<div class='orn'>&#10022; &#10022; &#10022;</div>
<div class='title'><h1>🏠 Best Loft Results</h1><div class='prog'>{{programme.name}}</div><div class='meta'>{{club.name}} &bull; Season {{programme.year}} &bull; Official Rankings</div></div>
<div class='orn'>&#8730; &mdash; &#8730;</div>
<table style='margin-top:10px'>
  <thead><tr><th>#</th><th>Fancier</th><th>Races</th><th>Pigeons</th><th>Best Vel.</th><th>Avg Vel.</th><th>Total Score</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank'>{{loftRank}}</td><td>{{fancierName}}</td><td>{{racesEntered}}</td><td>{{pigeonsEntered}}</td><td>{{bestSingleVelocityMperMin}}</td><td>{{averageVelocityMperMin}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &mdash; {{programme.name}} &mdash; Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildBLMinimal() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:20mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;color:#111}
.header{padding:20px 0 14px;border-bottom:2px solid #111}
.header h1{font-size:26pt;font-weight:900;letter-spacing:-1px}
.header .sub{font-size:11pt;color:#666;margin-top:3px}
.meta{display:flex;gap:32px;padding:10px 0;border-bottom:1px solid #DDD;margin-bottom:14px;font-size:8.5pt;color:#666}
.meta strong{display:block;color:#111;font-size:9pt}
thead th{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#666;font-weight:600;border-bottom:2px solid #111;padding:5px 6px;text-align:left}
tbody tr{border-bottom:1px solid #EEE} td{font-size:10pt;padding:7px 6px}
.rank{font-size:13pt;font-weight:900;text-align:center;width:44px}
tr:nth-child(1) .rank{color:#D4A017} tr:nth-child(2) .rank{color:#888} tr:nth-child(3) .rank{color:#CD7F32}
.score{font-weight:700}
.footer{font-size:8pt;color:#BBB;margin-top:16px}
</style></head><body>
<div class='header'><h1>Best Loft</h1><div class='sub'>{{programme.name}} &bull; {{club.name}}</div></div>
<div class='meta'>
  <div><strong>Season</strong>{{programme.year}}</div>
  <div><strong>Scoring</strong>{{programme.scoringMethod}}</div>
  <div><strong>Races</strong>{{programme.raceCount}}</div>
  <div><strong>Fanciers</strong>{{totalLofts}}</div>
</div>
<table>
  <thead><tr><th>#</th><th>Fancier</th><th>Races</th><th>Pigeons</th><th>Best m/min</th><th>Avg m/min</th><th>Total Score</th><th>Avg Score</th></tr></thead>
  <tbody>{{#each results}}<tr><td class='rank'>{{loftRank}}</td><td>{{fancierName}}</td><td>{{racesEntered}}</td><td>{{pigeonsEntered}}</td><td>{{bestSingleVelocityMperMin}}</td><td>{{averageVelocityMperMin}}</td><td class='score'>{{totalScore}}</td><td>{{averageScore}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &bull; Printed {{printDate}}</div>
</body></html>";

    private static string BuildBLBranded() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:12mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;color:#111}
.header{background:{{club.primaryColour}};color:#fff;padding:14px 20px;display:flex;justify-content:space-between;align-items:center}
.header h1{font-size:18pt;font-weight:800}.header .sub{font-size:9pt;opacity:.8}
.logo{width:56px;height:56px;object-fit:contain;background:rgba(255,255,255,.15);border-radius:4px;padding:4px}
.meta{display:flex;gap:20px;background:{{club.secondaryColour}}22;padding:8px 20px;border-bottom:2px solid {{club.secondaryColour}};font-size:9pt}
.meta strong{color:{{club.primaryColour}};display:block}
thead th{background:{{club.primaryColour}};color:{{club.secondaryColour}};font-size:9pt;padding:7px 9px;text-align:left}
tbody tr:nth-child(even){background:{{club.secondaryColour}}11} td{font-size:9.5pt;border-bottom:1px solid {{club.secondaryColour}}44;padding:6px 9px}
.rank{font-size:14pt;font-weight:900;color:{{club.primaryColour}};text-align:center;width:48px}
.score{font-weight:700;color:{{club.primaryColour}}}
.footer{font-size:8pt;color:#888;text-align:center;padding:7px}
</style></head><body>
<div class='header'><div><h1>🏠 Best Loft Results</h1><div class='sub'>{{programme.name}} &bull; Branded</div></div><img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'""></div>
<div class='meta'><div><strong>Programme</strong>{{programme.name}}</div><div><strong>Season</strong>{{programme.year}}</div><div><strong>Scoring</strong>{{programme.scoringMethod}}</div><div><strong>Races</strong>{{programme.raceCount}}</div><div><strong>Club</strong>{{club.name}}</div></div>
<table><thead><tr><th>#</th><th>Fancier</th><th>Races</th><th>Pigeons</th><th>Best m/min</th><th>Avg m/min</th><th>Total Score</th><th>Avg Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{loftRank}}</td><td>{{fancierName}}</td><td>{{racesEntered}}</td><td>{{pigeonsEntered}}</td><td>{{bestSingleVelocityMperMin}}</td><td>{{averageVelocityMperMin}}</td><td class='score'>{{totalScore}}</td><td>{{averageScore}}</td></tr>{{/each}}</tbody>
</table><div class='footer'>{{club.name}} Best Loft {{programme.year}} &bull; Printed {{printDate}}</div></body></html>";

    // ═══════════════════════════════════════════════════════════════════════════
    //  ACE PIGEON — 20 TEMPLATES
    // ═══════════════════════════════════════════════════════════════════════════

    public static readonly (string Id, string Name, string Style, string Html)[] AcePigeonTemplates = new[]
    {
        ("AP-01","Ace Pigeon Classic Navy",    "Classic",   BuildAP("Classic","#1E3A5F","#C9A84C","#F0F4F8",false)),
        ("AP-02","Ace Pigeon Gold Landscape",  "Elegant",   BuildAP("Gold","#1a1a1a","#C9A84C","#FAFAF0",true)),
        ("AP-03","Ace Pigeon Green",           "Classic",   BuildAP("Green","#2D6A4F","#F4A261","#F0F7F4",false)),
        ("AP-04","Ace Pigeon Crimson",         "Sporty",    BuildAP("Crimson","#C1121F","#2B2D42","#FFF5F5",false)),
        ("AP-05","Ace Pigeon Ivory Serif",     "Elegant",   BuildAPSerif()),
        ("AP-06","Ace Pigeon Dark Mode",       "Dark",      BuildAP("Dark","#0D1B2A","#1E90FF","#0D1B2A",true)),
        ("AP-07","Ace Pigeon Minimal",         "Minimal",   BuildAPMinimal()),
        ("AP-08","Ace Pigeon Purple",          "Elegant",   BuildAP("Purple","#4A0E8F","#D4A017","#F5F0FF",false)),
        ("AP-09","Ace Pigeon Teal",            "Modern",    BuildAP("Teal","#0077B6","#00B4D8","#F0F9FF",false)),
        ("AP-10","Ace Pigeon Branded",         "Branded",   BuildAPBranded()),
        ("AP-11","Ace Pigeon Ruby",            "Sporty",    BuildAP("Ruby","#9B1C1C","#FCA5A5","#FFF5F5",false)),
        ("AP-12","Ace Pigeon Slate",           "Corporate", BuildAP("Slate","#475569","#94A3B8","#F8FAFC",false)),
        ("AP-13","Ace Pigeon Ocean",           "Modern",    BuildAP("Ocean","#1E40AF","#93C5FD","#EFF6FF",false)),
        ("AP-14","Ace Pigeon Amber",           "Heritage",  BuildAP("Amber","#92400E","#FCD34D","#FFFBEB",false)),
        ("AP-15","Ace Pigeon Forest",          "Heritage",  BuildAP("Forest","#14532D","#86EFAC","#F0FDF4",false)),
        ("AP-16","Ace Pigeon Rose Gold",       "Elegant",   BuildAP("Rose Gold","#9F1239","#FCA5A1","#FFF1F2",false)),
        ("AP-17","Ace Pigeon Cobalt Wide",     "Modern",    BuildAP("Cobalt","#1E3A8A","#93C5FD","#EFF6FF",true)),
        ("AP-18","Ace Pigeon Heritage Brown",  "Heritage",  BuildAP("Heritage","#78350F","#D97706","#FEFCE8",false)),
        ("AP-19","Ace Pigeon Carbon Wide",     "Sporty",    BuildAP("Carbon","#1C1917","#A3E635","#1C1917",true)),
        ("AP-20","Ace Pigeon Lavender",        "Elegant",   BuildAP("Lavender","#4C1D95","#C4B5FD","#F5F3FF",false)),
    };

    private static string BuildAP(string label, string primary, string accent, string bg, bool landscape) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page {{{(landscape ? "size:A4 landscape;" : "")}margin:12mm}} @media print{{body{{-webkit-print-color-adjust:exact;print-color-adjust:exact}}}}
*{{box-sizing:border-box;margin:0;padding:0}} body{{font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;background:{bg};color:#111}}
.header{{background:{primary};color:#fff;padding:14px 20px}}
.header h1{{font-size:19pt;font-weight:800}}.header .sub{{font-size:9.5pt;opacity:.8}}
.prog-meta{{display:flex;gap:20px;background:{accent}22;padding:9px 20px;border-bottom:2px solid {accent};font-size:9pt}}
.prog-meta strong{{color:{primary};display:block}}
thead th{{background:{primary};color:{accent};font-size:9pt;padding:7px 9px;text-align:left}}
thead th:first-child{{width:48px;text-align:center}}
tbody tr:nth-child(even){{background:{accent}11}}
tbody tr:nth-child(1) td{{background:{accent}44;font-weight:700}}
tbody tr:nth-child(2) td{{background:{accent}22}}
tbody tr:nth-child(3) td{{background:{accent}11}}
td{{font-size:9.5pt;border-bottom:1px solid {accent}44;padding:6px 9px}}
.rank{{font-size:14pt;font-weight:900;color:{primary};text-align:center;width:48px}}
.ring{{font-family:monospace;font-size:9.5pt;font-weight:700}}
.score{{font-weight:700;color:{primary}}}
.sex-m{{color:#1E90FF;font-weight:600}} .sex-f{{color:#E63946;font-weight:600}}
.footer{{font-size:8pt;color:#888;text-align:center;padding:7px;margin-top:8px}}
</style></head><body>
<div class='header'><h1>🕊️ Ace Pigeon Results</h1><div class='sub'>{{{{programme.name}}}} &bull; {{{{club.name}}}} &bull; {label}</div></div>
<div class='prog-meta'>
  <div><strong>Programme</strong>{{{{programme.name}}}}</div>
  <div><strong>Season</strong>{{{{programme.year}}}}</div>
  <div><strong>Scoring</strong>{{{{programme.scoringMethod}}}}</div>
  <div><strong>Races</strong>{{{{programme.raceCount}}}}</div>
  <div><strong>Min Races</strong>{{{{programme.acePigeonMinRaces}}}}</div>
  <div><strong>Qualifying Birds</strong>{{{{totalPigeons}}}}</div>
</div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Name</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Races</th><th>Participation</th><th>Best m/min</th><th>Avg m/min</th><th>Score</th></tr></thead>
<tbody>{{{{#each results}}}}<tr>
  <td class='rank'>{{{{aceRank}}}}</td>
  <td class='ring'>{{{{ringNumber}}}}</td>
  <td>{{{{pigeonName}}}}</td>
  <td class='{{{{#if (eq pigeonSex "M")}}}}sex-m{{{{else}}}}sex-f{{{{/if}}}}'>{{{{pigeonSex}}}}</td>
  <td>{{{{pigeonYearOfBirth}}}}</td>
  <td>{{{{fancierName}}}}</td>
  <td>{{{{racesEntered}}}}/{{{{racesInProgramme}}}}</td>
  <td>{{{{participationRate}}}}%</td>
  <td>{{{{bestVelocityMperMin}}}}</td>
  <td>{{{{averageVelocityMperMin}}}}</td>
  <td class='score'>{{{{totalScore}}}}</td>
</tr>{{{{/each}}}}</tbody></table>
<div class='footer'>{{{{club.name}}}} &bull; Ace Pigeon {{{{programme.year}}}} &bull; Printed {{{{printDate}}}}</div>
</body></html>";

    private static string BuildAPSerif() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:15mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:Georgia,'Times New Roman',serif;font-size:10.5pt;background:#FFFEF8;color:#111}
.border{border:3px double #C9A84C;padding:16px}
.title{text-align:center;border-bottom:2px solid #C9A84C;padding-bottom:14px;margin-bottom:14px}
.title h1{font-size:22pt;color:#1E3A5F;font-weight:800;letter-spacing:1px}
.title .prog{font-size:13pt;color:#C9A84C;font-style:italic;margin-top:4px}
.title .meta{font-size:9.5pt;color:#777;margin-top:6px;text-transform:uppercase;letter-spacing:1px}
.orn{text-align:center;font-size:18pt;color:#C9A84C;margin:4px 0}
thead th{background:#1E3A5F;color:#C9A84C;font-family:Georgia,serif;font-size:9pt;text-transform:uppercase;letter-spacing:.5px;border:1px solid #C9A84C}
tbody tr:nth-child(even){background:#FBF7EC} td{border:1px solid #E8DFC8;font-size:10pt;padding:6px 8px}
.rank{font-size:14pt;font-weight:700;color:#C9A84C;text-align:center;width:48px}
.ring{font-family:monospace;font-weight:700}
.score{font-weight:700;color:#1E3A5F}
.footer{text-align:center;font-size:8.5pt;color:#999;margin-top:12px;font-style:italic}
</style></head><body><div class='border'>
<div class='orn'>&#10022; &#10022; &#10022;</div>
<div class='title'><h1>🕊️ Ace Pigeon Results</h1><div class='prog'>{{programme.name}}</div><div class='meta'>{{club.name}} &bull; Season {{programme.year}}</div></div>
<div class='orn'>&#8730; &mdash; &#8730;</div>
<table style='margin-top:10px'>
<thead><tr><th>#</th><th>Ring</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Races</th><th>Best m/min</th><th>Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{aceRank}}</td><td class='ring'>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYearOfBirth}}</td><td>{{fancierName}}</td><td>{{racesEntered}}/{{racesInProgramme}}</td><td>{{bestVelocityMperMin}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &mdash; Ace Pigeon {{programme.year}} &mdash; Printed {{printDate}}</div>
</div></body></html>";

    private static string BuildAPMinimal() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:20mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;color:#111}
.header{padding:20px 0 14px;border-bottom:2px solid #111}
.header h1{font-size:26pt;font-weight:900;letter-spacing:-1px}
.header .sub{font-size:11pt;color:#666;margin-top:3px}
.meta{display:flex;gap:32px;padding:10px 0;border-bottom:1px solid #DDD;margin-bottom:14px;font-size:8.5pt;color:#666}
.meta strong{display:block;color:#111;font-size:9pt}
thead th{font-size:8pt;text-transform:uppercase;letter-spacing:1px;color:#666;font-weight:600;border-bottom:2px solid #111;padding:5px 6px;text-align:left}
tbody tr{border-bottom:1px solid #EEE} td{font-size:9.5pt;padding:7px 6px}
.rank{font-size:13pt;font-weight:900;text-align:center;width:44px}
tr:nth-child(1) .rank{color:#D4A017} tr:nth-child(2) .rank{color:#888} tr:nth-child(3) .rank{color:#CD7F32}
.ring{font-family:monospace;font-weight:700}.score{font-weight:700}
.footer{font-size:8pt;color:#BBB;margin-top:16px}
</style></head><body>
<div class='header'><h1>Ace Pigeon</h1><div class='sub'>{{programme.name}} &bull; {{club.name}}</div></div>
<div class='meta'>
  <div><strong>Season</strong>{{programme.year}}</div>
  <div><strong>Scoring</strong>{{programme.scoringMethod}}</div>
  <div><strong>Races</strong>{{programme.raceCount}}</div>
  <div><strong>Min Races</strong>{{programme.acePigeonMinRaces}}</div>
</div>
<table>
<thead><tr><th>#</th><th>Ring</th><th>Pigeon</th><th>Sex</th><th>Yr</th><th>Fancier</th><th>Races</th><th>Part%</th><th>Best m/min</th><th>Avg m/min</th><th>Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{aceRank}}</td><td class='ring'>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYearOfBirth}}</td><td>{{fancierName}}</td><td>{{racesEntered}}/{{racesInProgramme}}</td><td>{{participationRate}}%</td><td>{{bestVelocityMperMin}}</td><td>{{averageVelocityMperMin}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table>
<div class='footer'>{{club.name}} &bull; Printed {{printDate}}</div>
</body></html>";

    private static string BuildAPBranded() => @"<!DOCTYPE html><html><head><meta charset='utf-8'><style>
@page{margin:12mm} @media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}}
*{box-sizing:border-box;margin:0;padding:0} body{font-family:'Segoe UI',Arial,sans-serif;font-size:10pt;color:#111}
.header{background:{{club.primaryColour}};color:#fff;padding:14px 20px;display:flex;justify-content:space-between;align-items:center}
.header h1{font-size:18pt;font-weight:800}.header .sub{font-size:9pt;opacity:.8}
.logo{width:56px;height:56px;object-fit:contain;background:rgba(255,255,255,.15);border-radius:4px;padding:4px}
.meta{display:flex;gap:20px;background:{{club.secondaryColour}}22;padding:8px 20px;border-bottom:2px solid {{club.secondaryColour}};font-size:9pt}
.meta strong{color:{{club.primaryColour}};display:block}
thead th{background:{{club.primaryColour}};color:{{club.secondaryColour}};font-size:9pt;padding:7px 9px;text-align:left}
tbody tr:nth-child(even){background:{{club.secondaryColour}}11} td{font-size:9.5pt;border-bottom:1px solid {{club.secondaryColour}}44;padding:6px 9px}
.rank{font-size:14pt;font-weight:900;color:{{club.primaryColour}};text-align:center;width:48px}
.ring{font-family:monospace;font-weight:700}
.score{font-weight:700;color:{{club.primaryColour}}}
.footer{font-size:8pt;color:#888;text-align:center;padding:7px}
</style></head><body>
<div class='header'><div><h1>🕊️ Ace Pigeon Results</h1><div class='sub'>{{programme.name}} &bull; Branded</div></div><img class='logo' src='{{club.logoUrl}}' onerror=""this.style.display='none'""></div>
<div class='meta'><div><strong>Programme</strong>{{programme.name}}</div><div><strong>Season</strong>{{programme.year}}</div><div><strong>Scoring</strong>{{programme.scoringMethod}}</div><div><strong>Races</strong>{{programme.raceCount}}</div><div><strong>Club</strong>{{club.name}}</div></div>
<table><thead><tr><th>#</th><th>Ring Number</th><th>Pigeon</th><th>Sex</th><th>Year</th><th>Fancier</th><th>Races</th><th>Part%</th><th>Best m/min</th><th>Score</th></tr></thead>
<tbody>{{#each results}}<tr><td class='rank'>{{aceRank}}</td><td class='ring'>{{ringNumber}}</td><td>{{pigeonName}}</td><td>{{pigeonSex}}</td><td>{{pigeonYearOfBirth}}</td><td>{{fancierName}}</td><td>{{racesEntered}}/{{racesInProgramme}}</td><td>{{participationRate}}%</td><td>{{bestVelocityMperMin}}</td><td class='score'>{{totalScore}}</td></tr>{{/each}}</tbody>
</table><div class='footer'>{{club.name}} Ace Pigeon {{programme.year}} &bull; Printed {{printDate}}</div></body></html>";
}
