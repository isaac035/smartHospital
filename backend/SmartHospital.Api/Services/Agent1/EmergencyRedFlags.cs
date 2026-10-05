using System.Text.RegularExpressions;

namespace SmartHospital.Api.Services.Agent1;

/// <summary>
/// Safety floor used when the Agent 1 service cannot be reached at all, so the
/// emergency notice never depends on the AI being up. Mirrors the primary screen in
/// ai/agent_1_clinical_triage_doctor_matching/app/utils/red_flags.py - keep them in sync.
/// </summary>
public static class EmergencyRedFlags
{
    public const string Notice =
        "If this is a medical emergency, call emergency services or go to the nearest emergency department now.";

    private static readonly Regex[] Patterns = new[]
    {
        @"\bchest\b.{0,25}\b(pain|pressure|tight(ness)?|crushing|squeez\w*)\b",
        @"\b(crushing|severe|sharp)\b.{0,25}\bchest\b",
        @"\b(can'?t|cannot|can not|unable to|struggling to|hard to)\s+breath\w*",
        @"\b(difficulty|trouble|problems?)\s+breathing\b",
        @"\bshort(ness)?\s+of\s+breath\b",
        @"\bnot\s+breathing\b",
        @"\bchoking\b",
        @"\bstroke\b",
        @"\bface\b.{0,15}\bdroop\w*",
        @"\bslurred\s+speech\b",
        @"\b(numb(ness)?|weak(ness)?|paralys\w*)\b.{0,25}\b(one|left|right)\s+side\b",
        @"\bsudden(ly)?\b.{0,25}\b(can'?t|cannot|unable to)\s+(speak|talk|move|see)\b",
        @"\b(severe|heavy|heavily|uncontrolled|a lot of)\b.{0,15}\bbleed\w*",
        @"\bbleed\w*\b.{0,20}\b(won'?t|will not|doesn'?t|does not|can'?t|cannot)\s+stop\b",
        @"\b(coughing|vomiting|throwing)\s+(up\s+)?blood\b",
        @"\b(unconscious|unresponsive|passed\s+out|fainted|collapsed|blacked\s+out)\b",
        @"\bloss\s+of\s+consciousness\b",
        @"\b(seizures?|convuls\w*)\b",
        @"\banaphyla\w*",
        @"\bthroat\b.{0,15}\b(closing|swelling|swollen)\b",
        @"\bsuicid\w*",
        @"\b(kill|hurt|harm)\s+myself\b",
        @"\bend\s+my\s+life\b",
        @"\boverdos\w*",
        @"\bpoison\w*",
    }.Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))).ToArray();

    public static bool Matches(string text)
    {
        var normalized = string.Join(' ', text.Replace('’', '\'').Replace('‘', '\'')
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return Patterns.Any(p => p.IsMatch(normalized));
    }
}
