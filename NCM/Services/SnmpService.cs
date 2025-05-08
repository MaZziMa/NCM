using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace NCM.Services
{
    public class SnmpService
    {
        private readonly string _community;
        private readonly VersionCode _version;
        private readonly int _timeout;
        private readonly IDictionary<string, string> _oids;

        public SnmpService(IConfiguration configuration)
        {
            // Inject IConfiguration vào constructor
            // rồi lấy section SNMP
            var snmpSection = configuration.GetSection("SNMP");

            // Bây giờ snmpSection đã tồn tại trong context
            _community = snmpSection["Community"];

            // Ví dụ config lưu "V2" hoặc "2"
            var rawVer = snmpSection["Version"]?.Trim() ?? "2";
            var verString = rawVer.StartsWith("V", StringComparison.OrdinalIgnoreCase)
                                ? rawVer
                                : $"V{rawVer}";
            if (!Enum.TryParse<VersionCode>(verString, true, out _version))
            {
                // fallback hoặc ném
                _version = VersionCode.V2;
            }

            _timeout = int.TryParse(snmpSection["TimeoutMs"], out var t) ? t : 5000;

            _oids = snmpSection.GetSection("Oids")
                        .GetChildren()
                        .ToDictionary(x => x.Key, x => x.Value);
        }

        public IDictionary<string, string> GetBasicInfo(string ip)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), 161);
            var vars = _oids.Values
                .Select(oid => new Variable(new ObjectIdentifier(oid)))
                .ToList();

            var result = Messenger.Get(
                _version,
                endpoint,
                new OctetString(_community),
                vars,
                _timeout);

            return result.ToDictionary(
                v => _oids.First(kv => kv.Value == v.Id.ToString()).Key,
                v => v.Data.ToString());
        }
    }
}
