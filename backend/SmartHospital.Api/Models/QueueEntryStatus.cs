namespace SmartHospital.Api.Models;

public enum QueueEntryStatus
{
    Waiting    = 1,
    Called     = 2,
    InProgress = 3,
    Completed  = 4,
    NoShow     = 5,
    Skipped    = 6
}
