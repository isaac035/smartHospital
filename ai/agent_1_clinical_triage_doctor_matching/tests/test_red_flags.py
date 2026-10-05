import pytest

from app.services.doctor_matching import normalize
from app.utils.red_flags import has_emergency_red_flag


@pytest.mark.parametrize(
    "text",
    [
        "I have severe chest pain",
        "crushing pain in my chest",
        "I can’t breathe properly",
        "difficulty breathing since this morning",
        "my face is drooping and speech is slurred",
        "sudden numbness on one side of my body",
        "the bleeding won't stop",
        "he passed out in the kitchen",
        "she had a seizure",
        "my throat is swelling after eating nuts",
        "I want to end my life",
    ],
)
def test_emergency_phrases_flagged(text):
    assert has_emergency_red_flag(text)


@pytest.mark.parametrize(
    "text",
    [
        "itchy rash on my arm",
        "mild back pain after gardening",
        "routine check-up for my child",
        "follow-up for my blood pressure medication review",
        "I need a new pair of glasses",
    ],
)
def test_routine_phrases_not_flagged(text):
    assert not has_emergency_red_flag(text)


def test_normalize_matches_department_spelling_variants():
    assert normalize("General Medicine") == normalize("general  medicine") == normalize("General-Medicine")
    assert normalize("ENT") != normalize("Neurology")
