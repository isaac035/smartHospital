from app.services.reasoner import _fallback


def test_fallback_is_explicitly_non_ai_and_priority_aware():
    result = _fallback({"appointmentSummary": {"priority": "Emergency"}, "sectionsPresent": ["Patient Information"]})
    assert result.mode == "non_ai_fallback"
    assert result.ai_recommendations == [
        "Staying adequately hydrated may help.",
        "Light regular activity as tolerated by your current condition may help.",
        "Getting adequate rest is generally advisable.",
        "Avoid known triggers if any are documented on file.",
        "If symptoms worsen or new severe symptoms develop, seek immediate medical attention. Follow up with your doctor as they advise.",
    ]


def test_priority_guidance_uses_triage_priority_and_never_invents_a_timeline():
    result = _fallback({
        "appointmentSummary": {"priority": "Normal"},
        "clinicalTriageSummary": {"priority": "Urgent"},
        "sectionsPresent": [],
    })
    assert result.ai_recommendations[-1] == "A follow-up visit within the timeframe recommended by your doctor is advisable."
    assert all("week" not in item.lower() and "month" not in item.lower() for item in result.ai_recommendations)


def test_fallback_does_not_claim_unrecorded_results():
    result = _fallback({"appointmentSummary": {}, "sectionsPresent": []})
    assert "no clinical sections" in result.ai_summary
