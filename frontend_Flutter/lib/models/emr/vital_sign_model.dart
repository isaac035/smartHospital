class VitalSignModel {
  final int id;
  final int patientId;
  final DateTime recordedAt;
  final double? temperatureCelsius;
  final int? systolicBloodPressure;
  final int? diastolicBloodPressure;
  final int? heartRateBpm;
  final int? respiratoryRateBpm;
  final double? oxygenSaturationSpO2;
  final double? weightKg;
  final double? heightCm;
  final double? bmi;
  final String notes;

  VitalSignModel({
    required this.id,
    required this.patientId,
    required this.recordedAt,
    this.temperatureCelsius,
    this.systolicBloodPressure,
    this.diastolicBloodPressure,
    this.heartRateBpm,
    this.respiratoryRateBpm,
    this.oxygenSaturationSpO2,
    this.weightKg,
    this.heightCm,
    this.bmi,
    required this.notes,
  });

  factory VitalSignModel.fromJson(Map<String, dynamic> json) {
    return VitalSignModel(
      id: json['id'] ?? 0,
      patientId: json['patientId'] ?? 0,
      recordedAt: json['recordedAt'] != null
          ? DateTime.parse(json['recordedAt'])
          : DateTime.now(),
      temperatureCelsius: (json['temperatureCelsius'] as num?)?.toDouble(),
      systolicBloodPressure: json['systolicBloodPressure'],
      diastolicBloodPressure: json['diastolicBloodPressure'],
      heartRateBpm: json['heartRateBpm'],
      respiratoryRateBpm: json['respiratoryRateBpm'],
      oxygenSaturationSpO2: (json['oxygenSaturationSpO2'] as num?)?.toDouble(),
      weightKg: (json['weightKg'] as num?)?.toDouble(),
      heightCm: (json['heightCm'] as num?)?.toDouble(),
      bmi: (json['bmi'] as num?)?.toDouble(),
      notes: json['notes'] ?? '',
    );
  }
}
