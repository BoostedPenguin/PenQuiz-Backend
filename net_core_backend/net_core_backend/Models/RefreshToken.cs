using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class RefreshToken
    {
        public int Id { get; set; }
        public int UsersId { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public string CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }
        public string RevokedByIp { get; set; }
        public string ReplacedByToken { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsActive => Revoked == null && !IsExpired;

        public virtual Users Users { get; set; }
    }
}
