namespace FarmerQuest.Server.GameFeatures.Farm;

/// <summary>
/// Bank/debt
/// </summary>
public static class FarmBankService
{
    // Attempts to borrow cash within bank limits; increases money and debt
    public static (bool ok, string? error) TryBorrow(FarmGameState state, int amount)
    {
        if (amount < FarmBankDefaults.MinBorrow)
            return (false, $"Du skal låne mindst {FarmBankDefaults.MinBorrow} kr.");

        if (amount > FarmBankDefaults.MaxBorrowPerRequest)
            return (false, $"Du kan højst låne {FarmBankDefaults.MaxBorrowPerRequest} kr ad gangen.");

        // Loans must be in 50 kr steps
        if (amount % 50 != 0)
            return (false, "Lån skal være i trin af 50 kr.");

        // Remaining credit under MaxDebt
        int room = FarmBankDefaults.MaxDebt - state.Debt;
        if (room <= 0)
            return (false, "Du har nået bankens kreditloft.");

        if (amount > room)
            return (false, $"Du kan højst låne {room} kr mere (loft {FarmBankDefaults.MaxDebt} kr).");

        state.Money += amount;
        state.Debt += amount;
        state.TotalBorrowed += amount;
        state.EventLog.Add($"DEBUG Bank: Lånt {amount} kr. Gæld {state.Debt} kr.");
        TrimLog(state);
        return (true, null);
    }

    // Repays debt from cash. amount ≤ 0 means pay as much as possible
    public static (bool ok, string? error, int paid) TryRepay(FarmGameState state, int amount)
    {
        if (state.Debt <= 0)
            return (false, "Du har ingen gæld.", 0);

        // Cap payment by requested amount, available cash, and outstanding debt
        int want = amount <= 0 ? state.Debt : amount;
        int pay = Math.Min(want, Math.Min(state.Money, state.Debt));
        if (pay <= 0)
            return (false, "Ikke nok penge til at afdrage.", 0);

        state.Money -= pay;
        state.Debt -= pay;
        state.TotalRepaid += pay;

        // Clear collections flag when debt is fully paid
        if (state.Debt <= 0)
        {
            state.Debt = 0;
            state.InkassoActive = false;
            state.EventLog.Add($"DEBUG Bank: Gæld betalt ({pay} kr).");
        }
        else
            state.EventLog.Add($"DEBUG Bank: Afdrag {pay} kr. Gæld {state.Debt} kr.");

        TrimLog(state);
        return (true, null, pay);
    }

    // Manual one-shot interest. amount&gt;0 = fixed amount, otherwise % of debt
    public static (bool ok, string? error, int interest) ChargeInterestOnce(FarmGameState state, int amount)
    {
        if (state.Debt <= 0)
            return (false, "Ingen gæld at tilskrive rente på.", 0);

        int interest = amount > 0
            ? amount
            : Math.Max(1, (int)Math.Ceiling(state.Debt * FarmBankDefaults.InterestRateManual));

        state.Debt += interest;
        state.TotalInterestCharged += interest;
        state.EventLog.Add($"DEBUG Bank: Rente +{interest} kr. Gæld {state.Debt} kr.");
        TrimLog(state);
        return (true, null, interest);
    }

    // Collections seize one amount once (not repeated over time)
    public static (bool ok, string? error, int seized) InkassoOnce(FarmGameState state, int amount)
    {
        if (state.Debt <= 0)
            return (false, "Ingen gæld til inkasso.", 0);
        if (state.Money <= 0)
            return (false, "Ingen kontanter at opkræve.", 0);

        // Default seize amount when caller passes 0
        int want = amount > 0 ? amount : FarmBankDefaults.DefaultInkassoSeize;
        int seize = Math.Min(want, Math.Min(state.Money, state.Debt));
        if (seize <= 0)
            return (false, "Kunne ikke opkræve beløb.", 0);

        state.Money -= seize;
        state.Debt -= seize;
        state.TotalRepaid += seize;
        state.InkassoActive = state.Debt > 0;

        if (state.Debt <= 0)
        {
            state.Debt = 0;
            state.InkassoActive = false;
            state.EventLog.Add($"DEBUG Inkasso: Opkrævet {seize} kr. Gæld lukket.");
        }
        else
            state.EventLog.Add($"DEBUG Inkasso: Opkrævet {seize} kr. Gæld {state.Debt} kr.");

        TrimLog(state);
        return (true, null, seize);
    }

    // Debug helper: adjusts cash by delta (clamped at 0)
    public static (bool ok, string? error) AdjustMoney(FarmGameState state, int delta)
    {
        if (delta == 0)
            return (false, "Beløb må ikke være 0.");

        state.Money = Math.Max(0, state.Money + delta);
        state.EventLog.Add(delta > 0
            ? $"DEBUG: +{delta} kr (saldo {state.Money})."
            : $"DEBUG: {delta} kr (saldo {state.Money}).");
        TrimLog(state);
        return (true, null);
    }

    // Debug helper: adjusts CO₂ by delta (clamped at 0)
    public static (bool ok, string? error) AdjustCo2(FarmGameState state, int delta)
    {
        if (delta == 0)
            return (false, "CO₂-ændring må ikke være 0.");

        state.Co2 = Math.Max(0f, state.Co2 + delta);
        state.EventLog.Add(delta > 0
            ? $"DEBUG: CO₂ +{delta} (nu {state.Co2:0})."
            : $"DEBUG: CO₂ {delta} (nu {state.Co2:0}).");
        TrimLog(state);
        return (true, null);
    }

    // Keeps the in-game event log to a short rolling window
    private static void TrimLog(FarmGameState state)
    {
        while (state.EventLog.Count > 8)
            state.EventLog.RemoveAt(0);
    }
}
