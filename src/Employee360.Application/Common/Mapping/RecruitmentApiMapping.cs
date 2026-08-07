using Employee360.Domain.Enums;

namespace Employee360.Application.Common.Mapping;

/// <summary>Maps recruitment enums to frontend contract string values.</summary>
public static class RecruitmentApiMapping
{
    public static string ToApiStage(CandidateStage stage) => stage switch
    {
        CandidateStage.Applied => "Applied",
        CandidateStage.Screening => "Screening",
        CandidateStage.Interview => "Interview",
        CandidateStage.Assessment => "Assessment",
        CandidateStage.Offer => "Offer",
        CandidateStage.Hired => "Hired",
        CandidateStage.Rejected => "Rejected",
        _ => stage.ToString(),
    };

    public static CandidateStage ParseStage(string value) => value.Trim() switch
    {
        "Applied" => CandidateStage.Applied,
        "Screening" => CandidateStage.Screening,
        "Interview" => CandidateStage.Interview,
        "Assessment" => CandidateStage.Assessment,
        "Offer" => CandidateStage.Offer,
        "Hired" => CandidateStage.Hired,
        "Rejected" => CandidateStage.Rejected,
        _ => Enum.Parse<CandidateStage>(value, ignoreCase: true),
    };

    public static string ToApiTimesheetStatus(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Draft => "Draft",
        TimesheetStatus.Submitted => "Submitted",
        TimesheetStatus.Approved => "Approved",
        TimesheetStatus.Rejected => "Rejected",
        _ => status.ToString(),
    };
}
