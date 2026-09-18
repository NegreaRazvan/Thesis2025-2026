using Domain.DTOs;

namespace Controller.Interfaces;

public interface IPythonMlService
{
    Task<PythonPredictResponseDTO> PredictAsync(string text);
}
