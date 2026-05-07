namespace PigeonRacing.Infrastructure.Templates;

/// <summary>
/// Provides translated column header labels for each supported locale.
/// These are injected as {{labels.*}} variables when rendering templates,
/// so the same HTML template works in all 6 languages.
/// </summary>
public static class TemplateLocales
{
    public static readonly Dictionary<string, TemplateLabels> All = new()
    {
        ["en"] = new TemplateLabels
        {
            Rank            = "#",
            RingNumber      = "Ring Number",
            Pigeon          = "Pigeon",
            PigeonName      = "Pigeon Name",
            Sex             = "Sex",
            Cock            = "Cock",
            Hen             = "Hen",
            YearOfBirth     = "Year",
            Fancier         = "Fancier",
            Arrival         = "Arrival",
            ArrivalTime     = "Arrival Time",
            DistanceKm      = "Distance (km)",
            Dist            = "Dist",
            VelocityMperMin = "Velocity (m/min)",
            VelocityKmH     = "Velocity (km/h)",
            Category        = "Category",
            Cat             = "Cat",
            Season          = "Season",
            OfficialResults = "Official Race Results",
            PrintedOn       = "Printed",
            LoftRank        = "Loft Rank",
            AceRank         = "Ace Rank",
            SuperAceRank    = "Super Ace Rank",
            RacesEntered    = "Races",
            PigeonsEntered  = "Pigeons",
            BestVelocity    = "Best m/min",
            AvgVelocity     = "Avg m/min",
            TotalScore      = "Total Score",
            AvgScore        = "Avg Score",
            Participation   = "Part%",
            BestRank        = "Best Rank",
            Scoring         = "Scoring",
            Qualification   = "Qualification",
            // Certificate
            CertificateOf   = "Certificate of Achievement",
            AwardedTo       = "Awarded to",
            Achieved        = "has achieved",
            PlaceIn         = "place in",
            ForAchieving    = "for achieving",
            ClubManager     = "Club Manager",
            Secretary       = "Secretary",
            BestLoftTitle   = "Best Loft Results",
            AcePigeonTitle  = "Ace Pigeon Results",
            SuperAceTitle   = "Super Ace Pigeon",
            Programme       = "Programme",
            Qualifiers      = "Qualifiers",
            Fanciers        = "Fanciers"
        },

        ["fr"] = new TemplateLabels
        {
            Rank            = "#",
            RingNumber      = "Numéro de bague",
            Pigeon          = "Pigeon",
            PigeonName      = "Nom du pigeon",
            Sex             = "Sexe",
            Cock            = "Mâle",
            Hen             = "Femelle",
            YearOfBirth     = "Année",
            Fancier         = "Colombophile",
            Arrival         = "Arrivée",
            ArrivalTime     = "Heure d'arrivée",
            DistanceKm      = "Distance (km)",
            Dist            = "Dist.",
            VelocityMperMin = "Vitesse (m/min)",
            VelocityKmH     = "Vitesse (km/h)",
            Category        = "Catégorie",
            Cat             = "Cat.",
            Season          = "Saison",
            OfficialResults = "Résultats officiels de course",
            PrintedOn       = "Imprimé le",
            LoftRank        = "Rang colombier",
            AceRank         = "Rang pigeon d'as",
            SuperAceRank    = "Rang super pigeon d'as",
            RacesEntered    = "Courses",
            PigeonsEntered  = "Pigeons",
            BestVelocity    = "Meilleure vitesse",
            AvgVelocity     = "Vitesse moy.",
            TotalScore      = "Score total",
            AvgScore        = "Score moy.",
            Participation   = "Participation",
            BestRank        = "Meilleur rang",
            Scoring         = "Calcul",
            Qualification   = "Qualification",
            CertificateOf   = "Certificat de mérite",
            AwardedTo       = "Décerné à",
            Achieved        = "a obtenu la",
            PlaceIn         = "place dans",
            ForAchieving    = "pour avoir obtenu la",
            ClubManager     = "Directeur de club",
            Secretary       = "Secrétaire",
            BestLoftTitle   = "Meilleur colombier",
            AcePigeonTitle  = "Pigeon d'as",
            SuperAceTitle   = "Super pigeon d'as",
            Programme       = "Programme",
            Qualifiers      = "Qualifiés",
            Fanciers        = "Colombophiles"
        },

        ["nl-BE"] = new TemplateLabels
        {
            Rank            = "#",
            RingNumber      = "Ringnummer",
            Pigeon          = "Duif",
            PigeonName      = "Naam duif",
            Sex             = "Geslacht",
            Cock            = "Doffer",
            Hen             = "Duivin",
            YearOfBirth     = "Jaar",
            Fancier         = "Liefhebber",
            Arrival         = "Aankomst",
            ArrivalTime     = "Aankomsttijd",
            DistanceKm      = "Afstand (km)",
            Dist            = "Afst.",
            VelocityMperMin = "Snelheid (m/min)",
            VelocityKmH     = "Snelheid (km/u)",
            Category        = "Categorie",
            Cat             = "Cat.",
            Season          = "Seizoen",
            OfficialResults = "Officiële vluchtuitslagen",
            PrintedOn       = "Afgedrukt op",
            LoftRank        = "Kwekerij rang",
            AceRank         = "Asduif rang",
            SuperAceRank    = "Super asduif rang",
            RacesEntered    = "Vluchten",
            PigeonsEntered  = "Duiven",
            BestVelocity    = "Beste snelheid",
            AvgVelocity     = "Gem. snelheid",
            TotalScore      = "Totale score",
            AvgScore        = "Gemid. score",
            Participation   = "Deelname%",
            BestRank        = "Beste rang",
            Scoring         = "Berekening",
            Qualification   = "Kwalificatie",
            CertificateOf   = "Getuigschrift van verdienste",
            AwardedTo       = "Uitgereikt aan",
            Achieved        = "de",
            PlaceIn         = "plaats behaald in",
            ForAchieving    = "voor het behalen van de",
            ClubManager     = "Clubbeheerder",
            Secretary       = "Secretaris",
            BestLoftTitle   = "Beste kwekerij",
            AcePigeonTitle  = "Asduif",
            SuperAceTitle   = "Super asduif",
            Programme       = "Programma",
            Qualifiers      = "Gekwalificeerden",
            Fanciers        = "Liefhebbers"
        },

        ["ar"] = new TemplateLabels
        {
            Rank            = "#",
            RingNumber      = "رقم الحلقة",
            Pigeon          = "حمامة",
            PigeonName      = "اسم الحمامة",
            Sex             = "الجنس",
            Cock            = "ذكر",
            Hen             = "أنثى",
            YearOfBirth     = "السنة",
            Fancier         = "مربي الحمام",
            Arrival         = "الوصول",
            ArrivalTime     = "وقت الوصول",
            DistanceKm      = "المسافة (كم)",
            Dist            = "المسافة",
            VelocityMperMin = "السرعة (م/د)",
            VelocityKmH     = "السرعة (كم/س)",
            Category        = "الفئة",
            Cat             = "فئة",
            Season          = "الموسم",
            OfficialResults = "النتائج الرسمية للسباق",
            PrintedOn       = "طُبع في",
            LoftRank        = "ترتيب البرج",
            AceRank         = "ترتيب الآس",
            SuperAceRank    = "ترتيب سوبر الآس",
            RacesEntered    = "السباقات",
            PigeonsEntered  = "الحمامات",
            BestVelocity    = "أفضل سرعة",
            AvgVelocity     = "متوسط السرعة",
            TotalScore      = "المجموع",
            AvgScore        = "المتوسط",
            Participation   = "المشاركة%",
            BestRank        = "أفضل مرتبة",
            Scoring         = "التسجيل",
            Qualification   = "التأهل",
            CertificateOf   = "شهادة تقدير",
            AwardedTo       = "تُمنح لـ",
            Achieved        = "بتحقيق المركز",
            PlaceIn         = "في",
            ForAchieving    = "لتحقيقه المركز",
            ClubManager     = "مدير النادي",
            Secretary       = "الأمين",
            BestLoftTitle   = "أفضل برج",
            AcePigeonTitle  = "حمامة الآس",
            SuperAceTitle   = "سوبر حمامة الآس",
            Programme       = "البرنامج",
            Qualifiers      = "المتأهلون",
            Fanciers        = "المربون"
        },

        ["zh"] = new TemplateLabels
        {
            Rank            = "名次",
            RingNumber      = "脚环号",
            Pigeon          = "赛鸽",
            PigeonName      = "鸽名",
            Sex             = "性别",
            Cock            = "雄",
            Hen             = "雌",
            YearOfBirth     = "年份",
            Fancier         = "鸽主",
            Arrival         = "归巢",
            ArrivalTime     = "归巢时间",
            DistanceKm      = "距离（公里）",
            Dist            = "距离",
            VelocityMperMin = "速度（米/分）",
            VelocityKmH     = "速度（公里/时）",
            Category        = "组别",
            Cat             = "组",
            Season          = "赛季",
            OfficialResults = "官方赛事成绩",
            PrintedOn       = "打印于",
            LoftRank        = "鸽舍名次",
            AceRank         = "明星鸽名次",
            SuperAceRank    = "超级明星鸽名次",
            RacesEntered    = "场次",
            PigeonsEntered  = "鸽数",
            BestVelocity    = "最高速度",
            AvgVelocity     = "平均速度",
            TotalScore      = "总分",
            AvgScore        = "平均分",
            Participation   = "参赛率",
            BestRank        = "最佳名次",
            Scoring         = "计分",
            Qualification   = "资格",
            CertificateOf   = "荣誉证书",
            AwardedTo       = "授予",
            Achieved        = "荣获",
            PlaceIn         = "名 在",
            ForAchieving    = "荣获",
            ClubManager     = "俱乐部管理员",
            Secretary       = "秘书",
            BestLoftTitle   = "最佳鸽舍成绩",
            AcePigeonTitle  = "明星鸽成绩",
            SuperAceTitle   = "超级明星鸽",
            Programme       = "赛程",
            Qualifiers      = "入围者",
            Fanciers        = "鸽主"
        },

        ["es"] = new TemplateLabels
        {
            Rank            = "#",
            RingNumber      = "Número de anilla",
            Pigeon          = "Paloma",
            PigeonName      = "Nombre de la paloma",
            Sex             = "Sexo",
            Cock            = "Macho",
            Hen             = "Hembra",
            YearOfBirth     = "Año",
            Fancier         = "Colombicultor",
            Arrival         = "Llegada",
            ArrivalTime     = "Hora de llegada",
            DistanceKm      = "Distancia (km)",
            Dist            = "Dist.",
            VelocityMperMin = "Velocidad (m/min)",
            VelocityKmH     = "Velocidad (km/h)",
            Category        = "Categoría",
            Cat             = "Cat.",
            Season          = "Temporada",
            OfficialResults = "Resultados oficiales de carrera",
            PrintedOn       = "Impreso el",
            LoftRank        = "Puesto del palomar",
            AceRank         = "Puesto paloma as",
            SuperAceRank    = "Puesto súper paloma as",
            RacesEntered    = "Carreras",
            PigeonsEntered  = "Palomas",
            BestVelocity    = "Mejor veloc.",
            AvgVelocity     = "Veloc. prom.",
            TotalScore      = "Puntuación total",
            AvgScore        = "Puntuación prom.",
            Participation   = "Participación%",
            BestRank        = "Mejor puesto",
            Scoring         = "Puntuación",
            Qualification   = "Clasificación",
            CertificateOf   = "Certificado de mérito",
            AwardedTo       = "Otorgado a",
            Achieved        = "ha obtenido el",
            PlaceIn         = "puesto en",
            ForAchieving    = "por obtener el",
            ClubManager     = "Director del club",
            Secretary       = "Secretario/a",
            BestLoftTitle   = "Mejor palomar",
            AcePigeonTitle  = "Paloma as",
            SuperAceTitle   = "Súper paloma as",
            Programme       = "Programa",
            Qualifiers      = "Clasificados",
            Fanciers        = "Colombicultores"
        }
    };

    /// <summary>Gets labels for a locale code, falling back to English.</summary>
    public static TemplateLabels Get(string? locale)
    {
        if (locale != null && All.TryGetValue(locale, out var labels))
            return labels;
        return All["en"];
    }
}

public class TemplateLabels
{
    public string Rank            { get; init; } = "#";
    public string RingNumber      { get; init; } = "Ring Number";
    public string Pigeon          { get; init; } = "Pigeon";
    public string PigeonName      { get; init; } = "Pigeon Name";
    public string Sex             { get; init; } = "Sex";
    public string Cock            { get; init; } = "Cock";
    public string Hen             { get; init; } = "Hen";
    public string YearOfBirth     { get; init; } = "Year";
    public string Fancier         { get; init; } = "Fancier";
    public string Arrival         { get; init; } = "Arrival";
    public string ArrivalTime     { get; init; } = "Arrival Time";
    public string DistanceKm      { get; init; } = "Distance (km)";
    public string Dist            { get; init; } = "Dist";
    public string VelocityMperMin { get; init; } = "Velocity (m/min)";
    public string VelocityKmH     { get; init; } = "Velocity (km/h)";
    public string Category        { get; init; } = "Category";
    public string Cat             { get; init; } = "Cat";
    public string Season          { get; init; } = "Season";
    public string OfficialResults { get; init; } = "Official Race Results";
    public string PrintedOn       { get; init; } = "Printed";
    public string LoftRank        { get; init; } = "Loft Rank";
    public string AceRank         { get; init; } = "Ace Rank";
    public string SuperAceRank    { get; init; } = "Super Ace Rank";
    public string RacesEntered    { get; init; } = "Races";
    public string PigeonsEntered  { get; init; } = "Pigeons";
    public string BestVelocity    { get; init; } = "Best m/min";
    public string AvgVelocity     { get; init; } = "Avg m/min";
    public string TotalScore      { get; init; } = "Total Score";
    public string AvgScore        { get; init; } = "Avg Score";
    public string Participation   { get; init; } = "Part%";
    public string BestRank        { get; init; } = "Best Rank";
    public string Scoring         { get; init; } = "Scoring";
    public string Qualification   { get; init; } = "Qualification";
    public string CertificateOf   { get; init; } = "Certificate of Achievement";
    public string AwardedTo       { get; init; } = "Awarded to";
    public string Achieved        { get; init; } = "has achieved";
    public string PlaceIn         { get; init; } = "place in";
    public string ForAchieving    { get; init; } = "for achieving";
    public string ClubManager     { get; init; } = "Club Manager";
    public string Secretary       { get; init; } = "Secretary";
    public string BestLoftTitle   { get; init; } = "Best Loft Results";
    public string AcePigeonTitle  { get; init; } = "Ace Pigeon Results";
    public string SuperAceTitle   { get; init; } = "Super Ace Pigeon";
    public string Programme       { get; init; } = "Programme";
    public string Qualifiers      { get; init; } = "Qualifiers";
    public string Fanciers        { get; init; } = "Fanciers";
}
