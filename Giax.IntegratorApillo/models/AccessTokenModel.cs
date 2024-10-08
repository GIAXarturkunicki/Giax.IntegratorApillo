using System;
using System.Collections.Generic;
using System.Text;

namespace Giax.IntegratorApillo.models
{
     class AccessTokenModel
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpireAt { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpireAt { get; set; }
    }
}
