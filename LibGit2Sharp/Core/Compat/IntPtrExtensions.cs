namespace LibGit2Sharp.Core.Compat
{
	using System;
	using System.Runtime.InteropServices;
	using System.Text;

	public static class IntPtrExtensions
	{
		internal static string ToManagedString(this IntPtr value)
		{
			int len = 0;
			while (Marshal.ReadByte(value, len) != 0) ++len;
			if (len == 0) return string.Empty;
			byte[] buffer = new byte[len - 1];
			Marshal.Copy(value, buffer, 0, buffer.Length);

			return Encoding.UTF8.GetString(buffer);
		}
	}
}
