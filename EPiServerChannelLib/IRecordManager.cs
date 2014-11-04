using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EPiServerChannelLib
{
    public interface IRecordManager
    {
        void Init();
        Guid GetEPiServerGuid(string key);
        void AddEPiServerGuid(string key, Guid pageGuid);
        void Close();
    }
}
