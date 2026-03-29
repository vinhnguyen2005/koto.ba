using AutoMapper;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Application.Mappings
{
    public class StoryProfile : Profile
    {
        public StoryProfile()
        {
            CreateMap<Story, StoryDto>()
                .ConstructUsing((src, context) => new StoryDto
                {
                    StoryId = src.Id,
                    UserId = src.UserId,
                    User = context.Mapper.Map<UserDto>(src.User),
                    Content = src.Content,
                    MediaUrl = src.MediaUrl,
                    ExpiresAt = src.ExpiresAt,
                    AllowedUserIds = src.Permissions.Select(p => p.UserId).ToList()
                });
        }
    }
}
