class PrescriptionItemModel {
  final int id;
  final String medicineName;
  final String dosage;
  final String route;
  final String frequency;
  final int durationDays;
  final String specialInstructions;

  PrescriptionItemModel({
    required this.id,
    required this.medicineName,
    required this.dosage,
    required this.route,
    required this.frequency,
    required this.durationDays,
    required this.specialInstructions,
  });

  factory PrescriptionItemModel.fromJson(Map<String, dynamic> json) {
    return PrescriptionItemModel(
      id: json['id'] ?? 0,
      medicineName: json['medicineName'] ?? '',
      dosage: json['dosage'] ?? '',
      route: json['route'] ?? '',
      frequency: json['frequency'] ?? '',
      durationDays: json['durationDays'] ?? 1,
      specialInstructions: json['specialInstructions'] ?? '',
    );
  }
}

class PrescriptionModel {
  final int id;
  final String prescriptionNumber;
  final int patientId;
  final String doctorName;
  final DateTime issueDate;
  final DateTime? expiryDate;
  final String status;
  final String generalInstructions;
  final List<PrescriptionItemModel> items;

  PrescriptionModel({
    required this.id,
    required this.prescriptionNumber,
    required this.patientId,
    required this.doctorName,
    required this.issueDate,
    this.expiryDate,
    required this.status,
    required this.generalInstructions,
    required this.items,
  });

  factory PrescriptionModel.fromJson(Map<String, dynamic> json) {
    var rawItems = json['items'] as List<dynamic>? ?? [];
    return PrescriptionModel(
      id: json['id'] ?? 0,
      prescriptionNumber: json['prescriptionNumber'] ?? '',
      patientId: json['patientId'] ?? 0,
      doctorName: json['doctorName'] ?? '',
      issueDate: json['issueDate'] != null
          ? DateTime.parse(json['issueDate'])
          : DateTime.now(),
      expiryDate: json['expiryDate'] != null
          ? DateTime.tryParse(json['expiryDate'])
          : null,
      status: json['status'] ?? '',
      generalInstructions: json['generalInstructions'] ?? '',
      items: rawItems.map((i) => PrescriptionItemModel.fromJson(i)).toList(),
    );
  }
}
