﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenWrap
{
    public class ActionOnDispose : IDisposable
    {
        Action _action;
        public static readonly ActionOnDispose None = new ActionOnDispose(() => { });
        public ActionOnDispose(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
