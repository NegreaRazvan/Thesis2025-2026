using Controller.Interfaces;
using Domain.DTOs;
using Domain.Exceptions.Custom;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Service.Interfaces;
using Service.Utils;

namespace Service.Services;

public class SubmissionService(
    ISubmissionRepository submissionRepository,
    IPythonMlService pythonMl,
    IAiFeedbackService aiFeedback,
    IAppValidatorFactory validator,
    IServiceScopeFactory scopeFactory) : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository = submissionRepository;
    private readonly IPythonMlService _pythonMl = pythonMl;
    private readonly IAiFeedbackService _aiFeedback = aiFeedback;
    private readonly IAppValidatorFactory _validator = validator;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILog _logger = LogManager.GetLogger(typeof(SubmissionService));

    public async Task<SubmissionResponseDTO> SubmitAsync(string userId, SubmitTextDTO dto)
    {
        _logger.InfoFormat("Submit request from user {0}, text length {1}", userId, dto.Text.Length);
        await ValidationHelper.ValidateAndThrowAsync(_validator.Get<SubmitTextDTO>(), dto);

        var mlResult = await _pythonMl.PredictAsync(dto.Text);
        var feedback = await _aiFeedback.GenerateFeedbackAsync(mlResult.PredictedLevel, mlResult);
        var submission = await _submissionRepository.AddAsync(userId, dto.Text, mlResult, feedback);

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var submRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
            var fcRepo   = scope.ServiceProvider.GetRequiredService<IFlashcardRepository>();
            try
            {
                foreach (var err in mlResult.Errors.Where(e => !string.IsNullOrEmpty(e.RuleId)))
                    await submRepo.UpsertErrorPatternAsync(userId, err.Category, err.RuleId);

                if (mlResult.Errors.Any(e => !string.IsNullOrEmpty(e.BadText)))
                    await fcRepo.CreateFromSubmissionAsync(userId, submission.Id);
            }
            catch (Exception ex)
            {
                _logger.Error("Background post-submission processing failed.", ex);
            }
        });

        return submission;
    }

    public async Task<List<SubmissionSummaryDTO>> GetHistoryAsync(string userId) =>
        await _submissionRepository.GetByUserAsync(userId);

    public async Task<SubmissionResponseDTO> GetByIdAsync(string userId, Guid submissionId) =>
        await _submissionRepository.GetByIdAsync(userId, submissionId)
            ?? throw new NotFoundException($"Submission {submissionId} not found.");

    public async Task DeleteAsync(string userId, Guid submissionId) =>
        await _submissionRepository.DeleteAsync(userId, submissionId);
}