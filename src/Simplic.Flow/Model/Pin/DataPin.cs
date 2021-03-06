﻿using System;

namespace Simplic.Flow
{
    public class DataPin : IPin
    {
        public Guid Id { get; set; }
        public BaseNode Owner { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public PinDirection Direction { get; set; }
        public Type DataType { get; set; }
        public DataPinContainerType ContainerType { get; set; }
        public bool IsNullable { get; set; }
        public object DefaultValue { get; set; }
        public bool IsGeneric { get; set; }

        /// <summary>
        /// Gets or sets the temporary node id
        /// </summary>
        public Guid TemporaryNodeId { get; set; }
    }
}