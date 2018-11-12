using System;
using System.Collections.Generic;
using SharpBox.Core.Processes;

namespace SharpBox.Core.Sessions
{
    public class Session : ISession
    {
        public String ID { get; private set; } = Guid.NewGuid().ToString();
        public List<IProcess> Processes { get; private set; } = new List<IProcess>();
    }
}
