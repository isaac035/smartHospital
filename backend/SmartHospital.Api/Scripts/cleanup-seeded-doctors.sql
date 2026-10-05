-- One-time shared-database cleanup for the exact doctor identities created by
-- DbSeeder and SampleDataSeeder. Safe to run again: removed identities produce
-- no candidates on subsequent runs.
--
-- Profiles/logins with appointments, queue entries, clinical records, orders,
-- or admissions are reported and preserved. No admin-created doctor outside
-- this explicit seed identity list is considered.

BEGIN;

CREATE TEMP TABLE _seed_doctor_targets (
    profile_email text PRIMARY KEY,
    license_number text NOT NULL,
    user_email text NOT NULL
) ON COMMIT DROP;

INSERT INTO _seed_doctor_targets (profile_email, license_number, user_email) VALUES
    ('nadia.perera@smarthospital.local', 'SLMC-00123', 'doctor@smarthospital.local'),
    ('ashan.wijesekara@smarthospital.local', 'SLMC-10231', 'ashan.wijesekara@smarthospital.local'),
    ('priya.gunawardena@smarthospital.local', 'SLMC-10874', 'priya.gunawardena@smarthospital.local'),
    ('sanjeewa.bandara@smarthospital.local', 'SLMC-11502', 'sanjeewa.bandara@smarthospital.local'),
    ('ishara.desilva@smarthospital.local', 'SLMC-12045', 'ishara.desilva@smarthospital.local'),
    ('roshan.herath@smarthospital.local', 'SLMC-09877', 'roshan.herath@smarthospital.local'),
    ('nimali.karunaratne@smarthospital.local', 'SLMC-11233', 'nimali.karunaratne@smarthospital.local'),
    ('chaminda.ekanayake@smarthospital.local', 'SLMC-08765', 'chaminda.ekanayake@smarthospital.local'),
    ('lakshan.senanayake@smarthospital.local', 'SLMC-13310', 'lakshan.senanayake@smarthospital.local');

CREATE TEMP TABLE _seed_doctor_candidates ON COMMIT DROP AS
SELECT
    target.profile_email,
    target.license_number,
    target.user_email,
    doctor."Id" AS doctor_id,
    doctor."UserId" AS linked_user_id,
    seed_user."Id" AS seed_user_id,
    COALESCE(appointments.appointment_count, 0) AS appointment_count,
    COALESCE(queues.queue_count, 0) AS queue_count,
    CASE
        WHEN doctor."UserId" IS NOT NULL
             AND (seed_user."Id" IS NULL OR doctor."UserId" <> seed_user."Id")
            THEN 'doctor profile has a non-seed or unexpected linked user'
        WHEN seed_user."Id" IS NOT NULL AND (
            EXISTS (SELECT 1 FROM "Appointments" a WHERE a."DoctorId" = seed_user."Id") OR
            EXISTS (SELECT 1 FROM "QueueEntries" q WHERE q."DoctorId" = seed_user."Id") OR
            EXISTS (SELECT 1 FROM "MedicalRecords" m WHERE m."DoctorId" = seed_user."Id") OR
            EXISTS (SELECT 1 FROM "Prescriptions" p WHERE p."DoctorId" = seed_user."Id") OR
            EXISTS (SELECT 1 FROM "LabOrders" l WHERE l."DoctorId" = seed_user."Id") OR
            EXISTS (SELECT 1 FROM "Admissions" ad WHERE ad."AdmittingDoctorId" = seed_user."Id")
        ) THEN 'linked user has appointment, queue, or clinical history'
        WHEN seed_user."Id" IS NOT NULL AND EXISTS (
            SELECT 1 FROM "Doctors" other_doctor
            WHERE other_doctor."UserId" = seed_user."Id"
              AND other_doctor."Id" <> doctor."Id"
        ) THEN 'seed login is linked to another doctor profile'
        ELSE NULL
    END AS preserve_reason
FROM _seed_doctor_targets target
JOIN "Doctors" doctor
  ON doctor."Email" = target.profile_email
 AND doctor."LicenseNumber" = target.license_number
LEFT JOIN "Users" seed_user
  ON seed_user."Email" = target.user_email
 AND seed_user."Role" = 2
LEFT JOIN LATERAL (
    SELECT COUNT(*) AS appointment_count
    FROM "Appointments" a
    WHERE a."DoctorId" = seed_user."Id"
) appointments ON TRUE
LEFT JOIN LATERAL (
    SELECT COUNT(*) AS queue_count
    FROM "QueueEntries" q
    WHERE q."DoctorId" = seed_user."Id"
) queues ON TRUE;

-- Show every matched seed profile that will be preserved and why.
SELECT profile_email, user_email, appointment_count, queue_count, preserve_reason
FROM _seed_doctor_candidates
WHERE preserve_reason IS NOT NULL
ORDER BY profile_email;

-- Delete only exact seed profiles with no history and the expected linked user.
CREATE TEMP TABLE _deleted_seed_doctors ON COMMIT DROP AS
WITH deleted AS (
    DELETE FROM "Doctors" doctor
    USING _seed_doctor_candidates candidate
    WHERE doctor."Id" = candidate.doctor_id
      AND candidate.preserve_reason IS NULL
    RETURNING doctor."Email" AS profile_email, doctor."UserId" AS linked_user_id
)
SELECT * FROM deleted;

-- Delete the corresponding seed login only when it has no history and no
-- remaining Doctor profile points at it. Accounts for flagged identities stay.
CREATE TEMP TABLE _deleted_seed_users ON COMMIT DROP AS
WITH deleted AS (
    DELETE FROM "Users" seed_user
    USING _seed_doctor_targets target
    WHERE seed_user."Email" = target.user_email
      AND seed_user."Role" = 2
      AND NOT EXISTS (
          SELECT 1 FROM "Appointments" a WHERE a."DoctorId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM "QueueEntries" q WHERE q."DoctorId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM "MedicalRecords" m WHERE m."DoctorId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM "Prescriptions" p WHERE p."DoctorId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM "LabOrders" l WHERE l."DoctorId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM "Admissions" ad WHERE ad."AdmittingDoctorId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM "Doctors" doctor WHERE doctor."UserId" = seed_user."Id"
      )
      AND NOT EXISTS (
          SELECT 1 FROM _seed_doctor_candidates candidate
          WHERE candidate.user_email = seed_user."Email"
            AND candidate.preserve_reason IS NOT NULL
      )
    RETURNING seed_user."Email" AS user_email
)
SELECT * FROM deleted;

SELECT 'doctor_profiles_deleted' AS result, COUNT(*) AS count FROM _deleted_seed_doctors
UNION ALL
SELECT 'doctor_users_deleted', COUNT(*) FROM _deleted_seed_users;

COMMIT;
