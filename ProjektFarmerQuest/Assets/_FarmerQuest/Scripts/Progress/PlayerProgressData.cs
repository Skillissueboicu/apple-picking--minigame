using System;
using UnityEngine;

namespace FarmerQuest.Progress
{
    public enum PillarCategory
    {
        Maden,
        Holdet,
        Marken,
        Energi,
        Naturen,
    }

    [Serializable]
    public sealed class PillarRingDef
    {
        public PillarCategory category;
        public string key;
        public string label;
        public string colorHex;
        public float startDeg;
        public float sweepDeg;
        public string[] subColors;
        public string[] badgeKeys;
        public string innerBadgeKey;
    }

    [Serializable]
    public class PlayerProgressData
    {
        public const int MaxXp = 1000;
        public const int MaxSlotPoints = 300;

        public int totalXp;
        public int markenXp;
        public int naturenXp;
        public int energiXp;
        public int madenXp;
        public int holdetXp;
        public int roundsPlayed;
        public BadgeData[] badges = Array.Empty<BadgeData>();

        public static readonly PillarRingDef[] Rings =
        {
            new()
            {
                category = PillarCategory.Maden, key = "madkurven", label = "Landbrugsproduktion",
                colorHex = "#FFC215", startDeg = 0f, sweepDeg = 90f,
                subColors = new[] { "#BC8B00", "#DEA400", "#FAB900", "#FFC729", "#FFD253", "#FFDD7D", "#FFE497" },
                badgeKeys = new[]
                {
                    "planteekspert", "saesonmester", "spildkriger", "jordven",
                    "drivhusgartner", "hostklar", "saedskiftespire",
                },
                innerBadgeKey = "salgsansvarlig",
            },
            new()
            {
                category = PillarCategory.Holdet, key = "teamwork", label = "Teknologi",
                colorHex = "#4573C4", startDeg = 90f, sweepDeg = 90f,
                subColors = new[] { "#2E5292", "#3D6BBD", "#5780C9", "#7395D3", "#87A4D9", "#AABFE4", "#B0C3E6" },
                badgeKeys = new[]
                {
                    "holdkaptajn", "rormester", "opfinderen", "logistikhelt",
                    "vedligeholder", "planlaegger", "dokumentarist",
                },
                innerBadgeKey = "fejlfinder",
            },
            new()
            {
                category = PillarCategory.Marken, key = "vand", label = "Klimahandling",
                colorHex = "#5396A0", startDeg = 180f, sweepDeg = 76f,
                subColors = new[] { "#3D7077", "#437A81", "#579DA7", "#7EB5BC", "#9AC4CA", "#B1D2D7" },
                badgeKeys = new[]
                {
                    "jorddetektiv", "vandvogter", "genbrugshelt",
                    "klimatilpasser", "kulstoftaenker", "ren_routing",
                },
                innerBadgeKey = "co2_jaeger",
            },
            new()
            {
                category = PillarCategory.Energi, key = "energi", label = "Energistyring",
                colorHex = "#96C259", startDeg = 256f, sweepDeg = 52f,
                subColors = new[] { "#56762C", "#729D39", "#93C058", "#B3D389" },
                badgeKeys = new[] { "energiingenioer", "rolig_drift", "kort_vej", "smart_strom" },
                innerBadgeKey = "stromspare",
            },
            new()
            {
                category = PillarCategory.Naturen, key = "naturen", label = "Naturbevaring",
                colorHex = "#C1E5F9", startDeg = 308f, sweepDeg = 52f,
                subColors = new[] { "#C1E5F9", "#85CCF3", "#50B6EE", "#22A3EA" },
                badgeKeys = new[] { "dyreven", "biodiversitetsbygger", "naturplejer", "skaansom_helt" },
                innerBadgeKey = "landskabsplejer",
            },
        };

        /// <summary>Slot-key for det indre kategori-felt (adskilt fra ydre badges).</summary>
        public static string InnerSlotKey(PillarRingDef ring) =>
            string.IsNullOrEmpty(ring?.innerBadgeKey) ? $"inner:{ring?.key}" : ring.innerBadgeKey;

        public static bool IsInnerSlotKey(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey)) return false;
            foreach (PillarRingDef ring in Rings)
            {
                if (string.Equals(InnerSlotKey(ring), slotKey, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static string InnerSlotLabel(PillarRingDef ring) =>
            $"{ring.label} · {BadgeCatalog.Title(InnerSlotKey(ring))}";

        public static string SlotKey(PillarRingDef ring, int segmentIndex)
        {
            if (ring?.badgeKeys != null && segmentIndex >= 0 && segmentIndex < ring.badgeKeys.Length
                && !string.IsNullOrEmpty(ring.badgeKeys[segmentIndex]))
                return ring.badgeKeys[segmentIndex];
            return $"seg:{ring.key}:{segmentIndex}";
        }

        public static string SlotLabel(PillarRingDef ring, int segmentIndex)
        {
            string key = SlotKey(ring, segmentIndex);
            return $"{ring.label} · {BadgeCatalog.Title(key)}";
        }

        public int GetSlotPoints(string slotKey)
        {
            BadgeData b = FindBadge(slotKey);
            return b?.points ?? 0;
        }

        /// <summary>Ydre felt fyld — 0 hvis kategorien er låst (indre badge ikke optjent).</summary>
        public float GetSegmentFill01(PillarRingDef ring, int segmentIndex)
        {
            if (!IsOuterUnlocked(ring)) return 0f;
            return Mathf.Clamp01(GetSlotPoints(SlotKey(ring, segmentIndex)) / (float)MaxSlotPoints);
        }

        /// <summary>Indre felt fyldes kun fra sit eget badge — ikke fra ydre slices.</summary>
        public float GetInnerFill01(PillarRingDef ring) =>
            Mathf.Clamp01(GetSlotPoints(InnerSlotKey(ring)) / (float)MaxSlotPoints);

        /// <summary>True hvis ydre badges i kategorien må optjenes / vises fyldt.</summary>
        public bool IsOuterUnlocked(PillarRingDef ring)
        {
            if (!PlayerProgressRules.LockOuterUntilInnerComplete) return true;
            return GetSlotPoints(InnerSlotKey(ring)) >= PlayerProgressRules.InnerCompletePoints;
        }

        public bool IsOuterSlot(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey)) return false;
            if (IsInnerSlotKey(slotKey)) return false;
            foreach (PillarRingDef ring in Rings)
            {
                if (string.Equals(InnerSlotKey(ring), slotKey, StringComparison.OrdinalIgnoreCase))
                    return false;
                int n = ring.subColors?.Length ?? 0;
                for (int i = 0; i < n; i++)
                {
                    if (string.Equals(SlotKey(ring, i), slotKey, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        public PillarRingDef FindRingForSlot(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey)) return null;
            foreach (PillarRingDef ring in Rings)
            {
                if (string.Equals(InnerSlotKey(ring), slotKey, StringComparison.OrdinalIgnoreCase))
                    return ring;
                int n = ring.subColors?.Length ?? 0;
                for (int i = 0; i < n; i++)
                {
                    if (string.Equals(SlotKey(ring, i), slotKey, StringComparison.OrdinalIgnoreCase))
                        return ring;
                }
            }
            return null;
        }

        /// <summary>False hvis ydre slot er låst og man prøver at sætte point &gt; 0.</summary>
        public bool CanSetSlotPoints(string slotKey, int points)
        {
            if (points <= 0) return true;
            if (!PlayerProgressRules.LockOuterUntilInnerComplete) return true;
            if (!IsOuterSlot(slotKey)) return true;
            PillarRingDef ring = FindRingForSlot(slotKey);
            return ring != null && IsOuterUnlocked(ring);
        }

        public int GetXp(PillarCategory c) => c switch
        {
            PillarCategory.Marken => markenXp,
            PillarCategory.Naturen => naturenXp,
            PillarCategory.Energi => energiXp,
            PillarCategory.Maden => madenXp,
            PillarCategory.Holdet => holdetXp,
            _ => 0,
        };

        public BadgeData FindBadge(string key)
        {
            if (string.IsNullOrEmpty(key) || badges == null) return null;
            foreach (BadgeData b in badges)
                if (string.Equals(b.badgeKey, key, StringComparison.OrdinalIgnoreCase))
                    return b;
            return null;
        }

        public static string ApiKey(PillarCategory c) => c switch
        {
            PillarCategory.Marken => "klimahandling",
            PillarCategory.Naturen => "naturbevaring",
            PillarCategory.Energi => "energistyring",
            PillarCategory.Maden => "landbrugsproduktion",
            PillarCategory.Holdet => "teknologi",
            _ => "klimahandling",
        };

        public static string Label(PillarCategory c)
        {
            foreach (PillarRingDef r in Rings)
                if (r.category == c) return r.label;
            return c.ToString();
        }

        public static Color ColorOf(PillarCategory c)
        {
            foreach (PillarRingDef r in Rings)
                if (r.category == c) return Hex(r.colorHex);
            return Color.white;
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return Color.magenta;
        }
    }

    [Serializable]
    public class BadgeData
    {
        public string badgeKey;
        public string tier;
        public int completions;
        public int bestCompletions;
        public int points;

        public static Color TierColor(string tier) => (tier ?? "Gray").ToLowerInvariant() switch
        {
            "gold" => PlayerProgressData.Hex("#B8860B"),
            "silver" => PlayerProgressData.Hex("#7D8590"),
            "bronze" => PlayerProgressData.Hex("#7A4B23"),
            _ => PlayerProgressData.Hex("#D1D5DB"),
        };

        public static string TierFromPoints(int points) => points switch
        {
            >= 300 => "Gold",
            >= 200 => "Silver",
            >= 100 => "Bronze",
            _ => "Gray",
        };
    }
}
