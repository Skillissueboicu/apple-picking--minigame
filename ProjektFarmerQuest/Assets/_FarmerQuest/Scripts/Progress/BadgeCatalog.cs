using System;
using System.Collections.Generic;

namespace FarmerQuest.Progress
{
    public sealed class BadgeInfo
    {
        public string Key;
        public string Title;
        public string Summary;
        public string BronzeHow;
        public string SilverHow;
        public string GoldHow;
        public string EarnedBronze;
        public string EarnedSilver;
        public string EarnedGold;
    }

    /// <summary>Danske navne + krav/beskeder for alle 33 badge-slots.</summary>
    public static class BadgeCatalog
    {
        private static readonly Dictionary<string, BadgeInfo> ByKey =
            new(StringComparer.OrdinalIgnoreCase);

        static BadgeCatalog()
        {
            void Add(string key, string title, string summary,
                string bronze, string silver, string gold,
                string earnedB, string earnedS, string earnedG)
            {
                ByKey[key] = new BadgeInfo
                {
                    Key = key,
                    Title = title,
                    Summary = summary,
                    BronzeHow = bronze,
                    SilverHow = silver,
                    GoldHow = gold,
                    EarnedBronze = earnedB,
                    EarnedSilver = earnedS,
                    EarnedGold = earnedG,
                };
            }

            // Indre
            Add("salgsansvarlig", "Salgsansvarlig",
                "Du styrer gårdens penge via laden — salg og drift.",
                "Sælg mindst én vare fra laden (penge ind).",
                "Sælg fra to forskellige produkttyper (fx korn + mælk).",
                "Hold positiv kasse efter driftomkostninger i et forløb hvor du både har solgt og haft gården i gang. Minispil: Gårdens penge.",
                "Bronze: Du kan sælge fra laden.",
                "Sølv: Du sælger flere slags produkter.",
                "Guld: Du holder styr på gårdens penge som en rigtig driftsansvarlig.");

            Add("fejlfinder", "Fejlfinder",
                "Du forstår hvad der sker når kredsløb og forbindelser går galt — og retter dem.",
                "Lav en “forkert” forbindelse med synlig konsekvens (fx gylle→hovedhus) — og fjern/ret den bagefter.",
                "Find og ret en produktions-stop (tom buffer / forkert flow) så bygningen kører igen.",
                "Minispil: Hvad skete der? (høj score) og én rettet fejl-forbindelse på gården.",
                "Bronze: Du har set og rettet en fejl-forbindelse.",
                "Sølv: Du kan finde ud af hvorfor noget er stoppet.",
                "Guld: Du er skarp til konsekvenser og fejlfinding.");

            Add("co2_jaeger", "CO₂-jæger",
                "Du sænker gårdens samlede CO₂-udledning uden at lukke produktionen.",
                "Få gårdens CO₂-indikator ned (synlig forbedring).",
                "Hold CO₂ lavere mens der stadig leveres noget til laden.",
                "Minispil: Klima på gården (høj score) og lav CO₂ samtidig med aktiv produktion.",
                "Bronze: Du har sænket gårdens CO₂.",
                "Sølv: Du holder CO₂ nede mens gården stadig producerer.",
                "Guld: Du jager CO₂ som en klimamester.");

            Add("stromspare", "Strømspare",
                "Du bruger mindre energi på det samme arbejde på gården.",
                "Gennemfør en leverance med lavt energiforbrug (effektiv rute / færre unødige ting tændt).",
                "To leverancer (mark-side og stald-side) med lavt forbrug.",
                "Minispil: Spar strøm (høj score) og produktion uden unødige energi-slugere.",
                "Bronze: Du kan spare strøm på en leverance.",
                "Sølv: Du sparer strøm på flere linjer.",
                "Guld: Du er en ægte strømspare på gården.");

            Add("landskabsplejer", "Landskabsplejer",
                "Du passer på hele gårdens landskab — ikke kun dyr eller ét kant-element.",
                "Aktivér/hold et landskabstræk (fx læhegn, vådområde eller blomsterbræmme).",
                "Landskab aktiv samtidig med at mindst én produktionslinje leverer.",
                "Minispil: Agerlandet (høj score) og landskab + produktion uden at naturen kollapser.",
                "Bronze: Du har sat gang i landskabspleje.",
                "Sølv: Landskab og drift kan leve side om side.",
                "Guld: Du er landskabets bedste ven på gården.");

            // Landbrugsproduktion
            Add("planteekspert", "Planteekspert",
                "Du dyrker planter på marken (og drivhus når det findes).",
                "Mark har fået frø/næring og produceret korn.",
                "Korn er nået laden.",
                "Både mark-høst og drivhus-høst (eller to forskellige afgrøder) + minispil: Mark.",
                "Bronze: Du kan få planter til at vokse.",
                "Sølv: Din høst når laden.",
                "Guld: Du er en dygtig planteekspert.");

            Add("saesonmester", "Sæsonmester",
                "Du dyrker det der passer til årstiden.",
                "Dyrk en afgrøde der matcher den aktive sæson.",
                "Høst med god kvalitet på en sæson-rigtig afgrøde.",
                "Minispil: Hvad gror hvornår? (høj score) og sæson-rigtig høst til laden.",
                "Bronze: Du vælger sæson-rigtigt.",
                "Sølv: Du høster godt i sæsonen.",
                "Guld: Du er sæsonmester.");

            Add("spildkriger", "Spildkriger",
                "Du undgår spild og får rester tilbage i kredsløbet.",
                "Noget der ellers var spild ender i kompost.",
                "Laden får en leverance uden unødig overflod (produktion → brug/salg).",
                "Minispil: Spild i kæden (høj score) og genbrug + laden-leverance i samme forløb.",
                "Bronze: Du sender spild til kompost.",
                "Sølv: Du holder produktionen uden spild-kaos.",
                "Guld: Du er spildkriger.");

            Add("jordven", "Jordven",
                "Du giver jorden næring via kompost.",
                "Kompost→mark lykkes.",
                "Kompost til mark eller drivhus, og høst efter.",
                "Kompost brugt til to plante-steder (eller to cyklusser) + minispil: Kompost.",
                "Bronze: Du bruger kompost på marken.",
                "Sølv: Kompost giver høst.",
                "Guld: Du er jordens ven.");

            Add("drivhusgartner", "Drivhusgartner",
                "Du dyrker under kontrolleret klima i drivhuset.",
                "Drivhus producerer (eller mark med klima-bonus indtil drivhus findes).",
                "Output fra drivhus videre til mark eller laden.",
                "Drivhus i balance + minispil: Drivhus-balance.",
                "Bronze: Du har gang i drivhus-produktion.",
                "Sølv: Drivhusets output bruges videre.",
                "Guld: Du mestrer drivhuset.");

            Add("hostklar", "Høstklar",
                "Du høster når afgrøden er klar — ikke for tidligt eller for sent.",
                "Høst ved høst-klart vækststadie (visuelt).",
                "Den høst når laden.",
                "Minispil: Hvornår høster man? (høj score) og stadie-rigtig høst.",
                "Bronze: Du kan spotte høstklart stadie.",
                "Sølv: Din rigtige høst når laden.",
                "Guld: Du er høstklar-mester.");

            Add("saedskiftespire", "Sædskiftespire",
                "Du skifter afgrøde så jorden og planterne har det bedre.",
                "Høst af afgrøde A.",
                "Skift til afgrøde B på samme mark efter A.",
                "Høst af B efter skift + minispil: Sædskifte.",
                "Bronze: Du har høstet første afgrøde.",
                "Sølv: Du har skiftet afgrøde.",
                "Guld: Du forstår sædskifte.");

            // Teknologi
            Add("holdkaptajn", "Holdkaptajn",
                "Du samarbejder med andre om at få gården til at virke.",
                "I co-op: to spillere bidrager til samme leverance (forskellige led).",
                "Hver sin linje kørende (mark vs stald).",
                "Fælles gylle→kompost→mark + minispil: Del opgaverne.",
                "Bronze: I har samarbejdet om en leverance.",
                "Sølv: Holdet holder flere linjer i gang.",
                "Guld: Du er holdkaptajn.");

            Add("rormester", "Rørmester",
                "Du bygger kredsløb med rør der faktisk flytter ting.",
                "Ét rør fra output til input der flytter noget.",
                "Tre forskellige rør-ruter i brug (fx mark→laden, stald→laden, kompost→mark).",
                "Minispil: Tegn kredsløb (høj score) og alle tre ruter har leveret.",
                "Bronze: Dit første rør virker.",
                "Sølv: Du har flere ruter i gang.",
                "Guld: Du er rørmester.");

            Add("opfinderen", "Opfinderen",
                "Du køber/placerer nye ting og får dem til at arbejde på gården.",
                "Køb/placer en ny ting der ikke var der fra start.",
                "Den nye ting indgår i en produktiv cyklus.",
                "Minispil: Nye ting + første fulde cyklus med den nye ting.",
                "Bronze: Du har tilføjet noget nyt.",
                "Sølv: Det nye er blevet produktivt.",
                "Guld: Du er opfinderen.");

            Add("logistikhelt", "Logistikhelt",
                "Du kan flytte ting både manuelt og via rør.",
                "Manuel flytning mark→laden lykkes.",
                "Samme strækning også via rør.",
                "Minispil: Bær eller rør? (høj score) og begge metoder har leveret.",
                "Bronze: Du kan bære lasten.",
                "Sølv: Du kan både bære og bruge rør.",
                "Guld: Du er logistikhelt.");

            Add("vedligeholder", "Vedligeholder",
                "Du får ting i gang igen når produktionen er stoppet.",
                "En bygning der var tom/stoppet får input igen.",
                "Output genoptages efter stop.",
                "Minispil: Find fejlen (høj score) og genopret + genoptag i samme forløb.",
                "Bronze: Du har genstartet en bygning.",
                "Sølv: Produktionen kører igen.",
                "Guld: Du er vedligeholderen.");

            Add("planlaegger", "Planlægger",
                "Du starter kun produktion der matcher det du har — og køber det der mangler.",
                "Start produktion der passer til lageret (ikke uden input).",
                "Køb det manglende input og få produktion i gang.",
                "Minispil: Kapacitet (høj score) og shop-køb matcher en konkret mangel.",
                "Bronze: Du planlægger efter lageret.",
                "Sølv: Du køber det der mangler.",
                "Guld: Du er planlægger.");

            Add("dokumentarist", "Dokumentarist",
                "Du lærer ved bygninger og bruger det på gården.",
                "Bestå ét lærings-minispil knyttet til en bygning.",
                "Bestå lærings-minispil ved to forskellige bygninger.",
                "Efter lærings-minispil: gennemfør den matching gård-handling.",
                "Bronze: Du har lært noget ved en bygning.",
                "Sølv: Du har lært ved flere bygninger.",
                "Guld: Du bruger det du har lært.");

            // Klimahandling
            Add("jorddetektiv", "Jorddetektiv",
                "Du forstår jord og næring — især via kompost.",
                "Kompost→mark.",
                "Bedre høst-kvalitet efter kompost-cyklus.",
                "Minispil: Jordtyper og Jordstruktur (begge over tærskel) + kompost→mark-høst.",
                "Bronze: Du har brugt kompost på marken.",
                "Sølv: Jorden giver bedre høst.",
                "Guld: Du er jorddetektiv.");

            Add("vandvogter", "Vandvogter",
                "Du passer vandbalancen på marken.",
                "Lav en vand/balance-handling på marken.",
                "Høst i en cyklus hvor vand-balancen var OK.",
                "Minispil: For lidt/for meget (høj score) og vand-OK høst.",
                "Bronze: Du har passet vandet.",
                "Sølv: Du høster med god vandbalance.",
                "Guld: Du er vandvogter.");

            Add("genbrugshelt", "Genbrugshelt",
                "Du lukker nærings-ringen: gylle → kompost → planter.",
                "Gylle→kompost.",
                "Kompost→plante + output.",
                "Hele loopet i ét forløb + minispil: Luk ringen.",
                "Bronze: Gylle bliver til kompost.",
                "Sølv: Kompost bliver til planter.",
                "Guld: Du har lukket ringen.");

            Add("klimatilpasser", "Klimatilpasser",
                "Du holder gården kørende under dansk vejr.",
                "En handling under aktivt vejr-event.",
                "Leverance til laden under vejr-event.",
                "Minispil: Tilpas dig (høj score) og leverance under vejr.",
                "Bronze: Du handler under vejr.",
                "Sølv: Du leverer under vejr.",
                "Guld: Du er klimatilpasser.");

            Add("kulstoftaenker", "Kulstoftænker",
                "Du bygger kulstof i jorden med dække og kompost.",
                "Efterafgrøde/jorddække aktiv på mark.",
                "Kompost→mark mens dække/efterafgrøde er eller var i brug.",
                "Minispil: Kulstof i jord (høj score) og dække + kompost-cyklus.",
                "Bronze: Du har sat dække/efterafgrøde.",
                "Sølv: Dække og kompost arbejder sammen.",
                "Guld: Du er kulstoftænker.");

            Add("ren_routing", "Ren routing",
                "Du sender næring den rigtige vej — via kompost, ikke gylle direkte på mark.",
                "Næring til planter via kompost-vej.",
                "Kompost→mark leverance gennemført.",
                "Minispil: Hvor må gylle? (høj score). Gylle→mark sænker kun dette badges fyld midlertidigt.",
                "Bronze: Du vælger kompost-vejen.",
                "Sølv: Kompost når marken.",
                "Guld: Du mestrer ren routing.");

            // Energi
            Add("energiingenioer", "Energiingeniør",
                "Du sætter energi op og holder produktion kørende med den.",
                "Sæt energi-modul op / tænd det.",
                "Produktion kører med modulet aktivt.",
                "Minispil: Energiingeniør + stabil drift med modul.",
                "Bronze: Energien er tændt.",
                "Sølv: Produktion kører på energien.",
                "Guld: Du er energiingeniør.");

            Add("rolig_drift", "Rolig drift",
                "Du holder både mark- og stald-linje i gang samtidig.",
                "Mark-linje og stald-linje har begge leveret i samme session.",
                "Laden har modtaget fra begge.",
                "Minispil: To linjer (høj score) og begge linjer uden stop i forløbet.",
                "Bronze: Begge linjer har leveret.",
                "Sølv: Laden har fået fra begge.",
                "Guld: Du holder rolig drift.");

            Add("kort_vej", "Kort vej",
                "Du vælger effektive veje fra produktion til laden.",
                "Mark→laden leverance.",
                "Stald→laden leverance.",
                "Minispil: Kortest vej (høj score) og begge ruter effektive.",
                "Bronze: Marken når laden.",
                "Sølv: Stalden når laden.",
                "Guld: Du finder den korte vej.");

            Add("smart_strom", "Smart strøm",
                "Du bruger grøn energi smart på gården.",
                "Grøn energi-kilde aktiv.",
                "Produktion under grøn energi.",
                "Minispil: Match behov (høj score) og produktion under grøn energi.",
                "Bronze: Grøn energi er aktiv.",
                "Sølv: Du producerer på grøn energi.",
                "Guld: Du bruger smart strøm.");

            // Natur
            Add("dyreven", "Dyreven",
                "Du passer dyrene i stalden.",
                "Kan passe dyr — foder til stald, output fra dyr.",
                "Dyrene har det godt — foder ind + gylle væk + OK output. Minispil: Plads & renhed.",
                "Dyrenes bedste ven — foder→dyr→gylle videre i kredsløb + høj score i dyre-minispil.",
                "Bronze: Du kan passe dyr.",
                "Sølv: Dyrene har det godt hos dig.",
                "Guld: Du er dyrenes bedste ven.");

            Add("biodiversitetsbygger", "Biodiversitetsbygger",
                "Du laver plads til natur og nyttedyr ved marken.",
                "Natur/kant-element aktiv sammen med mark.",
                "Mark leverer mens natur-element er aktivt.",
                "Minispil: Nyttedyr (høj score) og mark-leverance under natur-aktiv.",
                "Bronze: Du har sat natur i gang ved marken.",
                "Sølv: Mark og natur kører sammen.",
                "Guld: Du er biodiversitetsbygger.");

            Add("naturplejer", "Naturplejer",
                "Du plejer naturen samtidig med skånsom næring.",
                "Natur aktiv + næring via kompost.",
                "Natur-tilstand OK efter en produktions-run.",
                "Minispil: Habitater (høj score) og natur OK efter run med kompost-næring.",
                "Bronze: Natur og kompost hænger sammen.",
                "Sølv: Naturen har det godt efter drift.",
                "Guld: Du er naturplejer.");

            Add("skaansom_helt", "Skånsom helt",
                "Du beskytter afgrøder med nyttedyr — skånsomt.",
                "Nyttedyr-boost aktiv på mark.",
                "Høst under nyttedyr-boost.",
                "Minispil: Skadedyr eller nyttedyr? (høj score) og høst under boost.",
                "Bronze: Nyttedyr er aktive.",
                "Sølv: Du høster under nyttedyr-boost.",
                "Guld: Du er den skånsomme helt.");
        }

        public static BadgeInfo Get(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey)) return null;
            return ByKey.TryGetValue(slotKey, out BadgeInfo info) ? info : null;
        }

        public static string Title(string slotKey)
        {
            BadgeInfo info = Get(slotKey);
            return info != null ? info.Title : (slotKey ?? "?");
        }

        public static string ShortLabel(string slotKey)
        {
            string t = Title(slotKey);
            if (t.Length <= 10) return t;
            return t.Length <= 12 ? t : t.Substring(0, 10) + "…";
        }

        /// <summary>Tekst til info-fane ud fra nuværende point (0–300).</summary>
        public static string BuildDetailBody(string slotKey, int points)
        {
            BadgeInfo info = Get(slotKey);
            string title = Title(slotKey);
            string tier = BadgeData.TierFromPoints(points);
            if (info == null)
            {
                return $"{title}\n\nPoint: {points}/300 · {tier}\n\n(Ingen beskrivelse endnu.)";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(info.Title);
            sb.AppendLine();
            sb.AppendLine(info.Summary);
            sb.AppendLine();
            sb.AppendLine($"Status: {points}/300 · {DanishTier(tier)}");
            sb.AppendLine();

            if (points < 100)
            {
                sb.AppendLine("Sådan opnår du badge:");
                sb.AppendLine();
                sb.AppendLine("● Bronze");
                sb.AppendLine(info.BronzeHow);
                sb.AppendLine();
                sb.AppendLine("● Sølv (efter bronze)");
                sb.AppendLine(info.SilverHow);
                sb.AppendLine();
                sb.AppendLine("● Guld (efter sølv)");
                sb.AppendLine(info.GoldHow);
            }
            else if (points < 200)
            {
                sb.AppendLine(info.EarnedBronze);
                sb.AppendLine();
                sb.AppendLine("Næste trin — Sølv:");
                sb.AppendLine(info.SilverHow);
                sb.AppendLine();
                sb.AppendLine("Senere — Guld:");
                sb.AppendLine(info.GoldHow);
            }
            else if (points < 300)
            {
                sb.AppendLine(info.EarnedSilver);
                sb.AppendLine();
                sb.AppendLine("Næste trin — Guld:");
                sb.AppendLine(info.GoldHow);
            }
            else
            {
                sb.AppendLine(info.EarnedGold);
                sb.AppendLine();
                sb.AppendLine("Du har guld i dette badge. Godt gået!");
            }

            return sb.ToString().TrimEnd();
        }

        public static string DanishTier(string tier) => (tier ?? "Gray").ToLowerInvariant() switch
        {
            "gold" => "Guld",
            "silver" => "Sølv",
            "bronze" => "Bronze",
            _ => "Ikke optjent",
        };
    }
}
