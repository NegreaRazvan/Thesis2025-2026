using Domain.DTOs;
using Domain.Exceptions.Custom;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Controller.Interfaces;
using Service.Interfaces;
using Service.Services;

namespace Backend.Tests.Services;

public class SubmissionServiceTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepo = new();
    private readonly Mock<IFlashcardRepository> _flashcardRepo = new();
    private readonly Mock<IPythonMlService> _pythonMl = new();
    private readonly Mock<IAiFeedbackService> _aiFeedback = new();
    private readonly Mock<IAppValidatorFactory> _validatorFactory = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly SubmissionService _sut;

    private const string UserId = "user-123";

    public SubmissionServiceTests()
    {
        var validator = new Mock<IValidator<SubmitTextDTO>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<SubmitTextDTO>(), default))
            .ReturnsAsync(new ValidationResult());
        _validatorFactory.Setup(f => f.Get<SubmitTextDTO>()).Returns(validator.Object);

        // Wire scope factory so background Task.Run resolves repos from a fresh scope
        var sp = new Mock<IServiceProvider>();
        sp.Setup(p => p.GetService(typeof(ISubmissionRepository))).Returns(_submissionRepo.Object);
        sp.Setup(p => p.GetService(typeof(IFlashcardRepository))).Returns(_flashcardRepo.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(sp.Object);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        _sut = new SubmissionService(
            _submissionRepo.Object,
            _pythonMl.Object,
            _aiFeedback.Object,
            _validatorFactory.Object,
            _scopeFactory.Object);
    }

    private static PythonPredictResponseDTO CreateMlResult(string level = "B1", List<PythonErrorItemDTO>? errors = null) => new()
    {
        PredictedLevel = level,
        Confidence = new Dictionary<string, float> { ["B1"] = 0.85f },
        Features = new Dictionary<string, float> { ["mattr"] = 0.72f },
        Errors = errors ?? [],
        VocabCandidates = []
    };

    private static SubmissionResponseDTO CreateSubmissionResponse(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        TextContent = "Ich lerne Deutsch.",
        PredictedCefr = "B1",
        AiFeedback = "Good work!",
        SubmittedAt = DateTime.UtcNow
    };


    [Fact]
    public async Task SubmitAsync_Success_CallsMlAndFeedbackAndRepo()
    {
        var dto = new SubmitTextDTO("Ich lerne Deutsch.");
        var mlResult = CreateMlResult();
        var submission = CreateSubmissionResponse();

        _pythonMl.Setup(p => p.PredictAsync(dto.Text)).ReturnsAsync(mlResult);
        _aiFeedback.Setup(a => a.GenerateFeedbackAsync("B1", mlResult)).ReturnsAsync("Good work!");
        _submissionRepo.Setup(r => r.AddAsync(UserId, dto.Text, mlResult, "Good work!")).ReturnsAsync(submission);

        var result = await _sut.SubmitAsync(UserId, dto);

        result.Should().Be(submission);
        _pythonMl.Verify(p => p.PredictAsync(dto.Text), Times.Once);
        _aiFeedback.Verify(a => a.GenerateFeedbackAsync("B1", mlResult), Times.Once);
        _submissionRepo.Verify(r => r.AddAsync(UserId, dto.Text, mlResult, "Good work!"), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_WithErrors_UpsertsErrorPatternsInBackground()
    {
        var errors = new List<PythonErrorItemDTO>
        {
            new() { Category = "grammar", RuleId = "RULE_1", BadText = "fehler" },
            new() { Category = "spelling", RuleId = "RULE_2", BadText = "wort" }
        };
        var dto = new SubmitTextDTO("Test text.");
        var mlResult = CreateMlResult(errors: errors);
        var submission = CreateSubmissionResponse();

        _pythonMl.Setup(p => p.PredictAsync(dto.Text)).ReturnsAsync(mlResult);
        _aiFeedback.Setup(a => a.GenerateFeedbackAsync(It.IsAny<string>(), mlResult)).ReturnsAsync("feedback");
        _submissionRepo.Setup(r => r.AddAsync(UserId, dto.Text, mlResult, "feedback")).ReturnsAsync(submission);

        await _sut.SubmitAsync(UserId, dto);
        await Task.Delay(200);

        _submissionRepo.Verify(r => r.UpsertErrorPatternAsync(UserId, "grammar", "RULE_1"), Times.Once);
        _submissionRepo.Verify(r => r.UpsertErrorPatternAsync(UserId, "spelling", "RULE_2"), Times.Once);
        _flashcardRepo.Verify(r => r.CreateFromSubmissionAsync(UserId, submission.Id), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ErrorsWithEmptyRuleId_SkipsUpsert()
    {
        var errors = new List<PythonErrorItemDTO>
        {
            new() { Category = "grammar", RuleId = "", BadText = "fehler" }
        };
        var dto = new SubmitTextDTO("Test text.");
        var mlResult = CreateMlResult(errors: errors);
        var submission = CreateSubmissionResponse();

        _pythonMl.Setup(p => p.PredictAsync(dto.Text)).ReturnsAsync(mlResult);
        _aiFeedback.Setup(a => a.GenerateFeedbackAsync(It.IsAny<string>(), mlResult)).ReturnsAsync("feedback");
        _submissionRepo.Setup(r => r.AddAsync(UserId, dto.Text, mlResult, "feedback")).ReturnsAsync(submission);

        await _sut.SubmitAsync(UserId, dto);
        await Task.Delay(200);

        _submissionRepo.Verify(r => r.UpsertErrorPatternAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ValidationFails_ThrowsEntityValidation()
    {
        var validator = new Mock<IValidator<SubmitTextDTO>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<SubmitTextDTO>(), default))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Text", "Text is required.") }));
        _validatorFactory.Setup(f => f.Get<SubmitTextDTO>()).Returns(validator.Object);

        var sut = new SubmissionService(
            _submissionRepo.Object, _pythonMl.Object,
            _aiFeedback.Object, _validatorFactory.Object, _scopeFactory.Object);

        var act = () => sut.SubmitAsync(UserId, new SubmitTextDTO(""));

        await act.Should().ThrowAsync<EntityValidationException>();
    }

    [Fact]
    public async Task SubmitAsync_MlServiceDown_ThrowsServiceUnavailable()
    {
        _pythonMl.Setup(p => p.PredictAsync(It.IsAny<string>()))
            .ThrowsAsync(new ServiceUnavailableException("ML service is unavailable."));

        var act = () => _sut.SubmitAsync(UserId, new SubmitTextDTO("text"));

        await act.Should().ThrowAsync<ServiceUnavailableException>();
    }


    [Fact]
    public async Task GetHistoryAsync_DelegatesToRepo()
    {
        var expected = new List<SubmissionSummaryDTO>
        {
            new() { Id = Guid.NewGuid(), TextContent = "Test", PredictedCefr = "A2", SubmittedAt = DateTime.UtcNow }
        };
        _submissionRepo.Setup(r => r.GetByUserAsync(UserId)).ReturnsAsync(expected);

        var result = await _sut.GetHistoryAsync(UserId);

        result.Should().BeEquivalentTo(expected);
    }


    [Fact]
    public async Task GetByIdAsync_Found_ReturnsSubmission()
    {
        var id = Guid.NewGuid();
        var submission = CreateSubmissionResponse(id);
        _submissionRepo.Setup(r => r.GetByIdAsync(UserId, id)).ReturnsAsync(submission);

        var result = await _sut.GetByIdAsync(UserId, id);

        result.Should().Be(submission);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _submissionRepo.Setup(r => r.GetByIdAsync(UserId, id)).ReturnsAsync((SubmissionResponseDTO?)null);

        var act = () => _sut.GetByIdAsync(UserId, id);

        await act.Should().ThrowAsync<NotFoundException>();
    }


    [Fact]
    public async Task DeleteAsync_DelegatesToRepo()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteAsync(UserId, id);

        _submissionRepo.Verify(r => r.DeleteAsync(UserId, id), Times.Once);
    }
}
