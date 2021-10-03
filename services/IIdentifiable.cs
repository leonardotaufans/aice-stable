using System;
using System.Collections.Generic;
using System.Text;

namespace aice_stable.services
{
    /// <summary>
    /// This is to make the code identifiable
    /// </summary>
    public interface IIdentifiable
    {
        string Identifier { get; }
    }
}
