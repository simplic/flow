using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simplic.Flow.Editor.Definition
{
    public class PinDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public PinDirectionDefinition PinDirection { get; set; }
        public bool IsDynamic { get; set; }
    }
}
