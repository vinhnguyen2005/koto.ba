using AutoMapper;
using Kotoba.Modules.Domain.DTOs;
using Kotoba.Modules.Domain.Entities;

namespace Kotoba.Modules.Application.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDto>()
                .ConstructUsing((src, context) => new UserDto
                {
                    UserId = src.Id,
                    DisplayName = src.DisplayName,
                    AvatarUrl = src.AvatarUrl,
                    UserName = src.UserName!,
                    Email = src.Email!,
                    IsOnline = src.IsOnline,
                    LastSeenAt = src.LastSeenAt
                });
        }
    }
}
