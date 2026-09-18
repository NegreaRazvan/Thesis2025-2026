using Domain.DTOs;
using Microsoft.AspNetCore.Http;

namespace Controller.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResultDTO> TranscribeAsync(IFormFile audio);
}
