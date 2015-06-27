using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

#if LEAKS_CRTDBG
namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Class CrtDbg is used to augment the MSVC_CRTDBG memory leak reporting in libgit2
    /// by providing stack trace information for the C# stack at the point of the PInvoke.
    /// </summary>
    public static class CrtDbg
    {
        private static readonly object _lock = new object();
        /// <summary>
        /// This dictionary maps a unique C# formatted stack trace "aux_data" to a unique "aux_id".
        /// Yes, this looks backwards.  We use the formatted stack trace as the key. Think of this
        /// as a SQL stable with 2 indexed columns. During allocs, we compute the C# portion of the
        /// stack trace of the alloc and use that unique key to add/get the associated "aux_id"
        /// and give this back to C (because it does not want to marshall the C# stack trace back
        /// on every alloc). Later, during leak reporting, the C code will ask us for the "aux_data"
        /// for an "aux_id" and we do a reverse lookup. Reverse lookups are linear, but only
        /// happen at the end for actual leaks.
        /// </summary>
        private static Dictionary<string, uint> _dict = new Dictionary<string, uint>();
        private static readonly Encoding _enc = new UTF8Encoding(false, false);
        private static NativeMethods.git_win32__stack__aux_cb_alloc _cb_alloc = cb_alloc;
        private static NativeMethods.git_win32__stack__aux_cb_lookup _cb_lookup = cb_lookup;

        /// <summary>
        /// Uniquely insert into the dictionary.
        /// </summary>
        /// <param name="key">Formatted C# stack trace</param>
        /// <returns>Unique aux_id</returns>
        private static uint AddOrGet(string key)
        {
            lock (_lock)
            {
                uint value;
                if (!_dict.TryGetValue(key, out value))
                {
                    value = (uint)_dict.Count;
                    _dict.Add(key, value);
                }
                return value;
            }
        }

        /// <summary>
        /// Reverse lookup on dictionary.
        /// </summary>
        /// <param name="value">aux_id</param>
        /// <param name="key">Formatted C# stack trace</param>
        /// <returns>true if aux_id found</returns>
        private static bool ReverseLookup(uint value, out string key)
        {
            lock (_lock)
            {
                foreach (KeyValuePair<string, uint> p in _dict)
                {
                    if (p.Value == value)
                    {
                        key = p.Key;
                        return true;
                    }
                }

                key = null;
                return false;
            }
        }

        /// <summary>
        /// Callback used by C layer to get an "aux_id" for the current
        /// C# stack context.  (Internally adds data to dictionary if
        /// required.0
        /// </summary>
        /// <param name="aux_id"></param>
        private static void cb_alloc(out uint aux_id)
        {
            StackTrace st = new StackTrace(1, true);
            string s = "";
            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                s += string.Format("\t\t{0}:{1}> {2}\n",
                    Path.GetFileName(sf.GetFileName()),
                    sf.GetFileLineNumber(),
                    sf.GetMethod());
            }

            aux_id = CrtDbg.AddOrGet(s);
        }

        /// <summary>
        /// Callback used by C layer to get the "aux_data" (the
        /// formatted C# stacktrace) for the requested "aux_id".
        /// String is converted to UTF8 and copied into the
        /// provided buffer.
        /// </summary>
        /// <param name="aux_id"></param>
        /// <param name="buf"></param>
        /// <param name="buf_len"></param>
        private static void cb_lookup(uint aux_id, IntPtr buf, uint buf_len)
        {
            string s;
            if (!CrtDbg.ReverseLookup(aux_id, out s))
                return;

            int len_utf8 = _enc.GetByteCount(s);
            if (len_utf8 == 0)
                return;

            unsafe
            {
                fixed (char* ps = s)
                {
                    byte* b = (byte*)buf.ToPointer();
                    _enc.GetBytes(ps, len_utf8, b, (int)buf_len);
                }
            }
        }

        /// <summary>
        /// Register CRTDBG AUX callbacks.
        /// </summary>
        public static void SetCallbacks()
        {
            // We pass private static variables set to the actual functions
            // because we need for the function references to not be GC'd
            // while C is still using them.
            NativeMethods.git_win32__stack__set_aux_cb(_cb_alloc, _cb_lookup);
        }
    }
}
#endif
