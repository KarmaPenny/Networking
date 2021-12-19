using System;

namespace Networking {
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class Sync : Attribute { }
}