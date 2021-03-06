﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Simplic.Flow
{
    public class FlowInstance
    {
        public Guid Id { get; set; }
        [JsonIgnore]
        public Flow Flow { get; set; }
        [JsonIgnore]
        public Guid FlowId { get; set; }
        public IList<NodeScope<EventNode>> CurrentNodes { get; set; } = new List<NodeScope<EventNode>>();
        public IList<FlowVariable> Variables { get { return Flow.Variables; } }
        public DataPinScope DataScope { get; set; } = new DataPinScope();
        public bool IsAlive { get { return CurrentNodes.Count > 0; } }
        public bool IsFailed { get; set; }
    }
}
