using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SacanaWrapper
{
    public interface IPluginCreator
    {
        string Name { get; }
        string Filter { get; }
        IPlugin Create(byte[] Script);
    }

    public interface IPlugin
    {
        string[] Import();
        byte[] Export(string[] Lines);
    }
}
