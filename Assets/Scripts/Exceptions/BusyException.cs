using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using System;

namespace Exceptions
{
    public class BusyException : ApplicationException
    {
        public BusyException(string message) : base(message) { }
    }
}