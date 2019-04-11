using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace kinny_social_core.Services
{
    public interface ICredentialGetter<TCredentials>
    {
        Task<TCredentials> GetCredentials();
    }
}
