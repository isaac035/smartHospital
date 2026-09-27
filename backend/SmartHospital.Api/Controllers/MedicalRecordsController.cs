using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Api.DTOs.Emr;
using SmartHospital.Api.Services.Interfaces;

namespace SmartHospital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IEmrAuditService? _auditService;

    public MedicalRecordsController(
        IMedicalRecordService medicalRecordService,
        IEmrAuditService? auditService = null)
    {
        _medicalRecordService = medicalRecordService;
        _auditService = auditService;
    }

    [HttpGet]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] MedicalRecordQueryFilter filter)
    {
        filter ??= new MedicalRecordQueryFilter();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);

        if (role == "Patient")
        {
            if (filter.PatientId.HasValue && filter.PatientId.Value != currentUserId)
            {
                if (_auditService != null && currentUserId > 0)
                {
                    await _auditService.LogAsync(currentUserId, "Search", "MedicalRecord", null, filter.PatientId, "Unauthorized search attempt by patient", isSuccess: false);
                }
                return Forbid();
            }

            filter.PatientId = currentUserId;
        }

        try
        {
            var result = await _medicalRecordService.SearchAsync(filter);
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "Search", "MedicalRecord", null, filter.PatientId, $"Search records: page={result.Page}, count={result.Items.Count}, total={result.TotalCount}", isSuccess: true);
            }
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("patient/{patientId:int}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        if (patientId <= 0)
        {
            return BadRequest(new { message = "Invalid patient identifier." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);
        if (role == "Patient" && currentUserId != patientId)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "ViewList", "MedicalRecord", null, patientId, "Unauthorized access attempt by patient", isSuccess: false);
            }
            return Forbid();
        }

        try
        {
            var records = await _medicalRecordService.GetByPatientIdAsync(patientId);
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "ViewList", "MedicalRecord", null, patientId, $"Viewed medical records for patient {patientId}", isSuccess: true);
            }
            return Ok(records);
        }
        catch (ArgumentException ex)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "ViewList", "MedicalRecord", null, patientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "ViewList", "MedicalRecord", null, patientId, ex.Message, isSuccess: false);
            }
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);

        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "View", "MedicalRecord", id, null, "Medical record not found", isSuccess: false);
            }
            return NotFound(new { message = "Medical record not found." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        if (role == "Patient" && record.PatientId != currentUserId)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "View", "MedicalRecord", id, record.PatientId, "Unauthorized access attempt by patient", isSuccess: false);
            }
            return Forbid();
        }

        if (_auditService != null && currentUserId > 0)
        {
            await _auditService.LogAsync(currentUserId, "View", "MedicalRecord", record.Id, record.PatientId, $"RecordNumber: {record.RecordNumber}", isSuccess: true);
        }

        return Ok(record);
    }

    [HttpGet("appointment/{appointmentId:int}")]
    public async Task<IActionResult> GetByAppointment(int appointmentId)
    {
        if (appointmentId <= 0)
        {
            return BadRequest(new { message = "Invalid appointment identifier." });
        }

        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int.TryParse(currentUserIdStr, out var currentUserId);

        var record = await _medicalRecordService.GetByAppointmentIdAsync(appointmentId);
        if (record == null)
        {
            return NotFound(new { message = "Medical record not found for this appointment." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        if (role == "Patient" && record.PatientId != currentUserId)
        {
            if (_auditService != null && currentUserId > 0)
            {
                await _auditService.LogAsync(currentUserId, "View", "MedicalRecord", record.Id, record.PatientId, "Unauthorized access attempt by patient", isSuccess: false);
            }
            return Forbid();
        }

        if (_auditService != null && currentUserId > 0)
        {
            await _auditService.LogAsync(currentUserId, "View", "MedicalRecord", record.Id, record.PatientId, $"RecordNumber: {record.RecordNumber}; AppointmentId: {appointmentId}", isSuccess: true);
        }

        return Ok(record);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMedicalRecordRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var created = await _medicalRecordService.CreateAsync(doctorId, request);
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Create", "MedicalRecord", created.Id, created.PatientId, $"RecordNumber: {created.RecordNumber}", isSuccess: true);
            }
            return Created($"/api/medicalrecords/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Create", "MedicalRecord", null, request.PatientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Create", "MedicalRecord", null, request.PatientId, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicalRecordRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var updated = await _medicalRecordService.UpdateAsync(id, doctorId, request);
            if (updated == null)
            {
                if (_auditService != null)
                {
                    await _auditService.LogAsync(doctorId, "Update", "MedicalRecord", id, null, "Medical record not found or not editable by this doctor", isSuccess: false);
                }
                return NotFound(new { message = "Medical record not found or not editable by this doctor." });
            }

            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Update", "MedicalRecord", updated.Id, updated.PatientId, $"RecordNumber: {updated.RecordNumber}", isSuccess: true);
            }
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Update", "MedicalRecord", id, null, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(doctorId, "Update", "MedicalRecord", id, null, ex.Message, isSuccess: false);
            }
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/diagnoses")]
    public async Task<IActionResult> GetDiagnoses(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "Medical record not found." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (role == "Patient" && int.TryParse(currentUserIdStr, out var currentUserId) && record.PatientId != currentUserId)
        {
            return Forbid();
        }

        try
        {
            var diagnoses = await _medicalRecordService.GetDiagnosesAsync(id);
            return Ok(diagnoses);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/diagnoses")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> AddDiagnosis(int id, [FromBody] AddDiagnosisRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var created = await _medicalRecordService.AddDiagnosisAsync(id, doctorId, request);
            return Created($"/api/medicalrecords/{id}/diagnoses/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/diagnoses/{diagnosisId:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateDiagnosis(int id, int diagnosisId, [FromBody] UpdateDiagnosisRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (diagnosisId <= 0)
        {
            return BadRequest(new { message = "Invalid diagnosis identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var updated = await _medicalRecordService.UpdateDiagnosisAsync(id, diagnosisId, doctorId, request);
            if (updated == null)
            {
                return NotFound(new { message = "Diagnosis not found on this medical record." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/diagnosis")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdatePrimaryDiagnosis(int id, [FromBody] UpdateDiagnosisRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var updated = await _medicalRecordService.UpdatePrimaryDiagnosisAsync(id, doctorId, request);
            if (updated == null)
            {
                return NotFound(new { message = "Medical record not found." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/treatment-plans")]
    public async Task<IActionResult> GetTreatmentPlans(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "Medical record not found." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (role == "Patient" && int.TryParse(currentUserIdStr, out var currentUserId) && record.PatientId != currentUserId)
        {
            return Forbid();
        }

        try
        {
            var plans = await _medicalRecordService.GetTreatmentPlansAsync(id);
            return Ok(plans);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/treatment-plans")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> RecordTreatmentPlan(int id, [FromBody] RecordTreatmentPlanRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var created = await _medicalRecordService.RecordTreatmentPlanAsync(id, doctorId, request);
            return Created($"/api/medicalrecords/{id}/treatment-plans/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/treatment-plans/{planId:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateTreatmentPlan(int id, int planId, [FromBody] UpdateTreatmentPlanRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (planId <= 0)
        {
            return BadRequest(new { message = "Invalid treatment plan identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var updated = await _medicalRecordService.UpdateTreatmentPlanAsync(id, planId, doctorId, request);
            if (updated == null)
            {
                return NotFound(new { message = "Treatment plan not found on this medical record." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/treatment-plan")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdatePrimaryTreatmentPlan(int id, [FromBody] UpdateTreatmentPlanRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body cannot be null." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var doctorIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(doctorIdStr, out var doctorId) || doctorId <= 0)
        {
            return Unauthorized(new { message = "Invalid doctor identification." });
        }

        try
        {
            var updated = await _medicalRecordService.UpdatePrimaryTreatmentPlanAsync(id, doctorId, request);
            if (updated == null)
            {
                return NotFound(new { message = "Medical record not found." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/versions")]
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetVersionHistory(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "Medical record not found." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (role == "Patient" && int.TryParse(currentUserIdStr, out var currentUserId) && record.PatientId != currentUserId)
        {
            return Forbid();
        }

        try
        {
            var versions = await _medicalRecordService.GetVersionHistoryAsync(id);
            return Ok(versions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/versions/{versionNumber:int}")]
    public async Task<IActionResult> GetVersion(int id, int versionNumber)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid medical record identifier." });
        }

        if (versionNumber <= 0)
        {
            return BadRequest(new { message = "Invalid version number." });
        }

        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null)
        {
            return NotFound(new { message = "Medical record not found." });
        }

        var role = User?.FindFirstValue(ClaimTypes.Role);
        var currentUserIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (role == "Patient" && int.TryParse(currentUserIdStr, out var currentUserId) && record.PatientId != currentUserId)
        {
            return Forbid();
        }

        try
        {
            var version = await _medicalRecordService.GetVersionAsync(id, versionNumber);
            if (version == null)
            {
                return NotFound(new { message = $"Version {versionNumber} not found for medical record {id}." });
            }

            return Ok(version);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
