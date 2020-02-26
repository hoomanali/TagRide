using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TagRides.Shared.DataStore
{
    public interface IDataStore
    {
        Task<string> GetStringResource(string resource);
        Task PostStringResource(string resource, string data);

        Task GetStreamResource(string resource, Stream output);
        Task PostStreamResource(string resource, Stream data);

        Task DeleteResource(string resource);

        Task<byte[]> GetByteResource(string resource);

        Task<bool> ResourceExists(string resource);
    }
}
