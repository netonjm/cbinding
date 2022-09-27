using ClangSharp;

namespace CBinding.Parser
{
	public class Namespace : Symbol
	{
		public Namespace (CProject proj, CXCursor cursor) : base (proj, cursor)
		{
		}

        public object ParentCursor { get; internal set; }
        public object Parent { get; internal set; }
    }
}
