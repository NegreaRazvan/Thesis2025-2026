using AutoMapper;
using Domain.DTOs;
using Repository.EFEntities;
using System.Text.Json;

namespace Repository.AutoMapper;

public class EFEntitiesMappingProfile : Profile
{
    public EFEntitiesMappingProfile()
    {
        CreateMap<AppUser, UserResponseDTO>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Username, o => o.MapFrom(s => s.UserName ?? ""))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Email ?? ""));

        CreateMap<AppUser, UserProfileDTO>()
            .ForMember(d => d.Username, o => o.MapFrom(s => s.UserName ?? ""))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.Email ?? ""))
            .ForMember(d => d.HasAvatar, o => o.MapFrom(s => s.AvatarBytes != null && s.AvatarBytes.Length > 0));

        // SubmissionError → ErrorItemDTO  (must be registered before Submission map)
        CreateMap<SubmissionError, ErrorItemDTO>()
            .ForMember(d => d.Category, o => o.MapFrom(s => s.ErrorCategory))
            .ForMember(d => d.Suggestions, o => o.MapFrom(s =>
                JsonSerializer.Deserialize<List<string>>(s.SuggestionsJson,
                    (JsonSerializerOptions?)null) ?? new()))
            .ForMember(d => d.Lemma, o => o.MapFrom(s => s.Lemma))
            .ForMember(d => d.EnglishTranslation, o => o.MapFrom(s => s.EnglishTranslation))
            .ForMember(d => d.Article, o => o.MapFrom(s => s.Article))
            .ForMember(d => d.Plural, o => o.MapFrom(s => s.Plural));

        CreateMap<Submission, SubmissionResponseDTO>()
            .ForMember(d => d.TextContent, o => o.MapFrom(s => s.TextContent))
            .ForMember(d => d.Confidence, o => o.MapFrom(s =>
                JsonSerializer.Deserialize<Dictionary<string, float>>(s.ConfidenceJson,
                    (JsonSerializerOptions?)null) ?? new()))
            .ForMember(d => d.Features, o => o.MapFrom(s => new FeatureSnapshotDTO
            {
                NTokens = s.NTokens,
                Mattr = s.Mattr,
                MedianZipf = s.MedianZipf,
                AvgDepDepth = s.AvgDepDepth,
                GrammarErrorRate = s.GrammarErrorRate,
                CoherenceMean = s.CoherenceMean,
                VocabSyntaxInteraction = s.VocabSyntaxInteraction,
                LexicalSophistication = s.LexicalSophistication,
            }))
            .ForMember(d => d.Errors, o => o.MapFrom(s => s.Errors))
            .ForMember(d => d.VocabCandidates, o => o.Ignore())  // set manually in SubmissionRepository
            .ForMember(d => d.ShortTextWarning, o => o.Ignore()); // set manually in SubmissionRepository
    }
}