using Microsoft.AspNetCore.Http;

namespace CodeM.FastApi.Context.Wrappers
{
    public class CookieOptionsExt : CookieOptions
    {

        public bool Encrypt { get; set; } = false;

    }
}
