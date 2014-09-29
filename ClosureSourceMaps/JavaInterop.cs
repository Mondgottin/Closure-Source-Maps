namespace ClosureSourceMaps
{
	using System.Collections.Generic;

	static class JavaInterop
	{
		public static T PeekOrNull<T>(this Stack<T> stack)
			where T : class
		{
			return stack.Count == 0 ? null : stack.Peek();
		}
	}
}
