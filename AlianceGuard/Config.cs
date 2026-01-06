using System.ComponentModel;
using Exiled.API.Interfaces;

namespace AlianceGuard
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        public bool Debug { get; set; } = false;


    }
}
