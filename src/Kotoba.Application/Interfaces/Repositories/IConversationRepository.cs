using Kotoba.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kotoba.Application.Interfaces.Repositories
{
    public interface IConversationRepository
    {
        Task<Conversation> GetConversationAsync(string userId);
    }
}
