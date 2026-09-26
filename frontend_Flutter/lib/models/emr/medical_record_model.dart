class MedicalRecordModel {
  final int id;
  final String recordNumber;
  final int patientId;
  final String patientName;
  final int doctorId;
  final String doctorName;
  final DateTime visitDate;
  final String chiefComplaint;
  final String diagnosis;
  final DateTime? followUpDate;

  MedicalRecordModel({
    required this.id,
    required this.recordNumber,
    required this.patientId,
    required this.patientName,
    required this.doctorId,
    required this.doctorName,
    required this.visitDate,
    required this.chiefComplaint,
    required this.diagnosis,
    this.followUpDate,
  });

  factory MedicalRecordModel.fromJson(Map<String, dynamic> json) {
    return MedicalRecordModel(
      id: json['id'] ?? 0,
      recordNumber: json['recordNumber'] ?? '',
      patientId: json['patientId'] ?? 0,
      patientName: json['patientName'] ?? '',
      doctorId: json['doctorId'] ?? 0,
      doctorName: json['doctorName'] ?? '',
      visitDate: json['visitDate'] != null
          ? DateTime.parse(json['visitDate'])
          : DateTime.now(),
      chiefComplaint: json['chiefComplaint'] ?? '',
      diagnosis: json['diagnosis'] ?? '',
      followUpDate: json['followUpDate'] != null
          ? DateTime.tryParse(json['followUpDate'])
          : null,
    );
  }
}
