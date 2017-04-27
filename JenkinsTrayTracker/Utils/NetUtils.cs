﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JenkinsTrayTracker.Utils
{
    public static class NetUtils
    {
        public static string ConcatUrls(string fragment1, params string[] fragments)
        {
            string res = fragment1.TrimEnd('/');
            foreach (string fragment in fragments)
                res += "/" + fragment.TrimStart('/');
            return res;
        }
    }
}
