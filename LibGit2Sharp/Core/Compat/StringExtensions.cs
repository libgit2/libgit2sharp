namespace LibGit2Sharp.Core.Compat
{
	using System;
	using System.Runtime.InteropServices;
	using System.Text;

	public static class StringExtensions
	{
		internal static IntPtr ToNativeUtf8(this string value)
		{
			int len = Encoding.UTF8.GetByteCount(value);
			byte[] buffer = new byte[len + 1];
			Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, 0);
			IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
			Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

			return nativeUtf8;
		}
	}
}
