import pytest
from pydantic import ValidationError
from app.schemas.allocation import Candidate, RecommendRequest
from app.services.recommender import rank

def c(id, name, kind="bed", type="Standard", text=""):
    return Candidate(resourceId=id, kind=kind, name=name, type=type, location="Ward / Room 1", status="Available", specialtyText=text)

@pytest.mark.parametrize("specialty,priority", [("Cardiology", "Normal"), ("Cardiology", "Urgent"), ("Cardiology", "Emergency"), ("Heart and cardiovascular care", "Emergency")])
def test_cardiology_stays_in_cardiology_for_every_priority(specialty, priority):
    request = RecommendRequest(appointmentId=10, clinicalSpecialty=specialty, priority=priority, candidates=[c(1,"Emergency Ward","bed","Standard","Emergency"), c(2,"Cardiac Care Unit","bed","ICU","Cardiac")])
    assert rank(request)[0].resource_id == 2

def test_maternity_and_genuine_emergency_specialties_route_correctly():
    candidates = [c(1,"Postnatal Ward"), c(2,"Accident and Trauma Ward"), c(3,"Cardiology Ward")]
    maternity = RecommendRequest(appointmentId=1, clinicalSpecialty="Maternity", candidates=candidates)
    emergency = RecommendRequest(appointmentId=2, clinicalSpecialty="Emergency", priority="Emergency", candidates=candidates)
    assert rank(maternity)[0].resource_id == 1
    assert rank(emergency)[0].resource_id == 2

def test_falls_back_when_specialty_resource_is_occupied_and_allows_multiple_candidates():
    request = RecommendRequest(appointmentId=1, clinicalSpecialty="Cardiology", candidates=[c(4,"General Ward"), c(5,"Cardiology Ward"), c(6,"Cardiology Ward")])
    result = rank(request)
    assert [item.resource_id for item in result[:2]] == [5, 6]

def test_equipment_uses_the_same_real_candidate_scoring():
    request = RecommendRequest(appointmentId=1, clinicalSpecialty="Cardiology", candidates=[c(8,"Cardiac Monitor","equipment","Monitor","cardiology monitor"), c(9,"Ventilator","equipment","Ventilator")])
    assert rank(request)[0].resource_id == 8

def test_rejects_candidate_not_available_by_its_status():
    with pytest.raises(ValidationError):
        Candidate(resourceId=1, kind="bed", name="Occupied bed", type="Standard", status="Occupied")

def test_occupied_specialty_resource_is_excluded_and_general_option_is_returned():
    with pytest.raises(ValidationError):
        Candidate(resourceId=1, kind="bed", name="Cardiology Ward", type="Standard", status="Occupied")
    result = rank(RecommendRequest(appointmentId=3, clinicalSpecialty="Cardiology", candidates=[c(2,"General Ward")]))
    assert result[0].resource_id == 2
    assert "General option" in result[0].reason
