using Domain.DTOs;

namespace Controller.Interfaces;

public interface IAiFeedbackService
{
    Task<string> GenerateFeedbackAsync(string predictedLevel, PythonPredictResponseDTO mlResult);
}
