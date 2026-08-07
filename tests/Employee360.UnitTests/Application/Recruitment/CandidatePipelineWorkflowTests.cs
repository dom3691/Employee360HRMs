using Employee360.Application.Features.Recruitment;
using Employee360.Domain.Enums;
using FluentAssertions;

namespace Employee360.UnitTests.Application.Recruitment;

public class CandidatePipelineWorkflowTests
{
    [Theory]
    [InlineData(CandidateStage.Applied, CandidateStage.Screening, true)]
    [InlineData(CandidateStage.Screening, CandidateStage.Interview, true)]
    [InlineData(CandidateStage.Interview, CandidateStage.Assessment, true)]
    [InlineData(CandidateStage.Assessment, CandidateStage.Offer, true)]
    [InlineData(CandidateStage.Interview, CandidateStage.Offer, true)]
    [InlineData(CandidateStage.Offer, CandidateStage.Hired, true)]
    [InlineData(CandidateStage.Applied, CandidateStage.Rejected, true)]
    [InlineData(CandidateStage.Applied, CandidateStage.Hired, false)]
    [InlineData(CandidateStage.Hired, CandidateStage.Screening, false)]
    [InlineData(CandidateStage.Rejected, CandidateStage.Applied, false)]
    public void EnsureTransition_ValidatesPipeline(
        CandidateStage from,
        CandidateStage to,
        bool shouldSucceed)
    {
        CandidatePipelineWorkflow.EnsureTransition(from, to).IsSuccess.Should().Be(shouldSucceed);
    }

    [Fact]
    public void SplitName_ParsesFirstAndLast()
    {
        var (first, last) = CandidatePipelineWorkflow.SplitName("Ada Chioma Okafor");
        first.Should().Be("Ada");
        last.Should().Be("Chioma Okafor");
    }
}
