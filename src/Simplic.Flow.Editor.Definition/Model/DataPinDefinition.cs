using System;

namespace Simplic.Flow.Editor.Definition
{
    public class DataPinDefinition : PinDefinition
    {
        public Type Type { get; set; }
        public bool IsGeneric { get; set; }
        public string AllowedTypes { get; set; }
    }
}
