/*
 * BloodThinnerTracker.Web - Common Blood Thinners Data
 * Licensed under MIT License. See LICENSE file in the project root.
 *
 * Data source for medication name autocomplete with common blood thinner medications.
 */

namespace BloodThinnerTracker.Web.Data;

/// <summary>
/// Common blood thinner medications for autocomplete functionality.
/// </summary>
public static class CommonBloodThinners
{
    /// <summary>
    /// List of common blood thinner medications with brand and generic names.
    /// </summary>
    public static List<MedicationSuggestion> Medications { get; } = new()
    {
        // Vitamin K Antagonists (VKAs)
        new("Acenocoumarol", "Sintrom", "Vitamin K Antagonist", "Anticoagulant", "Tablet"),
        new("Warfarin", "Coumadin", "Vitamin K Antagonist", "Anticoagulant", "Tablet"),
        new("Warfarin Sodium", "Coumadin, Jantoven", "Vitamin K Antagonist", "Anticoagulant", "Tablet"),

        // Direct Oral Anticoagulants (DOACs) - Factor Xa Inhibitors
        new("Apixaban", "Eliquis", "Factor Xa Inhibitor", "Anticoagulant", "Tablet"),
        new("Rivaroxaban", "Xarelto", "Factor Xa Inhibitor", "Anticoagulant", "Tablet"),
        new("Edoxaban", "Savaysa, Lixiana", "Factor Xa Inhibitor", "Anticoagulant", "Tablet"),
        new("Betrixaban", "Bevyxxa", "Factor Xa Inhibitor", "Anticoagulant", "Capsule"),

        // Direct Thrombin Inhibitors
        new("Dabigatran", "Pradaxa", "Direct Thrombin Inhibitor", "Anticoagulant", "Capsule"),
        new("Dabigatran Etexilate", "Pradaxa", "Direct Thrombin Inhibitor", "Anticoagulant", "Capsule"),

        // Injectable Anticoagulants
        new("Enoxaparin", "Lovenox", "Low Molecular Weight Heparin", "Anticoagulant", "Injection"),
        new("Dalteparin", "Fragmin", "Low Molecular Weight Heparin", "Anticoagulant", "Injection"),
        new("Heparin", "Various", "Unfractionated Heparin", "Anticoagulant", "Injection"),
        new("Fondaparinux", "Arixtra", "Factor Xa Inhibitor", "Anticoagulant", "Injection"),

        // Antiplatelet Agents
        new("Aspirin", "Bayer, Ecotrin", "Antiplatelet", "Antiplatelet Agent", "Tablet"),
        new("Clopidogrel", "Plavix", "Antiplatelet", "Antiplatelet Agent", "Tablet"),
        new("Prasugrel", "Effient", "Antiplatelet", "Antiplatelet Agent", "Tablet"),
        new("Ticagrelor", "Brilinta", "Antiplatelet", "Antiplatelet Agent", "Tablet"),
        new("Dipyridamole", "Persantine", "Antiplatelet", "Antiplatelet Agent", "Tablet"),

        // Combination Products
        new("Aspirin/Dipyridamole", "Aggrenox", "Combination Antiplatelet", "Antiplatelet Agent", "Capsule"),

        // ACE Inhibitors (Angiotensin-Converting Enzyme Inhibitors)
        new("Perindopril", "Coversyl, Aceon", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),
        new("Enalapril", "Vasotec", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),
        new("Lisinopril", "Prinivil, Zestril", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),
        new("Ramipril", "Altace, Tritace", "ACE Inhibitor", "Hypertension/Heart Failure", "Capsule"),
        new("Captopril", "Capoten", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),
        new("Quinapril", "Accupril", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),
        new("Fosinopril", "Monopril", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),
        new("Benazepril", "Lotensin", "ACE Inhibitor", "Hypertension/Heart Failure", "Tablet"),

        // Beta Blockers (Beta-Adrenergic Blocking Agents)
        new("Propranolol", "Inderal, Hemangeol", "Beta Blocker", "Hypertension/Arrhythmia/Angina", "Tablet"),
        new("Bisoprolol", "Zebeta, Cardicor", "Beta Blocker", "Hypertension/Heart Failure", "Tablet"),
        new("Metoprolol", "Lopressor, Toprol-XL", "Beta Blocker", "Hypertension/Angina/Heart Failure", "Tablet"),
        new("Atenolol", "Tenormin", "Beta Blocker", "Hypertension/Angina", "Tablet"),
        new("Carvedilol", "Coreg", "Beta Blocker", "Hypertension/Heart Failure", "Tablet"),
        new("Nebivolol", "Bystolic", "Beta Blocker", "Hypertension", "Tablet"),
        new("Labetalol", "Trandate, Normodyne", "Beta Blocker", "Hypertension", "Tablet"),
        new("Sotalol", "Betapace, Sorine", "Beta Blocker", "Arrhythmia", "Tablet"),
    };

    /// <summary>
    /// Gets medication suggestions filtered by search term.
    /// </summary>
    public static List<MedicationSuggestion> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Medications;

        var term = searchTerm.ToLowerInvariant();

        return Medications
            .Where(m =>
                m.GenericName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.BrandNames.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.DrugClass.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

/// <summary>
/// Represents a medication suggestion for autocomplete.
/// </summary>
public record MedicationSuggestion(
    string GenericName,
    string BrandNames,
    string DrugClass,
    string Indication,
    string Form)
{
    /// <summary>
    /// Gets the display text for the autocomplete dropdown.
    /// </summary>
    public string DisplayText => $"{GenericName} ({BrandNames}) - {DrugClass}";

    /// <summary>
    /// Gets the primary name (generic name).
    /// </summary>
    public string PrimaryName => GenericName;
}
